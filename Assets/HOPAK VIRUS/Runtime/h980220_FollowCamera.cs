using UnityEngine;

public sealed class h980220_FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -9f);
    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float rotationSmooth = 10f;

    private void LateUpdate()
    {
        Follow(Time.deltaTime);
    }

    internal void Follow(float deltaTime)
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(
            transform.position, desiredPosition, positionSmooth * deltaTime);

        Vector3 lookDirection = target.position + Vector3.up * 1.5f - transform.position;
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
}
