using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(HitboxController))]
public class EnemyAI : MonoBehaviour, IParryable
{
    public enum EnemyState { Idle, Patrol, Chase, Attack, Stagger, Dead }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float fieldOfView = 120f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackWindup = 0.4f;
    [SerializeField] private float attackDuration = 0.3f;

    public EnemyState CurrentState { get; private set; }

    protected NavMeshAgent agent;
    protected HealthSystem health;
    protected HitboxController hitbox;
    protected Animator animator;
    protected Transform player;

    private int patrolIndex;
    private bool attackOnCooldown;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<HealthSystem>();
        hitbox = GetComponent<HitboxController>();
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void OnEnable()
    {
        health.OnDeath.AddListener(HandleDeath);
        health.OnDamageTaken.AddListener(HandleDamageTaken);
    }

    protected virtual void OnDisable()
    {
        health.OnDeath.RemoveListener(HandleDeath);
        health.OnDamageTaken.RemoveListener(HandleDamageTaken);
    }

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        SetState(EnemyState.Idle);
        StartCoroutine(StateMachine());
    }

    private IEnumerator StateMachine()
    {
        while (CurrentState != EnemyState.Dead)
        {
            yield return CurrentState switch
            {
                EnemyState.Idle => IdleRoutine(),
                EnemyState.Patrol => PatrolRoutine(),
                EnemyState.Chase => ChaseRoutine(),
                EnemyState.Attack => AttackRoutine(),
                EnemyState.Stagger => new WaitForSeconds(0.1f),
                _ => null
            };
        }
    }

    private IEnumerator IdleRoutine()
    {
        agent.isStopped = true;
        yield return new WaitForSeconds(1f);

        if (patrolPoints.Length > 0)
            SetState(EnemyState.Patrol);
    }

    private IEnumerator PatrolRoutine()
    {
        if (patrolPoints.Length == 0)
        {
            SetState(EnemyState.Idle);
            yield break;
        }

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[patrolIndex].position);

        while (agent.remainingDistance > 0.5f || !agent.hasPath)
        {
            if (CanSeePlayer()) { SetState(EnemyState.Chase); yield break; }
            yield return null;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        yield return new WaitForSeconds(patrolWaitTime);
    }

    private IEnumerator ChaseRoutine()
    {
        agent.isStopped = false;

        while (true)
        {
            if (player == null || health.IsDead) yield break;

            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= attackRange && !attackOnCooldown)
            {
                SetState(EnemyState.Attack);
                yield break;
            }

            if (dist > detectionRange * 1.5f)
            {
                SetState(EnemyState.Patrol);
                yield break;
            }

            agent.SetDestination(player.position);
            yield return null;
        }
    }

    protected virtual IEnumerator AttackRoutine()
    {
        agent.isStopped = true;
        SetState(EnemyState.Attack);

        // Face player
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        animator?.SetTrigger(HashAttack);
        yield return new WaitForSeconds(attackWindup);

        hitbox.EnableHitbox(attackDamage, false);
        yield return new WaitForSeconds(attackDuration);
        hitbox.DisableHitbox();

        attackOnCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        attackOnCooldown = false;

        SetState(EnemyState.Chase);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;
        if (dist > detectionRange) return false;
        if (Vector3.Angle(transform.forward, dir) > fieldOfView * 0.5f) return false;
        return !Physics.Raycast(transform.position + Vector3.up, dir.normalized, dist);
    }

    private void HandleDamageTaken(float damage, bool wasParried)
    {
        if (CurrentState == EnemyState.Dead) return;

        if (!wasParried)
            StartCoroutine(StaggerRoutine(0.5f));
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        agent.isStopped = true;
        hitbox.DisableHitbox();
        SetState(EnemyState.Stagger);
        animator?.SetTrigger(HashStagger);
        yield return new WaitForSeconds(duration);

        if (CurrentState != EnemyState.Dead)
            SetState(player != null ? EnemyState.Chase : EnemyState.Patrol);
    }

    private void HandleDeath()
    {
        SetState(EnemyState.Dead);
        agent.isStopped = true;
        hitbox.DisableHitbox();
        animator?.SetTrigger(HashDead);
        StopAllCoroutines();
        enabled = false;
    }

    public virtual void OnParried()
    {
        hitbox.DisableHitbox();
        StartCoroutine(StaggerRoutine(2f));
    }

    protected void SetState(EnemyState state)
    {
        CurrentState = state;
        if (animator == null) return;
        float speed = state == EnemyState.Chase ? agent.speed : 0f;
        animator.SetFloat(HashSpeed, speed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
