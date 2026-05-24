using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lockOnRange = 15f;
    [SerializeField] private float breakRange = 20f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstructionLayer;

    public Transform Target { get; private set; }
    public bool IsLockedOn => Target != null;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsLockedOn) return;

        float dist = Vector3.Distance(transform.position, Target.position);
        if (dist > breakRange || !IsTargetVisible(Target))
            ClearTarget();
    }

    public void ToggleLockOn()
    {
        if (IsLockedOn)
        {
            ClearTarget();
            return;
        }

        var best = FindBestTarget();
        if (best != null)
            Target = best;
    }

    public void SwitchTarget(float direction)
    {
        if (!IsLockedOn) return;

        var candidates = GetVisibleTargets();
        if (candidates.Count <= 1) return;

        var current = Target;
        var currentScreenPos = mainCamera.WorldToScreenPoint(current.position);

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var t in candidates)
        {
            if (t == current) continue;
            var screenPos = mainCamera.WorldToScreenPoint(t.position);
            float horizontalDiff = screenPos.x - currentScreenPos.x;
            if (Mathf.Sign(horizontalDiff) != Mathf.Sign(direction)) continue;

            float score = Mathf.Abs(horizontalDiff);
            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        if (best != null)
            Target = best;
    }

    private Transform FindBestTarget()
    {
        var targets = GetVisibleTargets();
        if (targets.Count == 0) return null;

        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
        return targets
            .OrderBy(t => Vector2.Distance(mainCamera.WorldToScreenPoint(t.position), screenCenter))
            .First();
    }

    private List<Transform> GetVisibleTargets()
    {
        var results = new List<Transform>();
        var cols = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);

        foreach (var col in cols)
        {
            var health = col.GetComponentInParent<HealthSystem>();
            if (health == null || health.IsDead) continue;
            if (IsTargetVisible(col.transform))
                results.Add(col.transform);
        }

        return results;
    }

    private bool IsTargetVisible(Transform target)
    {
        var dir = target.position - mainCamera.transform.position;
        if (Physics.Raycast(mainCamera.transform.position, dir.normalized, out var hit,
                dir.magnitude, obstructionLayer))
        {
            return hit.transform.IsChildOf(target) || hit.transform == target;
        }
        return true;
    }

    private void ClearTarget()
    {
        Target = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);
    }
}
