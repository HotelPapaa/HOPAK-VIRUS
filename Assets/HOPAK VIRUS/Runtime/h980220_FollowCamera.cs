using UnityEngine;

public sealed class h980220_FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -9f);
    [Header("Dance Framing")]
    [Tooltip("Lowers the camera so the legs remain visible below the torso.")]
    [SerializeField] private float danceCameraDrop = 2.25f;
    [SerializeField] private float danceLookHeight = 1.05f;

    [Header("Speed Framing")]
    [SerializeField] private float speedEffectStart = 2f;
    [SerializeField] private float speedForFullEffect = 14f;
    [SerializeField] private float maximumDollyOut = 8f;
    [SerializeField] private float maximumBirdEyeRise = 14f;
    [SerializeField] private float speedFramingSmooth = 3.5f;

    [SerializeField] private float positionSmooth = 8f;
    [Tooltip("Maximum world-space distance the camera may lag behind its desired position.")]
    [SerializeField] private float maximumTrackingLag = 3f;
    [SerializeField] private float rotationSmooth = 10f;
    [SerializeField] private float obstructionRadius = 0.25f;
    [SerializeField] private float obstructionPadding = 0.3f;
    [SerializeField] private float minimumDistance = 1f;

    private h980220_PlayerRhythmController rhythmController;
    private h980220_PlayerCombat playerCombat;
    private float currentSpeedFraming;
    private bool victoryView;

    private void LateUpdate()
    {
        Follow(Time.deltaTime);
    }

    internal void Follow(float deltaTime)
    {
        if (target == null)
            return;

        float desiredSpeedFraming = victoryView ? 0f : CalculateSpeedFraming();
        float framingBlend = 1f - Mathf.Exp(-speedFramingSmooth * Mathf.Max(0f, deltaTime));
        currentSpeedFraming = Mathf.Lerp(
            currentSpeedFraming, desiredSpeedFraming, framingBlend);

        Vector3 lookPivot = target.position + Vector3.up * danceLookHeight;
        Vector3 framedOffset = offset + Vector3.down * danceCameraDrop;
        framedOffset.z -= maximumDollyOut * currentSpeedFraming;
        framedOffset.y += maximumBirdEyeRise * currentSpeedFraming;
        Vector3 desiredPosition = target.TransformPoint(framedOffset);
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, desiredPosition, positionSmooth * deltaTime);
        Vector3 trackingLag = smoothedPosition - desiredPosition;
        if (trackingLag.sqrMagnitude > maximumTrackingLag * maximumTrackingLag)
        {
            smoothedPosition = desiredPosition +
                               trackingLag.normalized * maximumTrackingLag;
        }
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
        rhythmController = target == null
            ? null
            : target.GetComponent<h980220_PlayerRhythmController>();
        playerCombat = target == null
            ? null
            : target.GetComponent<h980220_PlayerCombat>();
        currentSpeedFraming = 0f;
    }

    public void SetVictoryView()
    {
        victoryView = true;
        offset = new Vector3(0f, 10f, -14f);
    }

    private float CalculateSpeedFraming()
    {
        if (rhythmController == null && target != null)
            rhythmController = target.GetComponent<h980220_PlayerRhythmController>();
        if (playerCombat == null && target != null)
            playerCombat = target.GetComponent<h980220_PlayerCombat>();

        if (rhythmController == null)
            return 0f;

        float movementSpeed = rhythmController.CurrentSpeed;
        if (playerCombat != null)
            movementSpeed *= playerCombat.CurrentMovementSpeedMultiplier;

        return Mathf.InverseLerp(speedEffectStart, speedForFullEffect, movementSpeed);
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
        if (hitTransform == null ||
            hitTransform.IsChildOf(target) ||
            hitTransform.IsChildOf(transform))
        {
            return true;
        }

        if (hitTransform.GetComponentInParent<h980220_EnemyController>() != null ||
            hitTransform.GetComponentInParent<h980220_Projectile>() != null)
        {
            return true;
        }

        Transform current = hitTransform;
        while (current != null)
        {
            if (current.name == "h980220_InfiniteMap")
                return true;
            current = current.parent;
        }

        return false;
    }

    private void OnValidate()
    {
        positionSmooth = Mathf.Max(0f, positionSmooth);
        maximumTrackingLag = Mathf.Max(0.1f, maximumTrackingLag);
        rotationSmooth = Mathf.Max(0f, rotationSmooth);
        obstructionRadius = Mathf.Max(0.01f, obstructionRadius);
        obstructionPadding = Mathf.Max(0f, obstructionPadding);
        minimumDistance = Mathf.Max(0.05f, minimumDistance);
        danceCameraDrop = Mathf.Max(0f, danceCameraDrop);
        danceLookHeight = Mathf.Max(0.1f, danceLookHeight);
        speedEffectStart = Mathf.Max(0f, speedEffectStart);
        speedForFullEffect = Mathf.Max(speedEffectStart + 0.01f, speedForFullEffect);
        maximumDollyOut = Mathf.Max(0f, maximumDollyOut);
        maximumBirdEyeRise = Mathf.Max(0f, maximumBirdEyeRise);
        speedFramingSmooth = Mathf.Max(0.01f, speedFramingSmooth);
    }
}
