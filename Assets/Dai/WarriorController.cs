using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class WarriorController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D warriorRigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 4f;

    [SerializeField]
    private bool spriteFacesRightByDefault = true;

    [Header("Attack Combo")]
  
    [SerializeField, Min(0.01f)]
    private float attack1Duration = 0.5f;

    [SerializeField, Min(0.01f)]
    private float attack2Duration = 0.55f;

    [SerializeField, Min(0.01f)]
    private float comboResetTime = 1f;
    [SerializeField]
    private bool stopMovingWhileAttacking = true;

    private bool stopMovingWhileGuarding = true;

    private Vector2 moveInput;

    private bool isAttacking;
    private bool isGuarding;
    private int nextComboStep;

    private float lastAttackFinishedTime =
        float.NegativeInfinity;

    private Coroutine attackCoroutine;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int Attack1Hash =
        Animator.StringToHash("Attack1");

    private static readonly int Attack2Hash =
        Animator.StringToHash("Attack2");

    private static readonly int IsGuardingHash =
        Animator.StringToHash("IsGuarding");

    private void Awake()
    {
        if (warriorRigidbody == null)
        {
            warriorRigidbody =
                GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        attack1Duration =
            Mathf.Max(0.01f, attack1Duration);

        attack2Duration =
            Mathf.Max(0.01f, attack2Duration);

        comboResetTime =
            Mathf.Max(0.01f, comboResetTime);
    }

    private void Update()
    {
        moveInput =
            Vector2.ClampMagnitude(
                ReadMovementInput(),
                1f
            );

        UpdateGuard();

        if (WasAttackPressed())
        {
            TryAttack();
        }

        UpdateFacingDirection();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Vector2 velocity =
            moveInput * moveSpeed;

        if (isAttacking &&
            stopMovingWhileAttacking)
        {
            velocity = Vector2.zero;
        }

        if (isGuarding &&
            stopMovingWhileGuarding)
        {
            velocity = Vector2.zero;
        }

        warriorRigidbody.linearVelocity =
            velocity;
    }

    private void UpdateGuard()
    {
        bool guardButtonHeld =
            IsGuardButtonHeld();

        isGuarding =
            guardButtonHeld &&
            !isAttacking;
    }

    private void TryAttack()
    {
        if (isAttacking || isGuarding)
        {
            return;
        }

        if (Time.time >
            lastAttackFinishedTime + comboResetTime)
        {
            nextComboStep = 0;
        }

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine =
            StartCoroutine(
                AttackRoutine(nextComboStep)
            );
    }

    private IEnumerator AttackRoutine(int comboStep)
    {
        isAttacking = true;

        animator.SetBool(
            IsGuardingHash,
            false
        );

        animator.SetBool(
            IsMovingHash,
            false
        );

        warriorRigidbody.linearVelocity =
            Vector2.zero;

        float currentAttackDuration;

        if (comboStep == 0)
        {
            animator.ResetTrigger(Attack2Hash);
            animator.ResetTrigger(Attack1Hash);
            animator.SetTrigger(Attack1Hash);

            currentAttackDuration =
                attack1Duration;

        
            nextComboStep = 1;
        }
        else
        {
            animator.ResetTrigger(Attack1Hash);
            animator.ResetTrigger(Attack2Hash);
            animator.SetTrigger(Attack2Hash);

            currentAttackDuration =
                attack2Duration;

          
            nextComboStep = 0;
        }

        yield return new WaitForSeconds(
            currentAttackDuration
        );

        isAttacking = false;

        lastAttackFinishedTime =
            Time.time;

        attackCoroutine = null;
    }

    private void UpdateAnimator()
    {
        bool isMoving =
            moveInput.sqrMagnitude > 0.01f;

        if (isAttacking &&
            stopMovingWhileAttacking)
        {
            isMoving = false;
        }

        if (isGuarding &&
            stopMovingWhileGuarding)
        {
            isMoving = false;
        }

        animator.SetBool(
            IsMovingHash,
            isMoving
        );

        animator.SetBool(
            IsGuardingHash,
            isGuarding
        );
    }

    private void UpdateFacingDirection()
    {
       
        if (isAttacking)
        {
            return;
        }

        if (Mathf.Abs(moveInput.x) <= 0.01f)
        {
            return;
        }

        bool movingLeft =
            moveInput.x < 0f;

        spriteRenderer.flipX =
            spriteFacesRightByDefault
                ? movingLeft
                : !movingLeft;
    }

    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        isGuarding = false;

        if (warriorRigidbody != null)
        {
            warriorRigidbody.linearVelocity =
                Vector2.zero;
        }
    }

    private Vector2 ReadMovementInput()
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

    private bool WasAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM

        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey
                .wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame;

        return spacePressed || mousePressed;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0);

#else

        return false;

#endif
    }

    private bool IsGuardButtonHeld()
    {
#if ENABLE_INPUT_SYSTEM

        bool fHeld =
            Keyboard.current != null &&
            Keyboard.current.fKey.isPressed;

        bool rightMouseHeld =
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed;

        return fHeld || rightMouseHeld;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return
            Input.GetKey(KeyCode.F) ||
            Input.GetMouseButton(1);

#else

        return false;

#endif
    }
}