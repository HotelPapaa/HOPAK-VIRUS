using UnityEngine;

public sealed class h980220_HopakJunior : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color JuniorColor = new Color(0.65f, 0.08f, 0.9f, 1f);
    private const float InfectionRadius = 1.5f;
    private readonly Collider[] infectionHits = new Collider[32];

    private h980220_PlayerRhythmController leader;
    private Material bodyMaterial;
    private Transform torso;
    private Transform leftThigh;
    private Transform leftShin;
    private Transform rightThigh;
    private Transform rightShin;
    private Vector3 torsoBasePosition;
    private Quaternion torsoBaseRotation;
    private Vector3 leftThighBasePosition;
    private Vector3 leftShinBasePosition;
    private Vector3 rightThighBasePosition;
    private Vector3 rightShinBasePosition;
    private Quaternion leftThighBaseRotation;
    private Quaternion leftShinBaseRotation;
    private Quaternion rightThighBaseRotation;
    private Quaternion rightShinBaseRotation;

    public void Initialize(h980220_PlayerRhythmController rhythmLeader, float side)
    {
        leader = rhythmLeader;
        foreach (Renderer sourceRenderer in rhythmLeader.GetComponentsInChildren<Renderer>(true))
        {
            if (sourceRenderer != null &&
                sourceRenderer.GetComponentInParent<h980220_HopakJunior>() == null)
            {
                bodyMaterial = sourceRenderer.sharedMaterial;
                break;
            }
        }
        transform.SetParent(rhythmLeader.transform, false);
        transform.localPosition = new Vector3(side * 2.2f, 0f, -0.25f);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * 0.5f;

        torso = CreateCube("Torso", new Vector3(0f, 2.1f, 0f),
            new Vector3(1.2f, 1.4f, 0.8f));
        leftThigh = CreateCube("LeftThigh", new Vector3(-0.4f, 1.2f, 0f),
            new Vector3(0.7f, 1.16f, 0.7f));
        leftShin = CreateCube("LeftShin", new Vector3(-0.4f, 0.435f, 0f),
            new Vector3(0.63f, 1.62f, 0.63f));
        rightThigh = CreateCube("RightThigh", new Vector3(0.4f, 1.2f, 0f),
            new Vector3(0.7f, 1.16f, 0.7f));
        rightShin = CreateCube("RightShin", new Vector3(0.4f, 0.435f, 0f),
            new Vector3(0.63f, 1.62f, 0.63f));
        CaptureBaselines();
    }

    private void LateUpdate()
    {
        if (leader == null)
            return;

        h980220_LegPose pose = leader.CurrentPose;
        ApplySegment(leftThigh, leftThighBasePosition, leftThighBaseRotation,
            h980220_HopakPose.LeftThighTarget(pose.ActiveLeg),
            h980220_HopakPose.LeftThighRotation(pose.ActiveLeg), pose.Weight);
        ApplySegment(leftShin, leftShinBasePosition, leftShinBaseRotation,
            h980220_HopakPose.LeftShinTarget(pose.ActiveLeg),
            h980220_HopakPose.LeftShinRotation(pose.ActiveLeg), pose.Weight);
        ApplySegment(rightThigh, rightThighBasePosition, rightThighBaseRotation,
            h980220_HopakPose.RightThighTarget(pose.ActiveLeg),
            h980220_HopakPose.RightThighRotation(pose.ActiveLeg), pose.Weight);
        ApplySegment(rightShin, rightShinBasePosition, rightShinBaseRotation,
            h980220_HopakPose.RightShinTarget(pose.ActiveLeg),
            h980220_HopakPose.RightShinRotation(pose.ActiveLeg), pose.Weight);

        Vector3 torsoTarget = torsoBasePosition + Vector3.down * (0.508f * 0.18f);
        torso.localPosition = Vector3.Lerp(torsoBasePosition, torsoTarget, pose.Weight);
        torso.localRotation = Quaternion.Slerp(torsoBaseRotation,
            torsoBaseRotation * h980220_HopakPose.TorsoRotation(pose.ActiveLeg, 1f),
            pose.Weight);

        InfectTouchingCivilians();
    }

    private void InfectTouchingCivilians()
    {
        Vector3 center = transform.TransformPoint(new Vector3(0f, 1.5f, 0f));
        Vector3 scale = transform.lossyScale;
        float radius = InfectionRadius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, infectionHits,
            ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = infectionHits[i];
            if (hit == null)
                continue;
            h980220_EnemyController enemy =
                hit.GetComponentInParent<h980220_EnemyController>();
            if (enemy == null || enemy.IsInfected ||
                enemy.EnemyType != h980220_EnemyType.Basic)
                continue;
            enemy.ReceiveVirusHit();
        }
    }

    private Transform CreateCube(string objectName, Vector3 position, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (bodyMaterial != null)
            renderer.sharedMaterial = bodyMaterial;
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColorId, JuniorColor);
        properties.SetColor(ColorId, JuniorColor);
        renderer.SetPropertyBlock(properties);
        return part.transform;
    }

    private void CaptureBaselines()
    {
        torsoBasePosition = torso.localPosition;
        torsoBaseRotation = torso.localRotation;
        leftThighBasePosition = leftThigh.localPosition;
        leftShinBasePosition = leftShin.localPosition;
        rightThighBasePosition = rightThigh.localPosition;
        rightShinBasePosition = rightShin.localPosition;
        leftThighBaseRotation = leftThigh.localRotation;
        leftShinBaseRotation = leftShin.localRotation;
        rightThighBaseRotation = rightThigh.localRotation;
        rightShinBaseRotation = rightShin.localRotation;
    }

    private static void ApplySegment(Transform segment, Vector3 basePosition,
        Quaternion baseRotation, Vector3 targetPosition, Quaternion targetRotation,
        float weight)
    {
        segment.localPosition = Vector3.Lerp(basePosition, targetPosition, weight);
        segment.localRotation = Quaternion.Slerp(baseRotation, targetRotation, weight);
    }
}
