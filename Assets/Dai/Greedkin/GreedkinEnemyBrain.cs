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

    private GreedkinRoute route;
    private Transform mainHouse;
    private int waypointIndex;
    private float nextAttackTime;
    private bool initialized;
    private bool isDead;
    private Vector2 pendingPosition;

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
        TryInitialize();
    }

    public void PlayDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
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
        if (!initialized || isDead || navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        Transform target = GetCurrentTarget(out bool finalLeg);
        if (target == null)
        {
            SetMoveSpeed(0f);
            return;
        }

        Vector2 worldPosition = ToWorldPosition(navMeshAgent.transform.position);
        Vector2 targetPosition = GetStraightRoutePosition(target, finalLeg);
        if (finalLeg && Vector2.Distance(worldPosition, targetPosition) <= attackRange)
        {
            navMeshAgent.isStopped = true;
            SetMoveSpeed(0f);
            TryAttack();
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

    private Transform GetCurrentTarget(out bool finalLeg)
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

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;
        animator?.SetTrigger(AttackHash);
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
}
