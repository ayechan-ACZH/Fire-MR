using UnityEngine;

public class SpreadFire : MonoBehaviour
{
    [Header("Wander Settings")]
    [SerializeField, Tooltip("Maximum distance from the original spawn point on the XZ plane.")]
    private float wanderRadius = 2.0f;

    [SerializeField, Tooltip("Movement speed toward each random target.")]
    private float moveSpeed = 1.5f;

    [SerializeField, Tooltip("How close the object must be to a target before selecting a new one.")]
    private float destinationTolerance = 0.1f;

    [SerializeField, Tooltip("Pause time at each target before choosing another destination.")]
    private float waitAtTargetSeconds = 0.4f;

    private Vector3 spawnOrigin;
    private Vector3 currentTarget;
    private float waitTimer;

    void Start()
    {
        spawnOrigin = transform.position;
        currentTarget = GetRandomTargetPoint();
    }

    void Update()
    {
        if (wanderRadius <= 0f || moveSpeed <= 0f)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        currentPosition.y = spawnOrigin.y;
        transform.position = currentPosition;

        float distanceToTarget = Vector3.Distance(currentPosition, currentTarget);
        if (distanceToTarget <= destinationTolerance)
        {
            if (waitAtTargetSeconds > 0f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer < waitAtTargetSeconds)
                {
                    return;
                }
            }

            waitTimer = 0f;
            currentTarget = GetRandomTargetPoint();
            return;
        }

        transform.position = Vector3.MoveTowards(currentPosition, currentTarget, moveSpeed * Time.deltaTime);
    }

    private Vector3 GetRandomTargetPoint()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        return new Vector3(spawnOrigin.x + offset.x, spawnOrigin.y, spawnOrigin.z + offset.y);
    }

    private void OnValidate()
    {
        wanderRadius = Mathf.Max(0f, wanderRadius);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        destinationTolerance = Mathf.Max(0.01f, destinationTolerance);
        waitAtTargetSeconds = Mathf.Max(0f, waitAtTargetSeconds);
    }
}
