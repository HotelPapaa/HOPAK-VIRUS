using UnityEngine;

public sealed class h980220_FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -9f);
    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float rotationSmooth = 10f;
    [SerializeField] private float obstructionRadius = 0.25f;
    [SerializeField] private float obstructionPadding = 0.3f;
    [SerializeField] private float minimumDistance = 1f;

    private void LateUpdate()
    {
        Follow(Time.deltaTime);
    }

    internal void Follow(float deltaTime)
    {
        if (target == null)
            return;

        Vector3 lookPivot = target.position + Vector3.up * 1.5f;
        Vector3 desiredPosition = target.TransformPoint(offset);
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, desiredPosition, positionSmooth * deltaTime);
        transform.position = ResolveObstruction(lookPivot, smoothedPosition);

        Vector3 lookDirection = lookPivot - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRotation, rotationSmooth * deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetVictoryView()
    {
        offset = new Vector3(0f, 10f, -14f);
    }

    private Vector3 ResolveObstruction(Vector3 pivot, Vector3 candidate)
    {
        Vector3 offsetFromPivot = candidate - pivot;
        float candidateDistance = offsetFromPivot.magnitude;
        if (candidateDistance <= 0.001f)
            return candidate;

        Vector3 direction = offsetFromPivot / candidateDistance;
        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.SphereCastAll(
                     pivot,
                     obstructionRadius,
                     direction,
                     candidateDistance,
                     Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || IsIgnoredHierarchy(hit.collider.transform))
                continue;

            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        if (float.IsPositiveInfinity(closestDistance))
            return candidate;

        float paddedDistance = Mathf.Max(0.05f, closestDistance - obstructionPadding);
        if (closestDistance >= minimumDistance + obstructionPadding)
            paddedDistance = Mathf.Max(minimumDistance, paddedDistance);
        return pivot + direction * paddedDistance;
    }

    private bool IsIgnoredHierarchy(Transform hitTransform)
    {
        return hitTransform == null ||
               hitTransform.IsChildOf(target) ||
               hitTransform.IsChildOf(transform);
    }

    private void OnValidate()
    {
        positionSmooth = Mathf.Max(0f, positionSmooth);
        rotationSmooth = Mathf.Max(0f, rotationSmooth);
        obstructionRadius = Mathf.Max(0.01f, obstructionRadius);
        obstructionPadding = Mathf.Max(0f, obstructionPadding);
        minimumDistance = Mathf.Max(0.05f, minimumDistance);
    }
}
