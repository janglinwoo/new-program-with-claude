using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(StaminaSystem))]
[RequireComponent(typeof(HitboxController))]
public class PlayerCombat : MonoBehaviour, IParryable
{
    [Header("Attack Stats")]
    [SerializeField] private float lightAttackDamage = 30f;
    [SerializeField] private float heavyAttackDamage = 60f;
    [SerializeField] private float lightAttackDuration = 0.5f;
    [SerializeField] private float heavyAttackDuration = 0.9f;
    [SerializeField] private float comboWindow = 0.4f;
    [SerializeField] private int maxComboCount = 3;

    [Header("Parry")]
    [SerializeField] private float parryWindow = 0.3f;
    [SerializeField] private float parryCounterDamage = 50f;
    [SerializeField] private float parryCooldown = 0.8f;

    [Header("Block")]
    [SerializeField] private float blockDamageReduction = 0.7f;

    public bool IsParrying { get; private set; }
    public bool IsBlocking { get; private set; }

    private PlayerController controller;
    private StaminaSystem stamina;
    private HitboxController hitbox;
    private Animator animator;

    private int comboCount;
    private bool canCombo;
    private bool canParry = true;
    private Coroutine attackRoutine;

    private static readonly int HashLightAttack = Animator.StringToHash("LightAttack");
    private static readonly int HashHeavyAttack = Animator.StringToHash("HeavyAttack");
    private static readonly int HashParry = Animator.StringToHash("Parry");
    private static readonly int HashBlock = Animator.StringToHash("Block");
    private static readonly int HashComboIndex = Animator.StringToHash("ComboIndex");

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stamina = GetComponent<StaminaSystem>();
        hitbox = GetComponent<HitboxController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (controller.CurrentState == PlayerController.PlayerState.Dead) return;

        HandleAttackInput();
        HandleBlockInput();
    }

    private void HandleAttackInput()
    {
        bool inAttack = controller.CurrentState == PlayerController.PlayerState.Attacking;

        if (Input.GetMouseButtonDown(0))
        {
            if (inAttack && canCombo)
                QueueCombo(false);
            else if (!inAttack)
                StartAttack(false);
        }

        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftControl))
        {
            if (!inAttack)
                StartAttack(true);
        }
    }

    private void HandleBlockInput()
    {
        if (controller.CurrentState == PlayerController.PlayerState.Attacking ||
            controller.CurrentState == PlayerController.PlayerState.Dodging) return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetMouseButtonDown(1) && canParry)
            {
                StartCoroutine(ParryRoutine());
                return;
            }

            if (!IsBlocking)
                StartBlock();
        }
        else if (IsBlocking)
        {
            StopBlock();
        }
    }

    private void StartAttack(bool isHeavy)
    {
        float cost = isHeavy ? stamina.heavyAttackCost : stamina.lightAttackCost;
        if (!stamina.TryConsume(cost)) return;
        if (IsBlocking) StopBlock();

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackRoutine(isHeavy));
    }

    private void QueueCombo(bool isHeavy)
    {
        if (comboCount >= maxComboCount) return;
        float cost = isHeavy ? stamina.heavyAttackCost : stamina.lightAttackCost;
        if (!stamina.TryConsume(cost)) return;

        canCombo = false;
        comboCount++;
        animator?.SetInteger(HashComboIndex, comboCount);

        if (isHeavy)
            animator?.SetTrigger(HashHeavyAttack);
        else
            animator?.SetTrigger(HashLightAttack);
    }

    private IEnumerator AttackRoutine(bool isHeavy)
    {
        controller.SetState(PlayerController.PlayerState.Attacking);
        comboCount = 1;
        canCombo = false;

        float damage = isHeavy ? heavyAttackDamage : lightAttackDamage;
        float duration = isHeavy ? heavyAttackDuration : lightAttackDuration;

        animator?.SetInteger(HashComboIndex, comboCount);
        if (isHeavy)
            animator?.SetTrigger(HashHeavyAttack);
        else
            animator?.SetTrigger(HashLightAttack);

        // Hitbox opens at ~30% into animation
        yield return new WaitForSeconds(duration * 0.3f);
        hitbox.EnableHitbox(damage);

        yield return new WaitForSeconds(duration * 0.3f);
        hitbox.DisableHitbox();

        // Combo window
        canCombo = true;
        yield return new WaitForSeconds(comboWindow);
        canCombo = false;

        comboCount = 0;
        controller.SetState(PlayerController.PlayerState.Idle);
        attackRoutine = null;
    }

    private IEnumerator ParryRoutine()
    {
        canParry = false;
        IsParrying = true;
        controller.SetState(PlayerController.PlayerState.Blocking);
        animator?.SetTrigger(HashParry);

        yield return new WaitForSeconds(parryWindow);
        IsParrying = false;

        yield return new WaitForSeconds(parryCooldown - parryWindow);
        canParry = true;
        controller.SetState(PlayerController.PlayerState.Idle);
    }

    private void StartBlock()
    {
        IsBlocking = true;
        stamina.SetBlocking(true);
        controller.SetState(PlayerController.PlayerState.Blocking);
        animator?.SetBool(HashBlock, true);
    }

    private void StopBlock()
    {
        IsBlocking = false;
        stamina.SetBlocking(false);
        controller.SetState(PlayerController.PlayerState.Idle);
        animator?.SetBool(HashBlock, false);
    }

    // Called when this character's attack is parried by someone
    public void OnParried()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        hitbox.DisableHitbox();
        controller.ApplyStagger(1.5f);
    }
}
