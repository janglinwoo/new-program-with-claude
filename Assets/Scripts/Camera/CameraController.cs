using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private LockOnSystem lockOn;

    [Header("Free Look")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private Vector3 offset = new(0f, 1.8f, 0f);
    [SerializeField] private float distance = 5f;

    [Header("Lock-On")]
    [SerializeField] private float lockOnSpeed = 8f;
    [SerializeField] private Vector3 lockOnOffset = new(0f, 1.2f, 0f);

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayer;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void LateUpdate()
    {
        if (lockOn != null && lockOn.IsLockedOn && lockOn.Target != null)
            UpdateLockOnCamera();
        else
            UpdateFreeLookCamera();
    }

    private void UpdateFreeLookCamera()
    {
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + offset;
        Vector3 desiredPos = pivot - rotation * Vector3.forward * distance;

        transform.rotation = rotation;
        transform.position = ResolveCollision(pivot, desiredPos);
    }

    private void UpdateLockOnCamera()
    {
        Vector3 pivot = target.position + lockOnOffset;
        Vector3 midpoint = (pivot + lockOn.Target.position) * 0.5f;
        midpoint.y = pivot.y;

        Vector3 lookDir = (lockOn.Target.position - pivot).normalized;
        lookDir.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        targetRot = Quaternion.Euler(20f, targetRot.eulerAngles.y, 0f);

        Vector3 desiredPos = pivot - targetRot * Vector3.forward * distance;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lockOnSpeed * Time.deltaTime);
        transform.position = ResolveCollision(pivot, desiredPos);

        // Sync yaw so free-look doesn't snap when lock-on ends
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPos)
    {
        Vector3 dir = desiredPos - pivot;
        if (Physics.SphereCast(pivot, collisionRadius, dir.normalized, out var hit,
                dir.magnitude, collisionLayer))
        {
            return pivot + dir.normalized * (hit.distance - collisionRadius);
        }
        return desiredPos;
    }
}
