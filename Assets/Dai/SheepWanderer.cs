using UnityEngine;

/// <summary>
/// Basic ambient behaviour for the sheep asset.  It intentionally has no
/// combat or death logic because the current sheep art contains no such clips.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SheepWanderer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D sheepRigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Wandering")]
    [SerializeField, Min(0f)] private float wanderRadius = 2.5f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.1f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.08f;
    [SerializeField, Min(0f)] private float minIdleDuration = 0.7f;
    [SerializeField, Min(0f)] private float maxIdleDuration = 1.6f;

    [Header("Grazing")]
    [SerializeField, Min(0f)] private float minGrazingDuration = 1.5f;
    [SerializeField, Min(0f)] private float maxGrazingDuration = 3f;

    [Header("Visuals")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private enum SheepState
    {
        Idle,
        Wandering,
        Grazing
    }

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing");

    private SheepState currentState;
    private Vector2 wanderOrigin;
    private Vector2 destination;
    private Vector2 desiredVelocity;
    private float stateTimer;

    private void Awake()
    {
        if (sheepRigidbody == null)
        {
            sheepRigidbody = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        maxIdleDuration = Mathf.Max(maxIdleDuration, minIdleDuration);
        maxGrazingDuration = Mathf.Max(maxGrazingDuration, minGrazingDuration);
        wanderOrigin = sheepRigidbody.position;
        BeginIdle();
    }

    private void Update()
    {
        switch (currentState)
        {
            case SheepState.Idle:
                UpdateIdle();
                break;

            case SheepState.Wandering:
                UpdateWandering();
                break;

            case SheepState.Grazing:
                UpdateGrazing();
                break;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        sheepRigidbody.linearVelocity = desiredVelocity;
    }

    private void UpdateIdle()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            BeginWandering();
        }
    }

    private void UpdateWandering()
    {
        Vector2 toDestination = destination - sheepRigidbody.position;

        if (toDestination.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            BeginGrazing();
            return;
        }

        Vector2 direction = toDestination.normalized;
        desiredVelocity = direction * moveSpeed;

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            bool movingLeft = direction.x < 0f;
            spriteRenderer.flipX = spriteFacesRightByDefault ? movingLeft : !movingLeft;
        }
    }

    private void UpdateGrazing()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            BeginIdle();
        }
    }

    private void BeginIdle()
    {
        currentState = SheepState.Idle;
        desiredVelocity = Vector2.zero;
        stateTimer = Random.Range(minIdleDuration, maxIdleDuration);
    }

    private void BeginWandering()
    {
        currentState = SheepState.Wandering;
        destination = wanderOrigin + Random.insideUnitCircle * wanderRadius;
    }

    private void BeginGrazing()
    {
        currentState = SheepState.Grazing;
        desiredVelocity = Vector2.zero;
        stateTimer = Random.Range(minGrazingDuration, maxGrazingDuration);
    }

    private void UpdateAnimator()
    {
        animator.SetBool(IsMovingHash, currentState == SheepState.Wandering);
        animator.SetBool(IsGrazingHash, currentState == SheepState.Grazing);
    }

    private void OnDisable()
    {
        if (sheepRigidbody != null)
        {
            sheepRigidbody.linearVelocity = Vector2.zero;
        }
    }
}
