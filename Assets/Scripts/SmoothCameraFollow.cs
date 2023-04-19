using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    private Vector3 offset;
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime;
    private Vector3 currentVelocity = Vector3.zero;

    private void Awake()
    {
        // How far to keep the camera from the object.
        offset = transform.position - target.position;
    }

    // The reason this is LateUpdate and not Update is because the target's position is very likely going to be
    // updated inside Update, so putting this code inside Update will create a situation where the target and camera
    // are competing to go first, creating jittery movement. So, we wait until the position has been updated and
    // THEN have the camera follow.
    private void LateUpdate()
    {
        // Add the offset to the current target position, this is to ensure that the camera stays the same distance away from the target.
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
