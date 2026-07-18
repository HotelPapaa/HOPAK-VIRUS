using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class h980220_PlayerCombat : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeedMultiplier = 3.5f;
    [SerializeField] private float rhythmGraceBeforeAndAfter = 0.5f;
    [SerializeField] private float fallbackDashDuration = 0.5f;

    private CharacterController characterController;
    private h980220_PlayerRhythmController rhythmController;
    private bool inputEnabled = true;
    private float dashStartedAt = float.NegativeInfinity;
    private float dashUntil = float.NegativeInfinity;
    private float dashInitialSpeed;
    private Vector3 dashDirection;

    public bool IsDashing => inputEnabled && Time.time < dashUntil;
    public float CurrentMovementSpeedMultiplier => IsDashing
        ? 1f + (dashSpeedMultiplier - 1f) * CurrentGlide(Time.time)
        : 1f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rhythmController = GetComponent<h980220_PlayerRhythmController>();
    }

    private void Update()
    {
        float now = Time.time;
        if (inputEnabled && Input.GetKeyDown(KeyCode.S))
            DashAtTime(now);

        if (!inputEnabled || now >= dashUntil)
            return;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        characterController.Move(
            dashDirection * dashInitialSpeed * CurrentGlide(now) * Time.deltaTime);
    }

    internal bool ProcessInputAtTime(bool dashPressed, float now)
    {
        return inputEnabled && dashPressed && DashAtTime(now);
    }

    public bool Dash()
    {
        return DashAtTime(Time.time);
    }

    public bool Fire()
    {
        return Dash();
    }

    internal bool DashAtTime(float now)
    {
        if (!inputEnabled)
            return false;

        if (rhythmController == null)
            rhythmController = GetComponent<h980220_PlayerRhythmController>();

        float duration = rhythmController == null
            ? fallbackDashDuration
            : rhythmController.RegisterDashBeat(rhythmGraceBeforeAndAfter);
        float currentSpeed = rhythmController == null ? 0f : rhythmController.CurrentSpeed;

        dashStartedAt = now;
        dashUntil = now + Mathf.Max(0.01f, duration);
        dashInitialSpeed = currentSpeed * dashSpeedMultiplier;
        dashDirection = transform.forward;
        return true;
    }

    internal bool TryContactEnemy(h980220_EnemyController enemy)
    {
        if (!inputEnabled || enemy == null)
            return false;

        bool enemyResolvedContact = enemy.ReceivePlayerContact(IsDashing);
        if (!enemyResolvedContact && enemy.IsPolice)
        {
            h980220_PlayerInfection infection = GetComponent<h980220_PlayerInfection>();
            infection?.ReceiveFatalContact();
        }

        return enemyResolvedContact;
    }

    private float CurrentGlide(float now)
    {
        float duration = Mathf.Max(0.01f, dashUntil - dashStartedAt);
        float progress = Mathf.Clamp01((now - dashStartedAt) / duration);
        return 1f - progress * progress;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider != null)
            TryContactEnemy(hit.collider.GetComponentInParent<h980220_EnemyController>());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
            TryContactEnemy(other.GetComponentInParent<h980220_EnemyController>());
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
            dashUntil = float.NegativeInfinity;
    }

    private void OnValidate()
    {
        dashSpeedMultiplier = Mathf.Max(1f, dashSpeedMultiplier);
        rhythmGraceBeforeAndAfter = Mathf.Max(0f, rhythmGraceBeforeAndAfter);
        fallbackDashDuration = Mathf.Max(0.01f, fallbackDashDuration);
    }
}
