using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LancerAnimatorDriver))]
public class Lancer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D lancerRigidbody;

    [SerializeField]
    private LancerAnimatorDriver animatorDriver;

    [Header("Movement")]
    [SerializeField]
    [Min(0f)]
    private float moveSpeed = 4f;

    [Header("Attack")]

    [SerializeField]
    [Min(0.01f)]
    private float attackDuration = 0.55f;

    [SerializeField]
    [Min(0.01f)]
    private float attackCooldown = 0.55f;

    [SerializeField]
    private bool stopMovingWhileAttacking = true;

    private Vector2 moveInput;

    private Vector2 lastLookDirection =
        Vector2.down;

    private bool isAttacking;
    private float nextAttackTime;

    private Coroutine attackCoroutine;

    private void Awake()
    {
        if (lancerRigidbody == null)
        {
            lancerRigidbody =
                GetComponent<Rigidbody2D>();
        }

        if (animatorDriver == null)
        {
            animatorDriver =
                GetComponent<LancerAnimatorDriver>();
        }

        attackCooldown =
            Mathf.Max(0.01f, attackCooldown);

        attackDuration =
            Mathf.Max(0.01f, attackDuration);
    }

    private void Update()
    {
        ReadMovement();

        if (WasAttackPressed())
        {
            TryAttack();
        }

        UpdateAnimation();
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

        lancerRigidbody.linearVelocity =
            velocity;
    }

    private void ReadMovement()
    {
        moveInput =
            ReadKeyboardMovement();
        moveInput =
            Vector2.ClampMagnitude(
                moveInput,
                1f
            );

        if (moveInput.sqrMagnitude > 0.01f &&
            !isAttacking)
        {
            lastLookDirection =
                moveInput.normalized;
        }
    }
    private void UpdateAnimation()
    {
        if (isAttacking)
        {

            animatorDriver.SetMovement(
                Vector2.zero
            );

            return;
        }

        animatorDriver.SetMovement(moveInput);

        if (moveInput.sqrMagnitude <= 0.01f)
        {
            animatorDriver.SetLookDirection(
                lastLookDirection
            );
        }
    }

    private void TryAttack()
    {
        if (isAttacking)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime =
            Time.time + attackCooldown;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine =
            StartCoroutine(AttackRoutine());
    }
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        lancerRigidbody.linearVelocity =
            Vector2.zero;

        animatorDriver.SetMovement(
            Vector2.zero
        );

        animatorDriver.PlayAttack(
            lastLookDirection
        );

        yield return new WaitForSeconds(
            attackDuration
        );

        isAttacking = false;
        attackCoroutine = null;
    }

    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;

        if (lancerRigidbody != null)
        {
            lancerRigidbody.linearVelocity =
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

        return
            spacePressed ||
            mousePressed;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0);

#else

        return false;

#endif
    }
}