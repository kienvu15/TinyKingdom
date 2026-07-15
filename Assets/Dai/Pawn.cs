using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PawnEquipmentAnimator))]
public class Pawn : MonoBehaviour
{
    [Header("Components")]

    [SerializeField]
    private Rigidbody2D pawnRigidbody;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private PawnEquipmentAnimator equipmentAnimator;

    [Header("Movement")]

    [SerializeField]
    [Min(0f)]
    private float moveSpeed = 4f;

    private bool spriteFacesRightByDefault = true;

    [Header("Interaction")]
    [SerializeField]
    [Min(0.01f)]
    private float interactDuration = 0.5f;
    [SerializeField]
    [Min(0.01f)]
    private float interactCooldown = 0.55f;
    [SerializeField]
    private bool stopMovingWhileInteracting = true;

    [Header("Testing")]
    [SerializeField]
    private bool enableEquipmentTestKeys = true;

    private Vector2 moveInput;

    private bool isInteracting;
    private float nextInteractTime;

    private Coroutine interactCoroutine;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int InteractHash =
        Animator.StringToHash("Interact");

    private void Awake()
    {
        if (pawnRigidbody == null)
        {
            pawnRigidbody =
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

        if (equipmentAnimator == null)
        {
            equipmentAnimator =
                GetComponent<PawnEquipmentAnimator>();
        }

        interactDuration =
            Mathf.Max(
                0.01f,
                interactDuration
            );

        interactCooldown =
            Mathf.Max(
                0.01f,
                interactCooldown
            );
    }

    private void Update()
    {
        moveInput =
            Vector2.ClampMagnitude(
                ReadMovementInput(),
                1f
            );

        UpdateFacingDirection();

        if (WasInteractPressed())
        {
            TryInteract();
        }

        if (enableEquipmentTestKeys)
        {
            ReadEquipmentTestKeys();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Vector2 velocity =
            moveInput * moveSpeed;

        if (
            isInteracting &&
            stopMovingWhileInteracting
        )
        {
            velocity =
                Vector2.zero;
        }

        pawnRigidbody.linearVelocity =
            velocity;
    }

    private void UpdateAnimator()
    {
        bool isMoving =
            moveInput.sqrMagnitude > 0.01f;

        if (
            isInteracting &&
            stopMovingWhileInteracting
        )
        {
            isMoving = false;
        }

        animator.SetBool(
            IsMovingHash,
            isMoving
        );
    }

    private void UpdateFacingDirection()
    {
        if (isInteracting)
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

    private void TryInteract()
    {
        if (isInteracting)
        {
            return;
        }

        if (Time.time < nextInteractTime)
        {
            return;
        }

        if (!equipmentAnimator.CanInteract)
        {
            Debug.Log(
                "Pawn cần cầm Axe, Hammer, Knife " +
                "hoặc Pickaxe để chạy Interact.",
                this
            );

            return;
        }

        nextInteractTime =
            Time.time + interactCooldown;

        if (interactCoroutine != null)
        {
            StopCoroutine(
                interactCoroutine
            );
        }

        interactCoroutine =
            StartCoroutine(
                InteractRoutine()
            );
    }

    private IEnumerator InteractRoutine()
    {
        isInteracting = true;

        if (stopMovingWhileInteracting)
        {
            pawnRigidbody.linearVelocity =
                Vector2.zero;
        }

        animator.SetBool(
            IsMovingHash,
            false
        );

        animator.ResetTrigger(
            InteractHash
        );

        animator.SetTrigger(
            InteractHash
        );

        yield return new WaitForSeconds(
            interactDuration
        );

        isInteracting = false;
        interactCoroutine = null;
    }

    /// <summary>
    /// Các phím số chỉ dùng để kiểm tra hệ thống.
    /// Sau này khi Pawn nhặt đồ, gọi các hàm Equip tương ứng.
    /// </summary>
    private void ReadEquipmentTestKeys()
    {
        if (isInteracting)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
        {
            return;
        }

        if (
            Keyboard.current.digit1Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.EquipNothing();
        }
        else if (
            Keyboard.current.digit2Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.EquipAxe();
        }
        else if (
            Keyboard.current.digit3Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.EquipHammer();
        }
        else if (
            Keyboard.current.digit4Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.EquipKnife();
        }
        else if (
            Keyboard.current.digit5Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.EquipPickaxe();
        }
        else if (
            Keyboard.current.digit6Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.CarryGold();
        }
        else if (
            Keyboard.current.digit7Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.CarryMeat();
        }
        else if (
            Keyboard.current.digit8Key
                .wasPressedThisFrame
        )
        {
            equipmentAnimator.CarryWood();
        }

#elif ENABLE_LEGACY_INPUT_MANAGER

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            equipmentAnimator.EquipNothing();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            equipmentAnimator.EquipAxe();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            equipmentAnimator.EquipHammer();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            equipmentAnimator.EquipKnife();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            equipmentAnimator.EquipPickaxe();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            equipmentAnimator.CarryGold();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            equipmentAnimator.CarryMeat();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            equipmentAnimator.CarryWood();
        }

#endif
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

        if (
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed
        )
        {
            horizontal -= 1f;
        }

        if (
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed
        )
        {
            horizontal += 1f;
        }

        if (
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed
        )
        {
            vertical -= 1f;
        }

        if (
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed
        )
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

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
        {
            return false;
        }

        return
            Keyboard.current.eKey
                .wasPressedThisFrame ||
            Keyboard.current.spaceKey
                .wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return
            Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space);

#else

        return false;

#endif
    }

    private void OnDisable()
    {
        if (interactCoroutine != null)
        {
            StopCoroutine(
                interactCoroutine
            );

            interactCoroutine = null;
        }

        isInteracting = false;

        if (pawnRigidbody != null)
        {
            pawnRigidbody.linearVelocity =
                Vector2.zero;
        }
    }
}