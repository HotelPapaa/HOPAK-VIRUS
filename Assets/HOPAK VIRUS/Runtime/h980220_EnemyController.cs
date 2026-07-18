using System;
using UnityEngine;

public enum h980220_EnemyType
{
    [InspectorName("Pedestrian")]
    Basic,
    [InspectorName("Medic")]
    Ranged,
    [InspectorName("Police")]
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
    [InspectorName("Movement / Flee Speed")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float contactRange = 1.25f;
    [InspectorName("Projectile Respawn Time")]
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float projectileSizeMultiplier = 1f;
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
    private Transform cylinderTorso;
    private Transform cylinderLeftThigh;
    private Transform cylinderLeftShin;
    private Transform cylinderRightThigh;
    private Transform cylinderRightShin;
    private Vector3 cylinderTorsoBasePosition;
    private Quaternion cylinderTorsoBaseRotation;
    private Vector3 cylinderLeftThighBasePosition;
    private Vector3 cylinderLeftShinBasePosition;
    private Vector3 cylinderRightThighBasePosition;
    private Vector3 cylinderRightShinBasePosition;
    private Quaternion cylinderLeftThighBaseRotation;
    private Quaternion cylinderLeftShinBaseRotation;
    private Quaternion cylinderRightThighBaseRotation;
    private Quaternion cylinderRightShinBaseRotation;

    public event Action<h980220_EnemyController> Infected;

    public h980220_EnemyType EnemyType => enemyType;
    public bool IsPolice => enemyType == h980220_EnemyType.Elite;
    public int RequiredHits => requiredHits;
    public bool IsInfected { get; private set; }
    public bool IsCombatEnabled => combatEnabled && !IsInfected;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        EnsureCylinderModel();
        danceOrigin = transform.position;
        danceRotation = transform.rotation;
    }

    public void Configure(h980220_EnemyType type, int hitsRequired)
    {
        EnsureCylinderModel();
        enemyType = type;
        requiredHits = Mathf.Max(1, hitsRequired);
        receivedHits = 0;
        IsInfected = false;
        combatEnabled = true;
        nextFireTime = float.NegativeInfinity;
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        SetCollisionsEnabled(true);
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
        SetCollisionsEnabled(false);
        danceOrigin = transform.position;
        danceRotation = transform.rotation;
        Infected?.Invoke(this);
    }

    public bool ReceivePlayerContact(bool playerIsDashing)
    {
        if (IsInfected)
            return true;

        if (IsPolice)
        {
            if (!playerIsDashing)
                return false;

            DefeatPolice();
            return true;
        }

        ReceiveVirusHit();
        return true;
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
                MoveAwayFromPlayer(deltaTime);
                if (IsInfected)
                    return;
                break;
            case h980220_EnemyType.Ranged:
                FacePlayer();
                TryFireCure(now);
                break;
            case h980220_EnemyType.Elite:
                MoveTowardPlayer(deltaTime);
                if (IsInfected)
                    return;
                FacePlayer();
                break;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null)
            return;

        h980220_PlayerCombat playerCombat =
            hit.collider.GetComponentInParent<h980220_PlayerCombat>();
        playerCombat?.TryContactEnemy(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        h980220_PlayerCombat playerCombat =
            other.GetComponentInParent<h980220_PlayerCombat>();
        playerCombat?.TryContactEnemy(this);
    }

    private void DefeatPolice()
    {
        if (IsInfected)
            return;

        receivedHits = requiredHits;
        IsInfected = true;
        combatEnabled = false;
        SetCollisionsEnabled(false);
        RefreshColor();
        Infected?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void SetCollisionsEnabled(bool enabled)
    {
        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>(true))
        {
            if (bodyCollider != null)
                bodyCollider.enabled = enabled && bodyCollider is CharacterController;
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

    private void MoveAwayFromPlayer(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        Vector3 direction = -HorizontalDirectionToPlayer();
        if (direction == Vector3.zero)
            return;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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

        if (!HasClearContactLine())
            return;

        h980220_PlayerInfection infection = player.GetComponentInParent<h980220_PlayerInfection>();
        if (infection != null)
            infection.ReceiveCureAtTime(transform.position, now);
    }

    private bool HasClearContactLine()
    {
        Vector3 enemyPoint = ControllerCenter(transform);
        Vector3 playerPoint = ControllerCenter(player);
        Vector3 offset = playerPoint - enemyPoint;
        float distance = offset.magnitude;
        if (distance <= 0.001f)
            return true;

        foreach (RaycastHit hit in Physics.RaycastAll(
                     enemyPoint,
                     offset / distance,
                     distance,
                     Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null &&
                !BelongsToHierarchy(hit.collider.transform, transform) &&
                !BelongsToHierarchy(hit.collider.transform, player))
            {
                return false;
            }
        }

        return true;
    }

    private static Vector3 ControllerCenter(Transform actor)
    {
        CharacterController controller = actor.GetComponent<CharacterController>();
        return controller == null
            ? actor.position + Vector3.up
            : actor.TransformPoint(controller.center);
    }

    private static bool BelongsToHierarchy(Transform candidate, Transform actor)
    {
        return candidate == actor || candidate.IsChildOf(actor) || actor.IsChildOf(candidate);
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
        projectile.transform.localScale = Vector3.Scale(
            projectile.transform.localScale,
            Vector3.one * projectileSizeMultiplier);
        IgnoreShooterCollisions(projectile);
        SetProjectileColor(projectile, Color.white);
        projectile.Initialize(h980220_ProjectileKind.Cure, direction, projectileSpeed, 12f);
        nextFireTime = now + Mathf.Max(0f, fireInterval);
    }

    private void IgnoreShooterCollisions(h980220_Projectile projectile)
    {
        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider == null)
            return;

        foreach (Collider shooterCollider in GetComponentsInChildren<Collider>(true))
        {
            if (shooterCollider != null)
                Physics.IgnoreCollision(projectileCollider, shooterCollider, true);
        }
    }

    private Vector3 HorizontalDirectionToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    private void Dance(float now)
    {
        const float danceStepDuration = 0.5f;
        float step = now / danceStepDuration;
        int stepIndex = Mathf.FloorToInt(step);
        h980220_Leg activeLeg = stepIndex % 2 == 0
            ? h980220_Leg.Left
            : h980220_Leg.Right;
        h980220_LegPose pose = h980220_HopakPose.Evaluate(activeLeg, step - stepIndex);

        ApplyCylinderSegment(cylinderLeftThigh,
            cylinderLeftThighBasePosition, cylinderLeftThighBaseRotation,
            h980220_HopakPose.LeftThighTarget(activeLeg),
            h980220_HopakPose.LeftThighRotation(activeLeg), pose.Weight);
        ApplyCylinderSegment(cylinderLeftShin,
            cylinderLeftShinBasePosition, cylinderLeftShinBaseRotation,
            h980220_HopakPose.LeftShinTarget(activeLeg),
            h980220_HopakPose.LeftShinRotation(activeLeg), pose.Weight);
        ApplyCylinderSegment(cylinderRightThigh,
            cylinderRightThighBasePosition, cylinderRightThighBaseRotation,
            h980220_HopakPose.RightThighTarget(activeLeg),
            h980220_HopakPose.RightThighRotation(activeLeg), pose.Weight);
        ApplyCylinderSegment(cylinderRightShin,
            cylinderRightShinBasePosition, cylinderRightShinBaseRotation,
            h980220_HopakPose.RightShinTarget(activeLeg),
            h980220_HopakPose.RightShinRotation(activeLeg), pose.Weight);
        if (cylinderTorso != null)
        {
            float heightStrength = danceHeight / 0.2f;
            float leanStrength = danceLeanDegrees / 12f;
            Vector3 targetPosition = cylinderTorsoBasePosition +
                                     Vector3.down * (0.508f * heightStrength);
            cylinderTorso.localPosition = Vector3.Lerp(
                cylinderTorsoBasePosition, targetPosition, pose.Weight);
            cylinderTorso.localRotation = Quaternion.Slerp(
                cylinderTorsoBaseRotation,
                cylinderTorsoBaseRotation * h980220_HopakPose.TorsoRotation(
                    activeLeg, leanStrength), pose.Weight);
        }
    }

    private static void ApplyCylinderSegment(
        Transform segment, Vector3 basePosition, Quaternion baseRotation,
        Vector3 targetPosition, Quaternion targetRotation, float weight)
    {
        if (segment == null)
            return;
        segment.localPosition = Vector3.Lerp(basePosition, targetPosition, weight);
        segment.localRotation = Quaternion.Slerp(baseRotation, targetRotation, weight);
    }

    private void EnsureCylinderModel()
    {
        if (cylinderTorso != null)
            return;

        Transform modelRoot = transform.Find("h980220_CylinderModel");
        if (modelRoot != null)
        {
            CaptureCylinderModel(modelRoot);
            return;
        }

        Material sourceMaterial = null;
        foreach (Renderer oldRenderer in bodyRenderers)
        {
            if (oldRenderer == null)
                continue;

            if (sourceMaterial == null)
                sourceMaterial = oldRenderer.sharedMaterial;
            oldRenderer.gameObject.SetActive(false);
        }

        var modelObject = new GameObject("h980220_CylinderModel");
        modelObject.transform.SetParent(transform, false);
        modelRoot = modelObject.transform;

        cylinderTorso = CreateCylinder("Torso", modelRoot,
            new Vector3(0f, 2.1f, 0f), new Vector3(1.2f, 0.7f, 0.8f), sourceMaterial);
        cylinderLeftThigh = CreateCylinder("LeftThigh", modelRoot,
            new Vector3(-0.4f, 1.2f, 0f), new Vector3(0.35f, 0.58f, 0.35f), sourceMaterial);
        cylinderLeftShin = CreateCylinder("LeftShin", modelRoot,
            new Vector3(-0.4f, 0.435f, 0f), new Vector3(0.315f, 0.81f, 0.315f), sourceMaterial);
        cylinderRightThigh = CreateCylinder("RightThigh", modelRoot,
            new Vector3(0.4f, 1.2f, 0f), new Vector3(0.35f, 0.58f, 0.35f), sourceMaterial);
        cylinderRightShin = CreateCylinder("RightShin", modelRoot,
            new Vector3(0.4f, 0.435f, 0f), new Vector3(0.315f, 0.81f, 0.315f), sourceMaterial);

        bodyRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        cylinderTorsoBasePosition = cylinderTorso.localPosition;
        cylinderTorsoBaseRotation = cylinderTorso.localRotation;
        CaptureCylinderBaselines();

        if (characterController != null)
        {
            characterController.center = new Vector3(0f, 1.75f, 0f);
            characterController.height = 3.5f;
            characterController.radius = 0.6f;
        }
    }

    private void CaptureCylinderModel(Transform modelRoot)
    {
        cylinderTorso = modelRoot.Find("Torso");
        cylinderLeftThigh = modelRoot.Find("LeftThigh");
        cylinderRightThigh = modelRoot.Find("RightThigh");
        cylinderLeftShin = modelRoot.Find("LeftShin");
        cylinderRightShin = modelRoot.Find("RightShin");
        bodyRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (cylinderTorso != null)
        {
            cylinderTorsoBasePosition = cylinderTorso.localPosition;
            cylinderTorsoBaseRotation = cylinderTorso.localRotation;
        }
        CaptureCylinderBaselines();
    }

    private void CaptureCylinderBaselines()
    {
        if (cylinderLeftThigh != null)
        {
            cylinderLeftThighBasePosition = cylinderLeftThigh.localPosition;
            cylinderLeftThighBaseRotation = cylinderLeftThigh.localRotation;
        }
        if (cylinderLeftShin != null)
        {
            cylinderLeftShinBasePosition = cylinderLeftShin.localPosition;
            cylinderLeftShinBaseRotation = cylinderLeftShin.localRotation;
        }
        if (cylinderRightThigh != null)
        {
            cylinderRightThighBasePosition = cylinderRightThigh.localPosition;
            cylinderRightThighBaseRotation = cylinderRightThigh.localRotation;
        }
        if (cylinderRightShin != null)
        {
            cylinderRightShinBasePosition = cylinderRightShin.localPosition;
            cylinderRightShinBaseRotation = cylinderRightShin.localRotation;
        }
    }

    private static Transform CreateCylinder(
        string name, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localScale = localScale;

        Collider primitiveCollider = cylinder.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            primitiveCollider.enabled = false;
            if (Application.isPlaying)
                Destroy(primitiveCollider);
            else
                DestroyImmediate(primitiveCollider);
        }

        if (material != null)
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
        return cylinder.transform;
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
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileSizeMultiplier = Mathf.Max(0.05f, projectileSizeMultiplier);
        danceHeight = Mathf.Max(0f, danceHeight);
        danceLeanDegrees = Mathf.Max(0f, danceLeanDegrees);
    }
}
