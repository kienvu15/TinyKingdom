using UnityEngine;

public enum CombatTeam
{
    Neutral,
    Player,
    Enemy,
}

/// <summary>
/// A lightweight health component for every attackable unit or building.
/// Attach it to the root object that owns the collider.  Enemy AI only selects
/// components whose team differs from its own.
/// </summary>
[DisallowMultipleComponent]
public sealed class CombatHealth : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private CombatTeam team = CombatTeam.Player;

    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private bool destroyWhenDefeated = true;
    [SerializeField, Min(0f)] private float destroyDelay = 0.08f;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitTint = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.1f;
    [SerializeField, Min(1f)] private float hitScale = 1.08f;
    [SerializeField] private GameObject hitEffectPrefab;

    private Collider2D primaryCollider;
    private SpriteRenderer[] spriteRenderers;
    private Color[] defaultColors;
    private Vector3 defaultScale;
    private float currentHealth;
    private float feedbackEndTime;
    private bool hasFeedback;
    private bool isDefeated;

    public CombatTeam Team => team;
    public bool IsAlive => !isDefeated && isActiveAndEnabled;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        primaryCollider = GetComponentInChildren<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        defaultColors = new Color[spriteRenderers.Length];
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            defaultColors[index] = spriteRenderers[index].color;
        }
        defaultScale = transform.localScale;
    }

    private void Update()
    {
        if (hasFeedback && Time.time >= feedbackEndTime)
        {
            RestoreFeedback();
        }
    }

    private void OnDisable()
    {
        RestoreFeedback();
    }

    public void ConfigureRuntime(CombatTeam newTeam, float newMaxHealth)
    {
        team = newTeam;
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth;
    }

    public bool CanBeAttackedBy(CombatTeam attacker)
    {
        return IsAlive && team != CombatTeam.Neutral && team != attacker;
    }

    public Vector2 GetClosestPoint(Vector2 fromPosition)
    {
        return primaryCollider != null
            ? primaryCollider.ClosestPoint(fromPosition)
            : transform.position;
    }

    public bool TakeDamage(float damage, Vector2 hitPosition)
    {
        if (!IsAlive || damage <= 0f)
        {
            return false;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        PlayHitFeedback(hitPosition);
        if (currentHealth > 0f)
        {
            return true;
        }

        isDefeated = true;
        DisableColliders();
        if (destroyWhenDefeated)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
        return true;
    }

    public static bool TryGetTarget(Collider2D collider, out CombatHealth health)
    {
        health = collider == null ? null : collider.GetComponentInParent<CombatHealth>();
        if (health != null)
        {
            return true;
        }

        // Existing controllable unit prefabs can participate without a manual
        // migration.  Buildings should receive CombatHealth in their prefab.
        if (collider == null || !LooksLikePlayerUnit(collider.transform.root.gameObject))
        {
            return false;
        }

        health = collider.transform.root.gameObject.AddComponent<CombatHealth>();
        health.ConfigureRuntime(CombatTeam.Player, 100f);
        return true;
    }

    private void PlayHitFeedback(Vector2 hitPosition)
    {
        feedbackEndTime = Time.time + hitFlashDuration;
        hasFeedback = true;
        transform.localScale = defaultScale * hitScale;
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            if (spriteRenderers[index] != null)
            {
                spriteRenderers[index].color = hitTint;
            }
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, GetClosestPoint(hitPosition), Quaternion.identity);
        }
    }

    private void RestoreFeedback()
    {
        hasFeedback = false;
        transform.localScale = defaultScale;
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            if (spriteRenderers[index] != null)
            {
                spriteRenderers[index].color = defaultColors[index];
            }
        }
    }

    private void DisableColliders()
    {
        foreach (Collider2D targetCollider in GetComponentsInChildren<Collider2D>())
        {
            targetCollider.enabled = false;
        }
    }

    private static bool LooksLikePlayerUnit(GameObject target)
    {
        return target.GetComponent("WarriorController") != null
               || target.GetComponent("TopDownArcherController") != null
               || target.GetComponent("ArcherController") != null
               || target.GetComponent("Lancer") != null
               || target.GetComponent("MonkHeal") != null
               || target.GetComponent("Pawn") != null;
    }
}
