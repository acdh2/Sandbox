using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;           // The GameObject to follow
    public float followSpeed = 5f;     // How fast the object moves
    public float stopDistance = 2f;    // Minimum distance to the target

    [Header("Follow Toggle")]
    public bool isFollowing = true;   // Enabled by default

    void Update()
    {
        if (!isFollowing || target == null)
            return;

        Vector3 toTarget = target.position - transform.position;

        // Already at the target: nothing to look at or move towards
        if (toTarget == Vector3.zero)
            return;

        // Turn towards the target
        transform.rotation = Quaternion.LookRotation(toTarget);

        // Move forward until we are close enough
        if (toTarget.magnitude > stopDistance)
        {
            transform.position += transform.forward * followSpeed * Time.deltaTime;
        }
    }

    // Enable/disable following, e.g. from a UnityEvent
    public void SetFollowing(bool follow)
    {
        isFollowing = follow;
    }
}
