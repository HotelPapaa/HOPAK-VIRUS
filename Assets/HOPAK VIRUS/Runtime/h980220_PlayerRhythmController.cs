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

    private CharacterController characterController;
    private h980220_RhythmState rhythm;
    private bool inputEnabled = true;

    public float CurrentSpeed => rhythm == null ? baseMoveSpeed : rhythm.CurrentSpeed;
    public int SuccessStreak => rhythm == null ? 0 : rhythm.SuccessStreak;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rhythm = new h980220_RhythmState(
            stepDuration, successWindow, baseMoveSpeed, maxMoveSpeed, successesToMaxSpeed);
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        rhythm.Tick(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.A))
            HandleLeg(h980220_Leg.Left);
        if (Input.GetKeyDown(KeyCode.D))
            HandleLeg(h980220_Leg.Right);

        float turn = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
            turn -= 1f;
        if (Input.GetKey(KeyCode.RightArrow))
            turn += 1f;
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

        if (rhythm.IsMoving)
            characterController.Move(transform.forward * rhythm.CurrentSpeed * Time.deltaTime);

        ApplyPose(h980220_HopakPose.Evaluate(rhythm.ActiveLeg, rhythm.NormalizedStep));
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
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
    }

    private void OnValidate()
    {
        baseMoveSpeed = Mathf.Max(0f, baseMoveSpeed);
        stepDuration = Mathf.Max(0.05f, stepDuration);
        successWindow = Mathf.Clamp(successWindow, 0.01f, stepDuration);
        maxMoveSpeed = Mathf.Max(baseMoveSpeed, maxMoveSpeed);
        successesToMaxSpeed = Mathf.Max(1, successesToMaxSpeed);
    }
}
