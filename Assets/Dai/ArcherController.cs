using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class ArcherController : MonoBehaviour
{
   
    [SerializeField] private Rigidbody2D archerRigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform aimPivot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ArrowProjectile2D arrowPrefab;


    [SerializeField, Min(0f)] private float patrolRadius = 2.5f;


    [SerializeField, Min(0f)] private float patrolSpeed = 1.8f;

    [SerializeField, Min(0.01f)] private float patrolStoppingDistance = 0.12f;

    [SerializeField, Min(0f)] private float minPatrolWaitTime = 0.7f;

    [SerializeField, Min(0f)] private float maxPatrolWaitTime = 1.6f;


    [SerializeField, Min(0.1f)] private float detectionRadius = 5f;

    [SerializeField] private LayerMask targetLayers;

    [SerializeField] private string targetTag = "";

    [SerializeField] private Transform specificTarget;

    [SerializeField, Min(0.02f)] private float targetScanInterval = 0.15f;


    [SerializeField, Min(0f)] private float arrowSpeed = 14f;

    [SerializeField, Min(0f)] private float shootCooldown = 0.8f;

    [SerializeField, Min(0f)] private float arrowReleaseDelay = 0.18f;

    [SerializeField, Min(0f)] private float shootDuration = 0.7f;


    [SerializeField] private bool spriteFacesRightByDefault = true;

    [SerializeField] private bool showDebugCircles = true;

    private Vector2 patrolCenter;
    private Vector2 patrolDestination;
    private Vector2 desiredPatrolVelocity;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 lockedShootDirection = Vector2.right;

    private Transform currentTarget;
    private Collider2D currentTargetCollider;

    private float patrolWaitTimer;
    private float nextTargetScanTime;
    private float nextShootTime;

    private bool hasPatrolDestination;
    private bool isWaitingAtPatrolPoint;
    private bool isShooting;
    private bool waitingToReleaseArrow;

    private Coroutine shootCoroutine;

    private static readonly int IsMovingParameter =
        Animator.StringToHash("IsMoving");

    private static readonly int ShootParameter =
        Animator.StringToHash("Shoot");

    private void Awake()
    {
        FindMissingReferences();

        shootDuration = Mathf.Max(shootDuration, arrowReleaseDelay);
        maxPatrolWaitTime = Mathf.Max(maxPatrolWaitTime, minPatrolWaitTime);

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        patrolCenter = archerRigidbody.position;
        ChooseNewPatrolDestination();
    }

    private void Update()
    {
        ScanForTargetWhenNeeded();

        if (currentTarget != null)
        {
            UpdateCombat();
        }
        else
        {
            UpdatePatrol();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (archerRigidbody == null)
        {
            return;
        }

        bool mustStop =
            isShooting ||
            currentTarget != null;

        archerRigidbody.linearVelocity =
            mustStop ? Vector2.zero : desiredPatrolVelocity;
    }

    private void OnDisable()
    {
        if (archerRigidbody != null)
        {
            archerRigidbody.linearVelocity = Vector2.zero;
        }

        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }

        isShooting = false;
        waitingToReleaseArrow = false;
        currentTarget = null;
        currentTargetCollider = null;
    }

    private void FindMissingReferences()
    {
        if (archerRigidbody == null)
        {
            archerRigidbody = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void UpdatePatrol()
    {
        if (!hasPatrolDestination)
        {
            ChooseNewPatrolDestination();
        }

        Vector2 currentPosition = archerRigidbody.position;
        Vector2 toDestination = patrolDestination - currentPosition;
        float distanceToDestination = toDestination.magnitude;

        if (distanceToDestination <= patrolStoppingDistance)
        {
            desiredPatrolVelocity = Vector2.zero;

            if (!isWaitingAtPatrolPoint)
            {
                isWaitingAtPatrolPoint = true;
                patrolWaitTimer = Random.Range(
                    minPatrolWaitTime,
                    maxPatrolWaitTime
                );
            }

            patrolWaitTimer -= Time.deltaTime;

            if (patrolWaitTimer <= 0f)
            {
                ChooseNewPatrolDestination();
            }

            return;
        }

        isWaitingAtPatrolPoint = false;

        Vector2 patrolDirection = toDestination.normalized;
        desiredPatrolVelocity = patrolDirection * patrolSpeed;

        aimDirection = patrolDirection;
        ApplyAimVisual(aimDirection);
    }

    private void ChooseNewPatrolDestination()
    {
   
        Vector2 randomOffset =
            Random.insideUnitCircle * patrolRadius;

        patrolDestination = patrolCenter + randomOffset;
        hasPatrolDestination = true;
        isWaitingAtPatrolPoint = false;
        patrolWaitTimer = 0f;
    }

    private void ScanForTargetWhenNeeded()
    {
        if (Time.time < nextTargetScanTime)
        {
            if (!IsCurrentTargetStillValid())
            {
                ClearCurrentTarget();
            }

            return;
        }

        nextTargetScanTime = Time.time + targetScanInterval;

        if (specificTarget != null)
        {
            TryUseSpecificTarget();
            return;
        }

        FindNearestTargetInCircle();
    }

    private bool TryUseSpecificTarget()
    {
        if (specificTarget == null ||
            specificTarget == transform ||
            !specificTarget.gameObject.activeInHierarchy)
        {
            ClearCurrentTarget();
            return false;
        }

        float sqrDistance =
            ((Vector2)specificTarget.position -
             archerRigidbody.position).sqrMagnitude;

        if (sqrDistance > detectionRadius * detectionRadius)
        {
            if (currentTarget == specificTarget)
            {
                ClearCurrentTarget();
            }

            return false;
        }

        currentTarget = specificTarget;
        currentTargetCollider =
            specificTarget.GetComponentInChildren<Collider2D>();

        return true;
    }

    private void FindNearestTargetInCircle()
    {
        if (targetLayers.value == 0)
        {
            ClearCurrentTarget();
            return;
        }

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll(
                archerRigidbody.position,
                detectionRadius,
                targetLayers
            );

        Transform nearestTarget = null;
        Collider2D nearestCollider = null;
        float nearestSqrDistance = float.PositiveInfinity;

        foreach (Collider2D detectedCollider in detectedColliders)
        {
            if (!IsValidDetectedCollider(detectedCollider))
            {
                continue;
            }

            Vector2 targetPosition =
                detectedCollider.bounds.center;

            float sqrDistance =
                (targetPosition -
                 archerRigidbody.position).sqrMagnitude;

            if (sqrDistance >= nearestSqrDistance)
            {
                continue;
            }

            nearestSqrDistance = sqrDistance;
            nearestTarget = detectedCollider.transform;
            nearestCollider = detectedCollider;
        }

        currentTarget = nearestTarget;
        currentTargetCollider = nearestCollider;
    }

    private bool IsValidDetectedCollider(Collider2D detectedCollider)
    {
        if (detectedCollider == null ||
            !detectedCollider.gameObject.activeInHierarchy)
        {
            return false;
        }
        if (detectedCollider.transform.root == transform.root)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetTag))
        {
            return true;
        }

        return
            detectedCollider.CompareTag(targetTag) ||
            detectedCollider.transform.root.CompareTag(targetTag);
    }

    private bool IsCurrentTargetStillValid()
    {
        if (currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            return false;
        }

        float sqrDistance =
            (GetCurrentTargetPosition() -
             archerRigidbody.position).sqrMagnitude;

        return sqrDistance <= detectionRadius * detectionRadius;
    }

    private Vector2 GetCurrentTargetPosition()
    {
        if (currentTargetCollider != null)
        {
            return currentTargetCollider.bounds.center;
        }

        return currentTarget != null
            ? (Vector2)currentTarget.position
            : archerRigidbody.position;
    }

    private void ClearCurrentTarget()
    {
        currentTarget = null;
        currentTargetCollider = null;
    }
    private void UpdateCombat()
    {
        desiredPatrolVelocity = Vector2.zero;

        if (!IsCurrentTargetStillValid())
        {
            ClearCurrentTarget();
            return;
        }

        Vector2 directionToTarget =
            GetCurrentTargetPosition() -
            archerRigidbody.position;

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            aimDirection = directionToTarget.normalized;
            ApplyAimVisual(aimDirection);
        }

        TryShoot();
    }

    private void ApplyAimVisual(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        float angle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg;

        aimPivot.rotation =
            Quaternion.Euler(0f, 0f, angle);

        if (Mathf.Abs(direction.x) > 0.05f)
        {
            bool aimingLeft = direction.x < 0f;

            spriteRenderer.flipX =
                spriteFacesRightByDefault
                    ? aimingLeft
                    : !aimingLeft;
        }
    }

    private void TryShoot()
    {
        if (currentTarget == null ||
            isShooting ||
            Time.time < nextShootTime)
        {
            return;
        }

        shootCoroutine =
            StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        isShooting = true;
        waitingToReleaseArrow = true;

        lockedShootDirection = aimDirection;

        nextShootTime =
            Time.time + shootCooldown;

        ApplyAimVisual(lockedShootDirection);

        animator.ResetTrigger(ShootParameter);
        animator.SetTrigger(ShootParameter);

        if (arrowReleaseDelay > 0f)
        {
            yield return new WaitForSeconds(
                arrowReleaseDelay
            );
        }

        ReleaseArrow();

        float remainingShootTime =
            Mathf.Max(
                0f,
                shootDuration - arrowReleaseDelay
            );

        if (remainingShootTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingShootTime
            );
        }

        isShooting = false;
        shootCoroutine = null;
    }
    public void ReleaseArrow()
    {
        if (!waitingToReleaseArrow)
        {
            return;
        }

        waitingToReleaseArrow = false;

        ArrowProjectile2D newArrow =
            Instantiate(
                arrowPrefab,
                firePoint.position,
                Quaternion.identity
            );

        newArrow.Launch(
            lockedShootDirection,
            arrowSpeed,
            gameObject
        );
    }
    private void UpdateAnimator()
    {
        bool isMoving =
            currentTarget == null &&
            !isShooting &&
            desiredPatrolVelocity.sqrMagnitude > 0.01f;

        animator.SetBool(
            IsMovingParameter,
            isMoving
        );
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (archerRigidbody == null)
        {
            Debug.LogError(
                "TopDownArcherController: thiếu Rigidbody2D.",
                this
            );
            valid = false;
        }

        if (animator == null)
        {
            Debug.LogError(
                "TopDownArcherController: thiếu Animator.",
                this
            );
            valid = false;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "TopDownArcherController: thiếu SpriteRenderer.",
                this
            );
            valid = false;
        }

        if (aimPivot == null)
        {
            Debug.LogError(
                "TopDownArcherController: chưa kéo AimPivot.",
                this
            );
            valid = false;
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "TopDownArcherController: chưa kéo FirePoint.",
                this
            );
            valid = false;
        }

        if (arrowPrefab == null)
        {
            Debug.LogError(
                "TopDownArcherController: chưa kéo Arrow Prefab.",
                this
            );
            valid = false;
        }

        if (specificTarget == null && targetLayers.value == 0)
        {
            Debug.LogWarning(
                "TopDownArcherController: chưa chọn Target Layers " +
                "và cũng chưa kéo Specific Target. Archer vẫn đi tuần " +
                "nhưng sẽ không phát hiện mục tiêu.",
                this
            );
        }

        return valid;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugCircles)
        {
            return;
        }

        Vector3 center =
            Application.isPlaying
                ? (Vector3)patrolCenter
                : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, patrolRadius);

      
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

    
        if (Application.isPlaying && hasPatrolDestination)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(patrolDestination, 0.08f);
        }
    }
}