using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ArrowProjectile2D : MonoBehaviour
{

   
    [SerializeField]
    private float lifeTime = 3f;

   
    [SerializeField]
    private float spriteAngleOffset = 0f;

    private Rigidbody2D arrowRigidbody;
    private Collider2D[] arrowColliders;

    private Transform ownerRoot;
    private bool hasHit;

    private void Awake()
    {
        arrowRigidbody = GetComponent<Rigidbody2D>();
        arrowColliders = GetComponentsInChildren<Collider2D>();
    }

   
    public void Launch(
        Vector2 direction,
        float speed,
        GameObject owner)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        if (owner != null)
        {
            ownerRoot = owner.transform.root;
            IgnoreOwnerCollisions(owner);
        }

        RotateArrow(direction);

        arrowRigidbody.linearVelocity = direction * speed;

        Destroy(gameObject, lifeTime);
    }

    
    private void IgnoreOwnerCollisions(GameObject owner)
    {
        Collider2D[] ownerColliders =
            owner.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D arrowCollider in arrowColliders)
        {
            foreach (Collider2D ownerCollider in ownerColliders)
            {
                if (arrowCollider != null && ownerCollider != null)
                {
                    Physics2D.IgnoreCollision(
                        arrowCollider,
                        ownerCollider,
                        true
                    );
                }
            }
        }
    }

   
    private void RotateArrow(Vector2 direction)
    {
        float angle =
            Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            angle + spriteAngleOffset
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        
        if (
            ownerRoot != null
            && other.transform.root == ownerRoot
        )
        {
            return;
        }

        hasHit = true;

        arrowRigidbody.linearVelocity = Vector2.zero;

        Destroy(gameObject);
    }
}