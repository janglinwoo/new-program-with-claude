using UnityEngine;
using UnityEngine.Events;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float recoveryRate = 20f;
    [SerializeField] private float recoveryDelay = 1.5f;

    [Header("Costs")]
    public float lightAttackCost = 15f;
    public float heavyAttackCost = 25f;
    public float dodgeCost = 25f;
    public float blockCostPerHit = 10f;
    public float blockDrainRate = 5f;

    public UnityEvent<float, float> OnStaminaChanged;

    private float currentStamina;
    private float recoveryTimer;
    private bool isBlocking;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaRatio => currentStamina / maxStamina;
    public bool IsExhausted => currentStamina <= 0f;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleRecovery();

        if (isBlocking)
        {
            Consume(blockDrainRate * Time.deltaTime);
        }
    }

    private void HandleRecovery()
    {
        if (recoveryTimer > 0f)
        {
            recoveryTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina && !isBlocking)
        {
            currentStamina = Mathf.Clamp(currentStamina + recoveryRate * Time.deltaTime, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public bool TryConsume(float amount)
    {
        if (currentStamina < amount) return false;
        Consume(amount);
        return true;
    }

    public void Consume(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
        recoveryTimer = recoveryDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void SetBlocking(bool value)
    {
        isBlocking = value;
    }
}
