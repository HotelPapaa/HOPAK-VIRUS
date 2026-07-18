using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class h980220_PlayerCombat : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dashSpeedMultiplier = 3.5f;
    [SerializeField] private float dashCooldown = 0.65f;

    private CharacterController characterController;
    private h980220_PlayerRhythmController rhythmController;
    private bool inputEnabled = true;
    private float dashUntil = float.NegativeInfinity;
    private float nextDashTime = float.NegativeInfinity;

    public bool IsDashing => inputEnabled && Time.time < dashUntil;
    public float CurrentMovementSpeedMultiplier => IsDashing ? dashSpeedMultiplier : 1f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rhythmController = GetComponent<h980220_PlayerRhythmController>();
    }

    private void Update()
    {
        float now = Time.time;
        if (inputEnabled && Input.GetKeyDown(KeyCode.Space))
            DashAtTime(now);

        if (!inputEnabled || now >= dashUntil)
            return;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (rhythmController == null)
            rhythmController = GetComponent<h980220_PlayerRhythmController>();

        float currentSpeed = rhythmController == null ? 0f : rhythmController.CurrentSpeed;
        characterController.Move(
            transform.forward * currentSpeed * dashSpeedMultiplier * Time.deltaTime);
    }

    internal bool ProcessInputAtTime(bool dashPressed, float now)
    {
        return inputEnabled && dashPressed && DashAtTime(now);
    }

    public bool Dash()
    {
        return DashAtTime(Time.time);
    }

    // Kept as a compatibility wrapper for existing scene/test callers.
    public bool Fire()
    {
        return Dash();
    }

    internal bool DashAtTime(float now)
    {
        if (!inputEnabled || now < nextDashTime)
            return false;

        dashUntil = now + dashDuration;
        nextDashTime = now + dashCooldown;
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
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashSpeedMultiplier = Mathf.Max(0f, dashSpeedMultiplier);
        dashCooldown = Mathf.Max(dashDuration, dashCooldown);
    }
}
