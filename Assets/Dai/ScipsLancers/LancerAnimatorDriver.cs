using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class LancerAnimatorDriver : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int DirectionHash =
        Animator.StringToHash("Direction");

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
    }
    public void SetMovement(Vector2 movement)
    {
        bool isMoving =
            movement.sqrMagnitude > 0.01f;

        animator.SetBool(
            IsMovingHash,
            isMoving
        );

        if (isMoving)
        {
            SetLookDirection(movement);
        }
    }
    public void SetLookDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.Normalize();

        int directionIndex =
            GetDirectionIndex(direction);

        animator.SetInteger(
            DirectionHash,
            directionIndex
        );

        if (Mathf.Abs(direction.x) > 0.05f)
        {
            spriteRenderer.flipX =
                direction.x < 0f;
        }
    }
    public void PlayAttack(Vector2 attackDirection)
    {
        if (attackDirection.sqrMagnitude < 0.001f)
        {
            attackDirection = Vector2.down;
        }

        SetLookDirection(attackDirection);

        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
    }
    private int GetDirectionIndex(Vector2 direction)
    {
        direction.Normalize();

        float absoluteX =
            Mathf.Abs(direction.x);

        if (direction.y <= -0.45f)
        {
            if (absoluteX > 0.35f)
            {
                return 1;
            }

            return 0;
        }

        if (direction.y >= 0.45f)
        {

            if (absoluteX > 0.35f)
            {
                return 3;
            }

            return 4;
        }
        return 2;
    }
}