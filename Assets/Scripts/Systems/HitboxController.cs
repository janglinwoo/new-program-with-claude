using System.Collections.Generic;
using UnityEngine;

public class HitboxController : MonoBehaviour
{
    [SerializeField] private List<Collider> hitboxes = new();

    private readonly HashSet<Collider> hitTargets = new();
    private float damage;
    private bool isParryable;

    public void EnableHitbox(float attackDamage, bool parryable = true)
    {
        damage = attackDamage;
        isParryable = parryable;
        hitTargets.Clear();
        foreach (var col in hitboxes)
            col.enabled = true;
    }

    public void DisableHitbox()
    {
        foreach (var col in hitboxes)
            col.enabled = false;
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hitTargets.Contains(other)) return;

        var health = other.GetComponentInParent<HealthSystem>();
        if (health == null || health.gameObject == transform.root.gameObject) return;

        hitTargets.Add(other);

        // Check if target is actively parrying
        var combat = other.GetComponentInParent<PlayerCombat>();
        bool wasParried = isParryable && combat != null && combat.IsParrying;

        health.TakeDamage(damage, wasParried);

        if (wasParried)
        {
            // Notify the attacker that they were parried
            GetComponentInParent<IParryable>()?.OnParried();
        }
    }
}

public interface IParryable
{
    void OnParried();
}
