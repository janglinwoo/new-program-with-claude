using UnityEngine;

// Attach to the same GameObject as the Animator.
// Animation clips call these methods via Animation Events.
public class PlayerAnimationEvents : MonoBehaviour
{
    private HitboxController hitbox;
    private PlayerCombat combat;

    private void Awake()
    {
        hitbox = GetComponentInParent<HitboxController>();
        combat = GetComponentInParent<PlayerCombat>();
    }

    // Called by animation event at the start of the hit frame
    public void OnAttackHitboxOpen()
    {
        // Damage values are set by PlayerCombat before the coroutine waits.
        // This event is an alternative activation point for precise timing.
        // If you prefer event-driven hitboxes over coroutine timing, use this.
    }

    // Called by animation event at the end of the hit frame
    public void OnAttackHitboxClose()
    {
        hitbox?.DisableHitbox();
    }

    // Called by animation event at the point the foot should land
    public void OnFootstep()
    {
        // Spawn footstep particle / play audio here
    }
}
