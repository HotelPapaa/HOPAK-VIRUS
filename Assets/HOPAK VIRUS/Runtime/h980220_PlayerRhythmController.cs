using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class h980220_PlayerRhythmController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 2f;
    [InspectorName("Reference Move Speed")]
    [Tooltip("Speed reached at the reference success count. Acceleration continues past it.")]
    [SerializeField] private float maxMoveSpeed = 6f;
    [InspectorName("Successes To Reference Speed")]
    [SerializeField] private int successesToMaxSpeed = 4;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float slideDeceleration = 12f;

    [Header("Rhythm")]
    [SerializeField] private float stepDuration = 0.5f;
    [SerializeField] private float successWindow = 0.2f;
    [SerializeField] private float cadenceAccelerationPerSuccess = 0.15f;

    [Header("Leg Segments")]
    [SerializeField] private Transform leftThigh;
    [SerializeField] private Transform leftShin;
    [SerializeField] private Transform rightThigh;
    [SerializeField] private Transform rightShin;

    [Header("Torso Motion")]
    [SerializeField] private Transform torso;
    [SerializeField] private float torsoBobHeight = 0.18f;
    [SerializeField] private float torsoLeanDegrees = 12f;

    private CharacterController characterController;
    private h980220_RhythmState rhythm;
    private bool inputEnabled = true;
    private Transform capturedTorso;
    private Vector3 torsoBasePosition;
    private Quaternion torsoBaseRotation;
    private Transform capturedLeftThigh;
    private Transform capturedLeftShin;
    private Transform capturedRightThigh;
    private Transform capturedRightShin;
    private Vector3 leftThighBasePosition;
    private Vector3 leftShinBasePosition;
    private Vector3 rightThighBasePosition;
    private Vector3 rightShinBasePosition;
    private Quaternion leftThighBaseRotation;
    private Quaternion leftShinBaseRotation;
    private Quaternion rightThighBaseRotation;
    private Quaternion rightShinBaseRotation;
    private float momentumSpeed;
    private Vector3 momentumDirection;
    private h980220_PlayerCombat playerCombat;

    public float CurrentSpeed => rhythm == null
        ? Mathf.Max(baseMoveSpeed, momentumSpeed)
        : Mathf.Max(rhythm.CurrentSpeed, momentumSpeed);
    public float CurrentStepDuration => rhythm == null ? stepDuration : rhythm.CurrentStepDuration;
    public int SuccessStreak => rhythm == null ? 0 : rhythm.SuccessStreak;
    public int MaximumSuccessStreak { get; private set; }
    public h980220_LegPose CurrentPose { get; private set; }

    internal void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCombat = GetComponent<h980220_PlayerCombat>();
        rhythm = new h980220_RhythmState(
            stepDuration, successWindow, baseMoveSpeed, maxMoveSpeed,
            successesToMaxSpeed, cadenceAccelerationPerSuccess);
        EnsureTorsoBaseline();
        EnsureLegBaselines();
    }

    private void Update()
    {
        float turn = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
            turn -= 1f;
        if (Input.GetKey(KeyCode.RightArrow))
            turn += 1f;

        ProcessFrame(
            Time.deltaTime,
            Input.GetKeyDown(KeyCode.A),
            Input.GetKeyDown(KeyCode.D),
            turn);
    }

    internal void ProcessFrame(float deltaTime, bool leftDown, bool rightDown, float turnAxis)
    {
        if (!inputEnabled)
            return;

        if (playerCombat == null)
            playerCombat = GetComponent<h980220_PlayerCombat>();
        if (playerCombat != null && playerCombat.IsJumping)
        {
            ApplyPose(h980220_HopakPose.Evaluate(h980220_Leg.None, 0f));
            return;
        }

        rhythm.Tick(deltaTime);

        if (leftDown)
            HandleLeg(h980220_Leg.Left);
        if (rightDown)
            HandleLeg(h980220_Leg.Right);
        MaximumSuccessStreak = Mathf.Max(MaximumSuccessStreak, rhythm.SuccessStreak);

        transform.Rotate(0f, turnAxis * turnSpeed * deltaTime, 0f);

        if (rhythm.IsMoving)
        {
            momentumSpeed = rhythm.CurrentSpeed;
            momentumDirection = transform.forward;
        }
        else
        {
            momentumSpeed = Mathf.MoveTowards(
                momentumSpeed, 0f, slideDeceleration * Mathf.Max(0f, deltaTime));
        }

        if (momentumSpeed > 0f && (playerCombat == null || !playerCombat.IsJumping))
            characterController.Move(momentumDirection * momentumSpeed * deltaTime);

        ApplyPose(h980220_HopakPose.Evaluate(rhythm.ActiveLeg, rhythm.NormalizedStep));
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled && rhythm != null)
        {
            rhythm.Reset();
            momentumSpeed = 0f;
            ApplyPose(h980220_HopakPose.Evaluate(h980220_Leg.None, 0f));
        }
    }

    public void SetLevelUpPaused(bool paused)
    {
        inputEnabled = !paused;
    }

    public float RegisterDashBeat(float graceSeconds)
    {
        if (rhythm == null)
            Awake();
        float duration = rhythm.RegisterDash(graceSeconds);
        MaximumSuccessStreak = Mathf.Max(MaximumSuccessStreak, rhythm.SuccessStreak);
        return duration;
    }

    private void HandleLeg(h980220_Leg leg)
    {
        if (rhythm.ActiveLeg == h980220_Leg.None)
            rhythm.ResumeFromSlidingSpeed(momentumSpeed);
        rhythm.RegisterInput(leg);
    }

    private void ApplyPose(h980220_LegPose pose)
    {
        CurrentPose = pose;
        EnsureLegBaselines();
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

        EnsureTorsoBaseline();
        if (torso != null)
        {
            float torsoStrength = torsoLeanDegrees / 12f;
            Vector3 targetPosition = torsoBasePosition +
                                     Vector3.down * (0.508f * torsoBobHeight);
            torso.localPosition = Vector3.Lerp(torsoBasePosition, targetPosition, pose.Weight);
            torso.localRotation = Quaternion.Slerp(torsoBaseRotation,
                torsoBaseRotation * h980220_HopakPose.TorsoRotation(
                    pose.ActiveLeg, torsoStrength), pose.Weight);
        }
    }

    private static void ApplySegment(
        Transform segment, Vector3 basePosition, Quaternion baseRotation,
        Vector3 targetPosition, Quaternion targetRotation, float weight)
    {
        if (segment == null)
            return;
        segment.localPosition = Vector3.Lerp(basePosition, targetPosition, weight);
        segment.localRotation = Quaternion.Slerp(baseRotation, targetRotation, weight);
    }

    private void EnsureLegBaselines()
    {
        CaptureLegBaseline(leftThigh, ref capturedLeftThigh,
            ref leftThighBasePosition, ref leftThighBaseRotation);
        CaptureLegBaseline(leftShin, ref capturedLeftShin,
            ref leftShinBasePosition, ref leftShinBaseRotation);
        CaptureLegBaseline(rightThigh, ref capturedRightThigh,
            ref rightThighBasePosition, ref rightThighBaseRotation);
        CaptureLegBaseline(rightShin, ref capturedRightShin,
            ref rightShinBasePosition, ref rightShinBaseRotation);
    }

    private static void CaptureLegBaseline(
        Transform segment, ref Transform captured,
        ref Vector3 basePosition, ref Quaternion baseRotation)
    {
        if (segment == null || captured == segment)
            return;
        captured = segment;
        basePosition = segment.localPosition;
        baseRotation = segment.localRotation;
    }

    private void EnsureTorsoBaseline()
    {
        if (torso == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name == "Torso")
                {
                    torso = child;
                    break;
                }
            }
        }

        if (torso == null || capturedTorso == torso)
            return;

        capturedTorso = torso;
        torsoBasePosition = torso.localPosition;
        torsoBaseRotation = torso.localRotation;
    }

    private void OnValidate()
    {
        baseMoveSpeed = Mathf.Max(0f, baseMoveSpeed);
        stepDuration = Mathf.Max(0.05f, stepDuration);
        successWindow = Mathf.Clamp(successWindow, 0.01f, stepDuration);
        maxMoveSpeed = Mathf.Max(baseMoveSpeed, maxMoveSpeed);
        successesToMaxSpeed = Mathf.Max(1, successesToMaxSpeed);
        cadenceAccelerationPerSuccess = Mathf.Max(0.01f, cadenceAccelerationPerSuccess);
        slideDeceleration = Mathf.Max(0.01f, slideDeceleration);
        torsoBobHeight = Mathf.Max(0f, torsoBobHeight);
        torsoLeanDegrees = Mathf.Max(0f, torsoLeanDegrees);
    }
}
