using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaSystem))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(LockOnSystem))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Idle, Running, Dodging, Attacking, Blocking, Staggered, Dead }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity = -20f;

    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeDuration = 0.4f;
    [SerializeField] private float invincibleDuration = 0.25f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    public PlayerState CurrentState { get; private set; }
    public bool IsInvincible { get; private set; }

    private CharacterController controller;
    private StaminaSystem stamina;
    private HealthSystem health;
    private LockOnSystem lockOn;
    private Animator animator;

    private Vector3 velocity;
    private Vector3 moveInput;
    private bool canAct = true;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashDodge = Animator.StringToHash("Dodge");
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<StaminaSystem>();
        health = GetComponent<HealthSystem>();
        lockOn = GetComponent<LockOnSystem>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        health.OnDeath.AddListener(HandleDeath);
        health.OnDamageTaken.AddListener(HandleDamageTaken);
    }

    private void OnDisable()
    {
        health.OnDeath.RemoveListener(HandleDeath);
        health.OnDamageTaken.RemoveListener(HandleDamageTaken);
    }

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        HandleInput();
        ApplyGravity();
        UpdateAnimator();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0f, v).normalized;

        if (Input.GetKeyDown(KeyCode.F))
            lockOn.ToggleLockOn();

        if (lockOn.IsLockedOn && Input.GetKeyDown(KeyCode.Q))
            lockOn.SwitchTarget(-1f);
        if (lockOn.IsLockedOn && Input.GetKeyDown(KeyCode.E))
            lockOn.SwitchTarget(1f);

        if (canAct)
        {
            if (Input.GetKeyDown(KeyCode.Space) && moveInput != Vector3.zero)
                TryDodge();
        }

        if (CurrentState != PlayerState.Dodging && CurrentState != PlayerState.Attacking)
            Move();
    }

    private void Move()
    {
        if (moveInput == Vector3.zero)
        {
            SetState(PlayerState.Idle);
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 worldMove = (camForward * moveInput.z + camRight * moveInput.x).normalized;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        controller.Move(worldMove * speed * Time.deltaTime);
        SetState(PlayerState.Running);

        if (lockOn.IsLockedOn && lockOn.Target != null)
        {
            Vector3 lookDir = lockOn.Target.position - transform.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(worldMove), rotationSpeed * Time.deltaTime);
        }
    }

    private void TryDodge()
    {
        if (!stamina.TryConsume(stamina.dodgeCost)) return;
        StartCoroutine(DodgeRoutine());
    }

    private IEnumerator DodgeRoutine()
    {
        SetState(PlayerState.Dodging);
        canAct = false;
        animator?.SetTrigger(HashDodge);

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        Vector3 dir = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        float elapsed = 0f;
        float invincibleEnd = invincibleDuration;
        IsInvincible = true;

        while (elapsed < dodgeDuration)
        {
            float t = elapsed / dodgeDuration;
            float speed = Mathf.Lerp(dodgeDistance / dodgeDuration, 0f, t);
            controller.Move(dir * speed * Time.deltaTime);
            elapsed += Time.deltaTime;

            if (elapsed >= invincibleEnd)
                IsInvincible = false;

            yield return null;
        }

        IsInvincible = false;
        SetState(PlayerState.Idle);
        canAct = true;
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        float targetSpeed = CurrentState == PlayerState.Running ? 1f : 0f;
        float current = animator.GetFloat(HashSpeed);
        animator.SetFloat(HashSpeed, Mathf.Lerp(current, targetSpeed, Time.deltaTime * 10f));
    }

    public void SetState(PlayerState state)
    {
        CurrentState = state;
    }

    public void ApplyStagger(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(StaggerRoutine(duration));
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        SetState(PlayerState.Staggered);
        canAct = false;
        animator?.SetTrigger(HashStagger);
        yield return new WaitForSeconds(duration);
        SetState(PlayerState.Idle);
        canAct = true;
    }

    private void HandleDamageTaken(float damage, bool wasParried)
    {
        if (IsInvincible) return;
        if (!wasParried && CurrentState != PlayerState.Blocking)
            ApplyStagger(0.6f);
    }

    private void HandleDeath()
    {
        SetState(PlayerState.Dead);
        canAct = false;
        animator?.SetTrigger(HashDead);
    }
}
