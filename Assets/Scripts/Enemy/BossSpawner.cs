using UnityEngine;

// Place this on a trigger volume at the boss arena entrance.
// When the player walks in, it registers the boss with the HUD
// and seals the entrance door.
public class BossSpawner : MonoBehaviour
{
    [SerializeField] private BossAI boss;
    [SerializeField] private string bossName = "The Ancient One";
    [SerializeField] private GameObject entranceDoor;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (entranceDoor != null)
            entranceDoor.SetActive(true);

        var hud = FindObjectOfType<HUDManager>();
        if (boss != null && hud != null)
        {
            var bossHealth = boss.GetComponent<HealthSystem>();
            if (bossHealth != null)
                hud.RegisterBoss(bossHealth, bossName);
        }
    }
}
