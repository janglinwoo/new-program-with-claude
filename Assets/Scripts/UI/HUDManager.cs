using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Player Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;

    [Header("Boss Bar")]
    [SerializeField] private GameObject bossBarRoot;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private Text bossNameText;

    [Header("Lock-On")]
    [SerializeField] private Image lockOnReticle;

    private LockOnSystem lockOn;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<HealthSystem>();
            health?.OnHealthChanged.AddListener((cur, max) =>
                SetSlider(healthSlider, cur / max));

            var stamina = player.GetComponent<StaminaSystem>();
            stamina?.OnStaminaChanged.AddListener((cur, max) =>
                SetSlider(staminaSlider, cur / max));

            lockOn = player.GetComponent<LockOnSystem>();
        }

        bossBarRoot?.SetActive(false);
        lockOnReticle?.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        UpdateLockOnReticle();
    }

    private void UpdateLockOnReticle()
    {
        if (lockOnReticle == null || lockOn == null) return;

        if (!lockOn.IsLockedOn || lockOn.Target == null)
        {
            lockOnReticle.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(lockOn.Target.position + Vector3.up);
        if (screenPos.z < 0f)
        {
            lockOnReticle.gameObject.SetActive(false);
            return;
        }

        lockOnReticle.gameObject.SetActive(true);
        lockOnReticle.transform.position = screenPos;
    }

    public void RegisterBoss(HealthSystem bossHealth, string bossName)
    {
        bossBarRoot?.SetActive(true);
        if (bossNameText != null)
            bossNameText.text = bossName;

        bossHealth.OnHealthChanged.AddListener((cur, max) =>
            SetSlider(bossHealthSlider, cur / max));
        bossHealth.OnDeath.AddListener(() => bossBarRoot?.SetActive(false));
    }

    private static void SetSlider(Slider slider, float ratio)
    {
        if (slider != null)
            slider.value = ratio;
    }
}
