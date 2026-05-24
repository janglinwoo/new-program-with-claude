using System.Collections;
using UnityEngine;

// BossAI extends EnemyAI with:
//   - Phase 2 triggered at 50% HP
//   - Multiple attack patterns (slam, sweep, charge, rage burst)
//   - Phase 2 increases speed, aggression, and unlocks new attacks
public class BossAI : EnemyAI
{
    [Header("Boss - Phase")]
    [SerializeField] private float phase2HealthThreshold = 0.5f;
    [SerializeField] private GameObject phase2VFXPrefab;

    [Header("Boss - Attacks")]
    [SerializeField] private float slamDamage = 50f;
    [SerializeField] private float sweepDamage = 35f;
    [SerializeField] private float chargeDamage = 45f;
    [SerializeField] private float rageBurstDamage = 80f;

    [Header("Boss - Movement")]
    [SerializeField] private float phase1MoveSpeed = 3f;
    [SerializeField] private float phase2MoveSpeed = 5.5f;
    [SerializeField] private float chargeSpeed = 12f;

    private bool isPhase2;
    private bool phaseTransitionDone;

    private static readonly int HashSlam = Animator.StringToHash("Slam");
    private static readonly int HashSweep = Animator.StringToHash("Sweep");
    private static readonly int HashCharge = Animator.StringToHash("Charge");
    private static readonly int HashRageBurst = Animator.StringToHash("RageBurst");
    private static readonly int HashPhase2 = Animator.StringToHash("Phase2");

    protected override void Awake()
    {
        base.Awake();
        agent.speed = phase1MoveSpeed;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        health.OnHealthChanged.AddListener(CheckPhaseTransition);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        health.OnHealthChanged.RemoveListener(CheckPhaseTransition);
    }

    private void CheckPhaseTransition(float current, float max)
    {
        if (phaseTransitionDone || current / max > phase2HealthThreshold) return;
        phaseTransitionDone = true;
        StartCoroutine(Phase2Transition());
    }

    private IEnumerator Phase2Transition()
    {
        isPhase2 = true;
        agent.isStopped = true;

        if (phase2VFXPrefab != null)
            Instantiate(phase2VFXPrefab, transform.position, Quaternion.identity);

        animator?.SetTrigger(HashPhase2);
        yield return new WaitForSeconds(2f);

        agent.speed = phase2MoveSpeed;
        agent.isStopped = false;
    }

    protected override IEnumerator AttackRoutine()
    {
        agent.isStopped = true;

        BossAttack chosen = ChooseAttack();
        yield return StartCoroutine(ExecuteAttack(chosen));

        float cooldown = isPhase2 ? 1f : 1.8f;
        yield return new WaitForSeconds(cooldown);

        SetState(EnemyState.Chase);
        agent.isStopped = false;
    }

    private BossAttack ChooseAttack()
    {
        if (player == null) return BossAttack.Slam;

        float dist = Vector3.Distance(transform.position, player.position);

        if (isPhase2)
        {
            float roll = Random.value;
            if (roll < 0.25f) return BossAttack.RageBurst;
            if (roll < 0.5f) return dist > 4f ? BossAttack.Charge : BossAttack.Sweep;
            if (roll < 0.75f) return BossAttack.Sweep;
            return BossAttack.Slam;
        }

        return dist > 3.5f ? BossAttack.Slam : BossAttack.Sweep;
    }

    private IEnumerator ExecuteAttack(BossAttack attack)
    {
        FacePlayer();

        switch (attack)
        {
            case BossAttack.Slam:
                yield return StartCoroutine(SlamAttack());
                break;
            case BossAttack.Sweep:
                yield return StartCoroutine(SweepAttack());
                break;
            case BossAttack.Charge:
                yield return StartCoroutine(ChargeAttack());
                break;
            case BossAttack.RageBurst:
                yield return StartCoroutine(RageBurstAttack());
                break;
        }
    }

    private IEnumerator SlamAttack()
    {
        animator?.SetTrigger(HashSlam);
        yield return new WaitForSeconds(0.6f);

        // Area-of-effect slam
        var cols = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 2.5f);
        foreach (var col in cols)
        {
            var hp = col.GetComponentInParent<HealthSystem>();
            if (hp != null && hp.gameObject != gameObject)
                hp.TakeDamage(slamDamage);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator SweepAttack()
    {
        animator?.SetTrigger(HashSweep);
        yield return new WaitForSeconds(0.4f);

        hitbox.EnableHitbox(sweepDamage);
        float elapsed = 0f;
        float sweepTime = 0.5f;

        while (elapsed < sweepTime)
        {
            transform.Rotate(0f, 200f * Time.deltaTime, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        hitbox.DisableHitbox();
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator ChargeAttack()
    {
        if (player == null) yield break;

        animator?.SetTrigger(HashCharge);
        yield return new WaitForSeconds(0.5f);

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        Vector3 dir = (targetPos - startPos).normalized;

        float elapsed = 0f;
        float chargeDuration = 0.5f;
        hitbox.EnableHitbox(chargeDamage);

        while (elapsed < chargeDuration)
        {
            agent.Move(dir * chargeSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        hitbox.DisableHitbox();
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator RageBurstAttack()
    {
        animator?.SetTrigger(HashRageBurst);
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < 3; i++)
        {
            var cols = Physics.OverlapSphere(transform.position, 3f);
            foreach (var col in cols)
            {
                var hp = col.GetComponentInParent<HealthSystem>();
                if (hp != null && hp.gameObject != gameObject)
                    hp.TakeDamage(rageBurstDamage / 3f);
            }
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public override void OnParried()
    {
        hitbox.DisableHitbox();
        // Bosses have shorter stagger from parries
        StartCoroutine(BossStagger(0.8f));
    }

    private IEnumerator BossStagger(float duration)
    {
        agent.isStopped = true;
        SetState(EnemyState.Stagger);
        yield return new WaitForSeconds(duration);
        SetState(EnemyState.Chase);
        agent.isStopped = false;
    }

    private enum BossAttack { Slam, Sweep, Charge, RageBurst }
}
