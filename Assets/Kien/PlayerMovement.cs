using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        if (movement.x != 0 || movement.y != 0)
        {
            anim.SetBool("IsMoving", true);
        }
        else
        {
            anim.SetBool("IsMoving", false);
        }

        FlipSprite();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    void FlipSprite()
    {
        if (movement.x > 0)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = false;
        }
        else if (movement.x < 0)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = true;
        }
    }
}