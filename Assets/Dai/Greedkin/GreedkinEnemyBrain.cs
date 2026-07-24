using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class GreedkinEnemyBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float runSpeed = 3.2f;
    [SerializeField, Min(0.01f)] private float waypointArrivalDistance = 0.15f;
    [SerializeField, Min(0.01f)] private float attackRange = 0.8f;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float attackInterval = 0.9f;
    [SerializeField, Range(0f, 1f)] private float attackImpactDelay = 0.22f;

    [Header("Targeting")]
    [SerializeField] private CombatTeam team = CombatTeam.Enemy;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField, Min(0.1f)] private float detectionRadius = 4f;
    [SerializeField, Min(0.02f)] private float targetScanInterval = 0.15f;
    [SerializeField, Min(1f)] private float attackDamage = 20f;
    [SerializeField, Min(1f)] private float maxHealth = 80f;

    private GreedkinRoute route;
    private Transform mainHouse;
    private int waypointIndex;
    private float nextAttackTime;
    private float nextTargetScanTime;
    private float pendingDamageTime;
    private bool initialized;
    private bool isDead;
    private bool hasPendingDamage;
    private Vector2 pendingPosition;
    private CombatHealth ownHealth;
    private CombatHealth currentTarget;
    private CombatHealth pendingDamageTarget;
    private readonly Collider2D[] targetBuffer = new Collider2D[32];

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        ownHealth = GetComponent<CombatHealth>();
        if (ownHealth == null)
        {
            ownHealth = gameObject.AddComponent<CombatHealth>();
        }
        ownHealth.ConfigureRuntime(team, maxHealth);

        // The visible game is XY, while Unity's NavMesh is XZ. The proxy agent
        // travels on the baked XZ NavMesh and this root mirrors it back to XY.
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
            navMeshAgent.transform.SetParent(null, true);
            navMeshAgent.transform.position = ToNavPosition(transform.position);
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
            navMeshAgent.enabled = true;
        }

        // A controller normally writes the first Idle frame automatically.
        // Force that first evaluation once so a prefab never appears blank for
        // a frame when it has been instantiated during a spawn wave.
        if (spriteRenderer != null && spriteRenderer.sprite == null && animator != null)
        {
            animator.Update(0f);
        }
    }

    public void Configure(GreedkinRoute newRoute, Transform newMainHouse)
    {
        route = newRoute;
        mainHouse = newMainHouse;
        EnsureMainHouseCanBeAttacked();
        TryInitialize();
    }

    public void PlayDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentTarget = null;
        pendingDamageTarget = null;
        hasPendingDamage = false;
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
        }
        if (animator != null)
        {
            animator.SetFloat(MoveSpeedHash, 0f);
            animator.SetTrigger(DieHash);
        }
    }

    private void Update()
    {
        ResolvePendingDamage();
        if (!initialized || isDead || navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        RefreshCombatTarget();
        if (IsCurrentTargetValid())
        {
            Vector2 combatTargetPosition = currentTarget.GetClosestPoint(transform.position);
            Vector2 toTarget = combatTargetPosition - (Vector2)transform.position;
            if (toTarget.sqrMagnitude <= attackRange * attackRange)
            {
                navMeshAgent.isStopped = true;
                pendingPosition = transform.position;
                SetMoveSpeed(0f);
                FaceTarget(currentTarget);
                TryAttack(currentTarget);
                return;
            }

            // Targets found inside the detection circle take priority over the
            // route.  The agent still uses the NavMesh, so it cannot cut through
            // walls or leave the baked walkable path.
            navMeshAgent.isStopped = false;
            navMeshAgent.speed = runSpeed;
            navMeshAgent.SetDestination(ToNavPosition(combatTargetPosition));
            pendingPosition = ToWorldPosition(navMeshAgent.transform.position);
            SetMoveSpeed(navMeshAgent.velocity.sqrMagnitude > 0.0025f ? 2f : 0f);
            if (spriteRenderer != null && Mathf.Abs(navMeshAgent.velocity.x) > 0.01f)
            {
                spriteRenderer.flipX = navMeshAgent.velocity.x < 0f;
            }
            return;
        }

        Transform routeTarget = GetCurrentRouteTarget(out bool finalLeg);
        if (routeTarget == null)
        {
            SetMoveSpeed(0f);
            return;
        }

        Vector2 worldPosition = ToWorldPosition(navMeshAgent.transform.position);
        Vector2 targetPosition = GetStraightRoutePosition(routeTarget, finalLeg);
        if (finalLeg && Vector2.Distance(worldPosition, targetPosition) <= attackRange)
        {
            navMeshAgent.isStopped = true;
            SetMoveSpeed(0f);
            CombatHealth houseHealth = GetHealth(mainHouse);
            if (houseHealth != null && houseHealth.CanBeAttackedBy(team))
            {
                currentTarget = houseHealth;
                TryAttack(houseHealth);
            }
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.speed = finalLeg ? runSpeed : walkSpeed;
        Vector3 navTarget = ToNavPosition(targetPosition);
        if ((navMeshAgent.destination - navTarget).sqrMagnitude > 0.01f)
        {
            navMeshAgent.SetDestination(navTarget);
        }

        pendingPosition = ToWorldPosition(navMeshAgent.transform.position);
        if (!finalLeg && Vector2.Distance(worldPosition, targetPosition) <= waypointArrivalDistance)
        {
            waypointIndex++;
        }

        float movementState = navMeshAgent.velocity.sqrMagnitude > 0.0025f ? (finalLeg ? 2f : 1f) : 0f;
        SetMoveSpeed(movementState);
        if (spriteRenderer != null && Mathf.Abs(navMeshAgent.velocity.x) > 0.01f)
        {
            spriteRenderer.flipX = navMeshAgent.velocity.x < 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!initialized || isDead)
        {
            return;
        }

        if (body != null)
        {
            body.MovePosition(pendingPosition);
        }
        else
        {
            transform.position = new Vector3(pendingPosition.x, pendingPosition.y, transform.position.z);
        }
    }

    private void OnDestroy()
    {
        if (navMeshAgent != null)
        {
            Destroy(navMeshAgent.gameObject);
        }
    }

    private void TryInitialize()
    {
        if (initialized || route == null || mainHouse == null || navMeshAgent == null)
        {
            return;
        }

        if (!NavMesh.SamplePosition(ToNavPosition(transform.position), out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name} could not find the Greedkin XZ NavMesh at its spawn position.", this);
            return;
        }

        navMeshAgent.Warp(hit.position);
        pendingPosition = ToWorldPosition(hit.position);
        initialized = true;
    }

    private Transform GetCurrentRouteTarget(out bool finalLeg)
    {
        Transform waypoint = route.GetWaypoint(waypointIndex);
        finalLeg = waypoint == null;
        return finalLeg ? mainHouse : waypoint;
    }

    private Vector2 GetStraightRoutePosition(Transform target, bool finalLeg)
    {
        if (finalLeg)
        {
            return mainHouse.position;
        }

        // The test level is a horizontal 2D lane.  Existing route waypoints
        // may have been moved vertically in the Scene, but the enemy must not
        // zig-zag: preserve each waypoint's X position and keep the lane at
        // the Main House's Y position.
        return new Vector2(target.position.x, mainHouse.position.y);
    }

    private void TryAttack(CombatHealth target)
    {
        if (target == null || hasPendingDamage || Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;
        pendingDamageTarget = target;
        pendingDamageTime = Time.time + attackImpactDelay;
        hasPendingDamage = true;
        animator?.SetTrigger(AttackHash);
    }

    private void RefreshCombatTarget()
    {
        if (Time.time < nextTargetScanTime)
        {
            return;
        }

        nextTargetScanTime = Time.time + targetScanInterval;
        ContactFilter2D targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = targetLayers,
            useTriggers = true
        };

        int found = Physics2D.OverlapCircle(transform.position, detectionRadius, targetFilter, targetBuffer);
        CombatHealth nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Vector2 origin = transform.position;
        for (int index = 0; index < found; index++)
        {
            if (!CombatHealth.TryGetTarget(targetBuffer[index], out CombatHealth candidate)
                || candidate == ownHealth
                || !candidate.CanBeAttackedBy(team))
            {
                continue;
            }

            float distance = (candidate.GetClosestPoint(origin) - origin).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }
        currentTarget = nearest;
    }

    private bool IsCurrentTargetValid()
    {
        if (currentTarget == null || !currentTarget.CanBeAttackedBy(team))
        {
            return false;
        }
        return (currentTarget.GetClosestPoint(transform.position) - (Vector2)transform.position).sqrMagnitude
               <= detectionRadius * detectionRadius;
    }

    private void ResolvePendingDamage()
    {
        if (!hasPendingDamage || Time.time < pendingDamageTime)
        {
            return;
        }

        hasPendingDamage = false;
        if (pendingDamageTarget != null && pendingDamageTarget.CanBeAttackedBy(team)
            && (pendingDamageTarget.GetClosestPoint(transform.position) - (Vector2)transform.position).sqrMagnitude
            <= attackRange * attackRange)
        {
            pendingDamageTarget.TakeDamage(attackDamage, transform.position);
        }
        pendingDamageTarget = null;
    }

    private void FaceTarget(CombatHealth target)
    {
        if (spriteRenderer == null || target == null)
        {
            return;
        }
        Vector2 direction = target.GetClosestPoint(transform.position) - (Vector2)transform.position;
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }
    }

    private void EnsureMainHouseCanBeAttacked()
    {
        if (mainHouse == null)
        {
            return;
        }
        CombatHealth health = GetHealth(mainHouse);
        if (health == null)
        {
            health = mainHouse.gameObject.AddComponent<CombatHealth>();
            health.ConfigureRuntime(CombatTeam.Player, 250f);
        }
    }

    private static CombatHealth GetHealth(Transform target)
    {
        return target == null ? null : target.GetComponentInParent<CombatHealth>();
    }

    private void SetMoveSpeed(float value)
    {
        animator?.SetFloat(MoveSpeedHash, value);
    }

    private static Vector3 ToNavPosition(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, 0f, worldPosition.y);
    }

    private static Vector2 ToWorldPosition(Vector3 navPosition)
    {
        return new Vector2(navPosition.x, navPosition.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
