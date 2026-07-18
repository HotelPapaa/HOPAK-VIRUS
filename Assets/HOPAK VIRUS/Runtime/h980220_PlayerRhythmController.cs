using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class h980220_PlayerRhythmController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 2f;
    [SerializeField] private float maxMoveSpeed = 6f;
    [SerializeField] private int successesToMaxSpeed = 4;
    [SerializeField] private float turnSpeed = 120f;

    [Header("Rhythm")]
    [SerializeField] private float stepDuration = 0.5f;
    [SerializeField] private float successWindow = 0.2f;

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

    public float CurrentSpeed => rhythm == null ? baseMoveSpeed : rhythm.CurrentSpeed;
    public int SuccessStreak => rhythm == null ? 0 : rhythm.SuccessStreak;

    internal void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rhythm = new h980220_RhythmState(
            stepDuration, successWindow, baseMoveSpeed, maxMoveSpeed, successesToMaxSpeed);
        EnsureTorsoBaseline();
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

        rhythm.Tick(deltaTime);

        if (leftDown)
            HandleLeg(h980220_Leg.Left);
        if (rightDown)
            HandleLeg(h980220_Leg.Right);

        transform.Rotate(0f, turnAxis * turnSpeed * deltaTime, 0f);

        if (rhythm.IsMoving)
            characterController.Move(transform.forward * rhythm.CurrentSpeed * deltaTime);

        ApplyPose(h980220_HopakPose.Evaluate(rhythm.ActiveLeg, rhythm.NormalizedStep));
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled && rhythm != null)
        {
            rhythm.Reset();
            ApplyPose(h980220_HopakPose.Evaluate(h980220_Leg.None, 0f));
        }
    }

    private void HandleLeg(h980220_Leg leg)
    {
        rhythm.RegisterInput(leg);
    }

    private void ApplyPose(h980220_LegPose pose)
    {
        if (leftThigh != null)
            leftThigh.localRotation = Quaternion.Euler(pose.LeftThighX, 0f, 0f);
        if (leftShin != null)
            leftShin.localRotation = Quaternion.Euler(pose.LeftShinX, 0f, 0f);
        if (rightThigh != null)
            rightThigh.localRotation = Quaternion.Euler(pose.RightThighX, 0f, 0f);
        if (rightShin != null)
            rightShin.localRotation = Quaternion.Euler(pose.RightShinX, 0f, 0f);

        EnsureTorsoBaseline();
        if (torso != null)
        {
            torso.localPosition = torsoBasePosition + Vector3.down * (pose.TorsoDip * torsoBobHeight);
            torso.localRotation = torsoBaseRotation *
                Quaternion.Euler(0f, 0f, pose.TorsoLean * torsoLeanDegrees);
        }
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
        torsoBobHeight = Mathf.Max(0f, torsoBobHeight);
        torsoLeanDegrees = Mathf.Max(0f, torsoLeanDegrees);
    }
}
