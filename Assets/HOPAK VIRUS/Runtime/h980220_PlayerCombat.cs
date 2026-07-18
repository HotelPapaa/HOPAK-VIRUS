using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class h980220_PlayerCombat : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeedMultiplier = 3.5f;
    [SerializeField] private float rhythmGraceBeforeAndAfter = 0.5f;
    [SerializeField] private float fallbackDashDuration = 0.5f;
    [SerializeField] private float dashChargeSeconds = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 16f;
    [SerializeField] private float minimumJumpForwardSpeed = 6f;
    [SerializeField] private float gravity = 22f;
    [SerializeField] private float jumpChargeSeconds = 4f;
    [SerializeField] private float groundedGraceSeconds = 0.15f;

    private CharacterController characterController;
    private h980220_PlayerRhythmController rhythmController;
    private bool inputEnabled = true;
    private float dashStartedAt = float.NegativeInfinity;
    private float dashUntil = float.NegativeInfinity;
    private float dashInitialSpeed;
    private Vector3 dashDirection;
    private bool dashUnlocked;
    private bool jumpUnlocked;
    private int jumpUpgradeLevel;
    private float verticalSpeed;
    private int maximumDashCharges = 1;
    private int currentDashCharges;
    private float nextDashChargeTime = float.PositiveInfinity;
    private int maximumJumpCharges = 1;
    private int currentJumpCharges;
    private float nextJumpChargeTime = float.PositiveInfinity;
    private bool isJumping;
    private float jumpForwardSpeed;
    private Vector3 jumpDirection;
    private float lastGroundedTime = float.NegativeInfinity;

    public bool IsDashing => inputEnabled && Time.time < dashUntil;
    public bool IsJumping => isJumping;
    public bool IsDashUnlocked => dashUnlocked;
    public bool IsJumpUnlocked => jumpUnlocked;
    public int CurrentDashCharges => currentDashCharges;
    public int MaximumDashCharges => maximumDashCharges;
    public int CurrentJumpCharges => currentJumpCharges;
    public int MaximumJumpCharges => maximumJumpCharges;
    public float DashChargeRemaining => dashUnlocked && currentDashCharges < maximumDashCharges
        ? Mathf.Max(0f, nextDashChargeTime - Time.time) : 0f;
    public float JumpChargeRemaining => jumpUnlocked && currentJumpCharges < maximumJumpCharges
        ? Mathf.Max(0f, nextJumpChargeTime - Time.time) : 0f;
    public float DashChargeSeconds => dashChargeSeconds;
    public float JumpChargeSeconds => jumpChargeSeconds;
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
        RechargeAbilities(now);
        if (characterController != null && characterController.isGrounded)
            lastGroundedTime = now;
        if (inputEnabled && Input.GetKeyDown(KeyCode.S))
            DashAtTime(now);

        if (inputEnabled && jumpUnlocked && Input.GetKeyDown(KeyCode.Space) &&
            currentJumpCharges > 0 && characterController != null &&
            (characterController.isGrounded ||
             now - lastGroundedTime <= groundedGraceSeconds))
            StartJump(now);

        if (characterController != null)
        {
            if (isJumping)
            {
                verticalSpeed -= gravity * Time.deltaTime;
                CollisionFlags flags = characterController.Move(
                    (jumpDirection * jumpForwardSpeed + Vector3.up * verticalSpeed) *
                    Time.deltaTime);
                if ((flags & CollisionFlags.Below) != 0 && verticalSpeed <= 0f)
                {
                    isJumping = false;
                    verticalSpeed = -1f;
                    lastGroundedTime = now;
                }
            }
            else
            {
                if (characterController.isGrounded && verticalSpeed < 0f)
                    verticalSpeed = -2f;
                else
                    verticalSpeed -= gravity * Time.deltaTime;
                CollisionFlags flags = characterController.Move(
                    Vector3.up * verticalSpeed * Time.deltaTime);
                if ((flags & CollisionFlags.Below) != 0)
                    lastGroundedTime = now;
            }
        }

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
        if (!inputEnabled || !dashUnlocked || currentDashCharges <= 0)
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
        currentDashCharges--;
        if (currentDashCharges == maximumDashCharges - 1)
            nextDashChargeTime = now + dashChargeSeconds;
        return true;
    }

    private void StartJump(float now)
    {
        if (rhythmController == null)
            rhythmController = GetComponent<h980220_PlayerRhythmController>();
        verticalSpeed = jumpSpeed * (1f + jumpUpgradeLevel * 0.18f);
        float currentSpeed = rhythmController == null ? 0f : rhythmController.CurrentSpeed;
        jumpForwardSpeed = Mathf.Max(minimumJumpForwardSpeed, currentSpeed);
        jumpDirection = transform.forward;
        isJumping = true;
        currentJumpCharges--;
        if (currentJumpCharges == maximumJumpCharges - 1)
            nextJumpChargeTime = now + jumpChargeSeconds;
    }

    private void RechargeAbilities(float now)
    {
        Recharge(ref currentDashCharges, maximumDashCharges,
            ref nextDashChargeTime, dashChargeSeconds, dashUnlocked, now);
        Recharge(ref currentJumpCharges, maximumJumpCharges,
            ref nextJumpChargeTime, jumpChargeSeconds, jumpUnlocked, now);
    }

    private static void Recharge(ref int current, int maximum, ref float nextTime,
        float chargeSeconds, bool unlocked, float now)
    {
        if (!unlocked || current >= maximum || now < nextTime)
            return;
        current++;
        nextTime = current < maximum ? now + chargeSeconds : float.PositiveInfinity;
    }

    internal bool TryContactEnemy(h980220_EnemyController enemy)
    {
        if (!inputEnabled || enemy == null)
            return false;

        bool enemyResolvedContact = enemy.ReceivePlayerContact(IsDashing);
        if (!enemyResolvedContact && enemy.IsPolice)
        {
            h980220_PlayerInfection infection = GetComponent<h980220_PlayerInfection>();
            infection?.TryReceiveCure(enemy.transform.position);
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

    public void SetLevelUpPaused(bool paused)
    {
        inputEnabled = !paused;
    }

    public void UnlockDash()
    {
        dashUnlocked = true;
        currentDashCharges = maximumDashCharges;
        nextDashChargeTime = float.PositiveInfinity;
    }

    public void UnlockJump()
    {
        jumpUnlocked = true;
        currentJumpCharges = maximumJumpCharges;
        nextJumpChargeTime = float.PositiveInfinity;
    }

    public void UpgradeDash()
    {
        maximumDashCharges++;
        currentDashCharges++;
    }

    public void UpgradeJump()
    {
        jumpUpgradeLevel++;
    }

    private void OnValidate()
    {
        dashSpeedMultiplier = Mathf.Max(1f, dashSpeedMultiplier);
        rhythmGraceBeforeAndAfter = Mathf.Max(0f, rhythmGraceBeforeAndAfter);
        fallbackDashDuration = Mathf.Max(0.01f, fallbackDashDuration);
        dashChargeSeconds = Mathf.Max(0.1f, dashChargeSeconds);
        jumpSpeed = Mathf.Max(1f, jumpSpeed);
        minimumJumpForwardSpeed = Mathf.Max(0.1f, minimumJumpForwardSpeed);
        gravity = Mathf.Max(1f, gravity);
        jumpChargeSeconds = Mathf.Max(0.1f, jumpChargeSeconds);
        groundedGraceSeconds = Mathf.Clamp(groundedGraceSeconds, 0f, 0.5f);
    }
}
