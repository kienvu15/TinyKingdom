using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class MonkHeal : MonoBehaviour
{
    [SerializeField] private Rigidbody2D monkRigidbody;
    [SerializeField] private Animator monkAnimator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Transform healEffectPoint;

    [SerializeField] private GameObject healEffectPrefab;

    [Header("Movement")]
    [SerializeField]
    [Min(0f)]
    private float moveSpeed = 4f;

    [Header("Heal Timing")]
    [SerializeField]
    [Min(0f)]
    private float effectSpawnDelay = 0.25f;

    [SerializeField]
    [Min(0.01f)]
    private float effectLifetime = 0.8f;

    [SerializeField]
    [Min(0.01f)]
    private float castDuration = 0.8f;

    [SerializeField]
    [Min(0.01f)]
    private float cooldown = 1f;

    [Header("Heal Behaviour")]
    [SerializeField]
    private bool stopMovingWhileCasting = true;

    [SerializeField]
    private bool effectFollowsMonk = true;

    private Vector2 moveInput;

    private bool isCasting;
    private float nextCastTime;
    private Coroutine castCoroutine;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int HealHash =
        Animator.StringToHash("Heal");

    private void Awake()
    {
        if (monkRigidbody == null)
        {
            monkRigidbody = GetComponent<Rigidbody2D>();
        }

        if (monkAnimator == null)
        {
            monkAnimator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        castDuration = Mathf.Max(
            castDuration,
            effectSpawnDelay
        );

        cooldown = Mathf.Max(
            cooldown,
            castDuration
        );
    }

    private void Update()
    {
        ReadMovementInput();

        if (WasHealPressed())
        {
            TryCastHealEffect();
        }

        UpdateAnimator();
        UpdateFacingDirection();
    }

    private void FixedUpdate()
    {
        Vector2 velocity =
            moveInput * moveSpeed;

        if (isCasting && stopMovingWhileCasting)
        {
            velocity = Vector2.zero;
        }

        monkRigidbody.linearVelocity = velocity;
    }

    private void ReadMovementInput()
    {
        moveInput = ReadKeyboardMovement();

        moveInput = Vector2.ClampMagnitude(
            moveInput,
            1f
        );
    }

    private void UpdateAnimator()
    {
        bool isMoving =
            moveInput.sqrMagnitude > 0.01f;

        if (isCasting && stopMovingWhileCasting)
        {
            isMoving = false;
        }

        monkAnimator.SetBool(
            IsMovingHash,
            isMoving
        );
    }

    private void UpdateFacingDirection()
    {
        if (isCasting)
        {
            return;
        }

        if (Mathf.Abs(moveInput.x) <= 0.01f)
        {
            return;
        }
        spriteRenderer.flipX =
            moveInput.x < 0f;
    }

    private void TryCastHealEffect()
    {
        if (isCasting)
        {
            return;
        }

        if (Time.time < nextCastTime)
        {
            return;
        }

        if (healEffectPrefab == null)
        {
            Debug.LogError(
                "MonkHeal: chưa kéo HealEffectPrefab vào script.",
                this
            );

            return;
        }

        if (healEffectPoint == null)
        {
            Debug.LogError(
                "MonkHeal: chưa kéo HealEffectPoint vào script.",
                this
            );

            return;
        }

        castCoroutine =
            StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        isCasting = true;

        nextCastTime =
            Time.time + cooldown;

        if (stopMovingWhileCasting)
        {
            monkRigidbody.linearVelocity =
                Vector2.zero;
        }

        monkAnimator.SetBool(
            IsMovingHash,
            false
        );

        monkAnimator.ResetTrigger(
            HealHash
        );

        monkAnimator.SetTrigger(
            HealHash
        );

        if (effectSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                effectSpawnDelay
            );
        }

        SpawnHealEffect();

        float remainingCastTime =
            Mathf.Max(
                0f,
                castDuration - effectSpawnDelay
            );

        if (remainingCastTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingCastTime
            );
        }

        isCasting = false;
        castCoroutine = null;
    }

    private void SpawnHealEffect()
    {
        GameObject newEffect;

        if (effectFollowsMonk)
        {
            newEffect = Instantiate(
                healEffectPrefab,
                healEffectPoint.position,
                healEffectPoint.rotation,
                healEffectPoint
            );

            newEffect.transform.localPosition =
                Vector3.zero;

            newEffect.transform.localRotation =
                Quaternion.identity;
        }
        else
        {
            newEffect = Instantiate(
                healEffectPrefab,
                healEffectPoint.position,
                healEffectPoint.rotation
            );
        }

        Destroy(
            newEffect,
            effectLifetime
        );
    }

    private void OnDisable()
    {
        if (castCoroutine != null)
        {
            StopCoroutine(castCoroutine);
            castCoroutine = null;
        }

        isCasting = false;

        if (monkRigidbody != null)
        {
            monkRigidbody.linearVelocity =
                Vector2.zero;
        }
    }

    private Vector2 ReadKeyboardMovement()
    {
#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        return new Vector2(
            horizontal,
            vertical
        );

#elif ENABLE_LEGACY_INPUT_MANAGER

        return new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

#else

        return Vector2.zero;

#endif
    }

    private bool WasHealPressed()
    {
#if ENABLE_INPUT_SYSTEM

        bool hPressed =
            Keyboard.current != null &&
            Keyboard.current.hKey
                .wasPressedThisFrame;

        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey
                .wasPressedThisFrame;

        return hPressed || spacePressed;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return
            Input.GetKeyDown(KeyCode.H) ||
            Input.GetKeyDown(KeyCode.Space);

#else

        return false;

#endif
    }
}