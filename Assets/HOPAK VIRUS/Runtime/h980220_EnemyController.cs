using System;
using UnityEngine;

public enum h980220_EnemyType
{
    Basic,
    Ranged,
    Elite
}

[RequireComponent(typeof(CharacterController))]
public sealed class h980220_EnemyController : MonoBehaviour, h980220_IVirusHitReceiver
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private h980220_EnemyType enemyType;
    [SerializeField] private int requiredHits = 1;
    [SerializeField] private Transform player;
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float contactRange = 1.25f;
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private h980220_Projectile cureProjectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Infection Visuals")]
    [SerializeField] private Renderer[] bodyRenderers = Array.Empty<Renderer>();
    [SerializeField] private Color healthyColor = new Color(0f, 0.8f, 0.7f, 1f);
    [SerializeField] private Color infectedColor = new Color(0.65f, 0.1f, 0.85f, 1f);
    [SerializeField] private float danceHeight = 0.2f;
    [SerializeField] private float danceLeanDegrees = 12f;

    private CharacterController characterController;
    private int receivedHits;
    private bool combatEnabled = true;
    private float nextFireTime = float.NegativeInfinity;
    private Vector3 danceOrigin;
    private Quaternion danceRotation;

    public event Action<h980220_EnemyController> Infected;

    public h980220_EnemyType EnemyType => enemyType;
    public int RequiredHits => requiredHits;
    public bool IsInfected { get; private set; }
    public bool IsCombatEnabled => combatEnabled && !IsInfected;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        danceOrigin = transform.position;
        danceRotation = transform.rotation;
    }

    public void Configure(h980220_EnemyType type, int hitsRequired)
    {
        enemyType = type;
        requiredHits = Mathf.Max(1, hitsRequired);
        receivedHits = 0;
        IsInfected = false;
        combatEnabled = true;
        nextFireTime = float.NegativeInfinity;
        danceOrigin = transform.position;
        danceRotation = transform.rotation;
        RefreshColor();
    }

    public void ReceiveVirusHit()
    {
        if (IsInfected)
            return;

        receivedHits++;
        RefreshColor();
        if (receivedHits < requiredHits)
            return;

        IsInfected = true;
        combatEnabled = false;
        danceOrigin = transform.position;
        danceRotation = transform.rotation;
        Infected?.Invoke(this);
    }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled && !IsInfected;
    }

    private void Update()
    {
        Tick(Time.time, Time.deltaTime);
    }

    internal void Tick(float now, float deltaTime)
    {
        if (IsInfected)
        {
            Dance(now);
            return;
        }

        if (!combatEnabled || player == null)
            return;

        switch (enemyType)
        {
            case h980220_EnemyType.Basic:
                MoveTowardPlayer(deltaTime);
                TryContactCure(now);
                break;
            case h980220_EnemyType.Ranged:
                FacePlayer();
                TryFireCure(now);
                break;
            case h980220_EnemyType.Elite:
                MoveTowardPlayer(deltaTime);
                FacePlayer();
                TryContactCure(now);
                TryFireCure(now);
                break;
        }
    }

    private void MoveTowardPlayer(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        Vector3 direction = HorizontalDirectionToPlayer();
        if (direction == Vector3.zero)
            return;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        characterController.Move(direction * movementSpeed * deltaTime);
    }

    private void FacePlayer()
    {
        Vector3 direction = HorizontalDirectionToPlayer();
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void TryContactCure(float now)
    {
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > contactRange * contactRange)
            return;

        if (Physics.Linecast(transform.position, player.position, out RaycastHit hit,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            !hit.transform.IsChildOf(player) && !player.IsChildOf(hit.transform))
        {
            return;
        }

        h980220_PlayerInfection infection = player.GetComponentInParent<h980220_PlayerInfection>();
        if (infection != null)
            infection.ReceiveCureAtTime(transform.position, now);
    }

    private void TryFireCure(float now)
    {
        if (cureProjectilePrefab == null || now < nextFireTime)
            return;

        Vector3 direction = HorizontalDirectionToPlayer();
        if (direction == Vector3.zero)
            return;

        Transform origin = firePoint == null ? transform : firePoint;
        h980220_Projectile projectile = Instantiate(cureProjectilePrefab, origin.position,
            Quaternion.LookRotation(direction, Vector3.up));
        SetProjectileColor(projectile, Color.white);
        projectile.Initialize(h980220_ProjectileKind.Cure, direction, 7f, 12f);
        nextFireTime = now + Mathf.Max(0f, fireInterval);
    }

    private Vector3 HorizontalDirectionToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    private void Dance(float now)
    {
        float wave = Mathf.Sin(now);
        transform.position = danceOrigin + Vector3.up * (wave * danceHeight);
        transform.rotation = danceRotation * Quaternion.Euler(0f, 0f, wave * danceLeanDegrees);
    }

    private void RefreshColor()
    {
        float progress = requiredHits <= 0 ? 0f : Mathf.Clamp01((float)receivedHits / requiredHits);
        Color color = Color.Lerp(healthyColor, infectedColor, progress);
        var properties = new MaterialPropertyBlock();

        foreach (Renderer bodyRenderer in bodyRenderers)
        {
            if (bodyRenderer == null)
                continue;

            bodyRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            bodyRenderer.SetPropertyBlock(properties);
            properties.Clear();
        }
    }

    private static void SetProjectileColor(h980220_Projectile projectile, Color color)
    {
        Renderer[] renderers = projectile.GetComponentsInChildren<Renderer>(true);
        var properties = new MaterialPropertyBlock();
        foreach (Renderer projectileRenderer in renderers)
        {
            projectileRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            projectileRenderer.SetPropertyBlock(properties);
            properties.Clear();
        }
    }

    private void OnValidate()
    {
        requiredHits = Mathf.Max(1, requiredHits);
        movementSpeed = Mathf.Max(0f, movementSpeed);
        contactRange = Mathf.Max(0f, contactRange);
        fireInterval = Mathf.Max(0f, fireInterval);
        danceHeight = Mathf.Max(0f, danceHeight);
        danceLeanDegrees = Mathf.Max(0f, danceLeanDegrees);
    }
}
