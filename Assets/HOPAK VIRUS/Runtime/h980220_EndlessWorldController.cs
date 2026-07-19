using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class h980220_EnemySpawnSettings
{
    [Header("Spawn Counts")]
    [Min(1)] public int initialEnemyCount = 8;
    [Min(1)] public int initialMaximumEnemies = 8;
    [Min(1)] public int maximumEnemies = 40;
    [Min(1)] public int maximumTotalCharacters = 70;
    [Min(1)] public int enemyLimitIncreasePerInterval = 5;

    [Header("Spawn Timing / Distance")]
    [Min(1f)] public float enemyIncreaseInterval = 20f;
    [Min(0.1f)] public float offscreenSpawnInterval = 2.5f;
    [Min(1)] public int offscreenEnemiesPerWave = 2;
    [Min(2f)] public float spawnRadiusMin = 18f;
    [Min(2f)] public float spawnRadiusMax = 28f;
    [Min(5f)] public float despawnDistance = 60f;

    [Header("Medic")]
    [Min(0f)] public float medicStartTime = 15f;
    [Range(0f, 1f)] public float medicStartingChance = 0.10f;
    [Range(0f, 1f)] public float medicMaximumChance = 0.28f;
    [Min(0f)] public float medicChanceIncreasePerSecond = 0.001f;
    [InspectorName("기본 발사 속도 (초당 횟수)")]
    [Min(0.05f)] public float medicShotsPerSecond = 1f;
    [InspectorName("치료제 이동 속도")]
    [Min(0.1f)] public float medicBaseProjectileSpeed = 7f;
    [InspectorName("치료제 사거리")]
    [Min(0.5f)] public float medicProjectileRange = 12f;

    [Header("Police")]
    [Min(0f)] public float policeStartTime = 45f;
    [Range(0f, 1f)] public float policeStartingChance = 0.04f;
    [Range(0f, 1f)] public float policeMaximumChance = 0.18f;
    [Min(0f)] public float policeChanceIncreasePerSecond = 0.001f;
    public bool guaranteeFirstSpecialSpawn = true;

    [Header("시민 벽 회피")]
    [Min(1f)] public float civilianWallAvoidanceDistance = 14f;
    [Min(0f)] public float civilianWallAvoidanceStrength = 2.5f;

    [Header("Final Minute Pressure")]
    [Min(1f)] public float policeEndSpeedMultiplier = 1.9f;
    [Min(1f)] public float medicEndProjectileSpeedMultiplier = 1.55f;
    [Min(1f)] public float medicEndFireRateMultiplier = 1.45f;

    [Header("스테이지 2")]
    [Min(1f)] public float stageDuration = 120f;
    [Min(1f)] public float stageTwoEnemySpeedMultiplier = 1.35f;
    [Range(4, 30)] public int stageTwoObstacleCount = 16;

    public void Sanitize()
    {
        initialEnemyCount = Mathf.Max(1, initialEnemyCount);
        initialMaximumEnemies = Mathf.Max(1, initialMaximumEnemies);
        maximumEnemies = Mathf.Max(initialMaximumEnemies, maximumEnemies);
        maximumTotalCharacters = Mathf.Max(maximumEnemies, maximumTotalCharacters);
        enemyLimitIncreasePerInterval = Mathf.Max(1, enemyLimitIncreasePerInterval);
        enemyIncreaseInterval = Mathf.Max(1f, enemyIncreaseInterval);
        offscreenSpawnInterval = Mathf.Max(0.1f, offscreenSpawnInterval);
        offscreenEnemiesPerWave = Mathf.Max(1, offscreenEnemiesPerWave);
        spawnRadiusMin = Mathf.Max(2f, spawnRadiusMin);
        spawnRadiusMax = Mathf.Max(spawnRadiusMin, spawnRadiusMax);
        despawnDistance = Mathf.Max(spawnRadiusMax + 5f, despawnDistance);
        medicStartTime = Mathf.Max(0f, medicStartTime);
        medicStartingChance = Mathf.Clamp01(medicStartingChance);
        medicMaximumChance = Mathf.Clamp(medicMaximumChance, medicStartingChance, 1f);
        medicChanceIncreasePerSecond = Mathf.Max(0f, medicChanceIncreasePerSecond);
        medicShotsPerSecond = Mathf.Max(0.05f, medicShotsPerSecond);
        medicBaseProjectileSpeed = Mathf.Max(0.1f, medicBaseProjectileSpeed);
        medicProjectileRange = Mathf.Max(0.5f, medicProjectileRange);
        policeStartTime = Mathf.Max(0f, policeStartTime);
        policeStartingChance = Mathf.Clamp01(policeStartingChance);
        policeMaximumChance = Mathf.Clamp(policeMaximumChance, policeStartingChance, 1f);
        policeChanceIncreasePerSecond = Mathf.Max(0f, policeChanceIncreasePerSecond);
        civilianWallAvoidanceDistance = Mathf.Max(1f, civilianWallAvoidanceDistance);
        civilianWallAvoidanceStrength = Mathf.Max(0f, civilianWallAvoidanceStrength);
        policeEndSpeedMultiplier = Mathf.Max(1f, policeEndSpeedMultiplier);
        medicEndProjectileSpeedMultiplier = Mathf.Max(1f, medicEndProjectileSpeedMultiplier);
        medicEndFireRateMultiplier = Mathf.Max(1f, medicEndFireRateMultiplier);
        stageDuration = Mathf.Max(1f, stageDuration);
        stageTwoEnemySpeedMultiplier = Mathf.Max(1f, stageTwoEnemySpeedMultiplier);
        stageTwoObstacleCount = Mathf.Clamp(stageTwoObstacleCount, 4, 30);
    }
}

public sealed class h980220_EndlessWorldController : MonoBehaviour
{
    [Header("Infinite Map")]
    [SerializeField] private float chunkSize = 30f;
    [SerializeField] private int chunkRadius = 2;
    [SerializeField] private float checkerSize = 5f;

    private h980220_EnemySpawnSettings spawnSettings = new h980220_EnemySpawnSettings();

    private static readonly Color DarkFloor = new Color(0.082f, 0.094f, 0.125f, 1f);
    private static readonly Color LightFloor = new Color(0.145f, 0.173f, 0.22f, 1f);

    private readonly List<Transform> chunks = new List<Transform>();
    private readonly List<h980220_EnemyController> spawnedEnemies =
        new List<h980220_EnemyController>();

    private Transform player;
    private Transform chunkRoot;
    private Transform enemyRoot;
    private Transform stageTwoObstacleRoot;
    private Material darkFloorMaterial;
    private Material lightFloorMaterial;
    private h980220_Projectile cureProjectilePrefab;
    private bool simulationEnabled;
    private float elapsed;
    private bool survivalCompleted;
    private float nextOffscreenSpawnTime;
    private Camera gameplayCamera;
    private bool hasSpawnedMedic;
    private bool hasSpawnedPolice;
    private Vector3 arenaCenter;
    private float arenaHalfExtent;
    private int currentStage = 1;
    private int nextWallSpawnIndex;
    private bool gameCompleted;

    public event System.Action Survived;
    public event System.Action StageTwoStarted;
    public event System.Action<h980220_EnemyController> EnemySpawned;
    public float RemainingTime
    {
        get
        {
            float stageElapsed = currentStage == 1
                ? elapsed
                : elapsed - spawnSettings.stageDuration;
            return Mathf.Max(0f, spawnSettings.stageDuration - stageElapsed);
        }
    }
    public int CurrentStage => currentStage;
    public Vector3 ArenaCenter => arenaCenter;
    public float ArenaSize => arenaHalfExtent * 2f;
    public float SurvivalProgress => spawnSettings.stageDuration <= 0f
        ? 0f : Mathf.Clamp01(elapsed / (spawnSettings.stageDuration * 2f));
    private float SurvivalPressure => SurvivalProgress * SurvivalProgress;
    public float PoliceSpeedMultiplier => Mathf.Lerp(
        1f, spawnSettings.policeEndSpeedMultiplier, SurvivalPressure);
    public float MedicProjectileSpeedMultiplier => Mathf.Lerp(
        1f, spawnSettings.medicEndProjectileSpeedMultiplier, SurvivalPressure);
    public float MedicFireRateMultiplier => Mathf.Lerp(
        1f, spawnSettings.medicEndFireRateMultiplier, SurvivalPressure);
    public float StageEnemySpeedMultiplier => currentStage >= 2
        ? spawnSettings.stageTwoEnemySpeedMultiplier : 1f;

    public Vector3 GetCivilianWallAvoidance(Vector3 position)
    {
        if (arenaHalfExtent <= 0f)
            return Vector3.zero;

        float distance = spawnSettings.civilianWallAvoidanceDistance;
        float minX = arenaCenter.x - arenaHalfExtent;
        float maxX = arenaCenter.x + arenaHalfExtent;
        float minZ = arenaCenter.z - arenaHalfExtent;
        float maxZ = arenaCenter.z + arenaHalfExtent;
        Vector3 avoidance = Vector3.zero;
        avoidance += Vector3.right * WallPressure(position.x - minX, distance);
        avoidance += Vector3.left * WallPressure(maxX - position.x, distance);
        avoidance += Vector3.forward * WallPressure(position.z - minZ, distance);
        avoidance += Vector3.back * WallPressure(maxZ - position.z, distance);
        return avoidance * spawnSettings.civilianWallAvoidanceStrength;
    }

    private static float WallPressure(float distanceFromWall, float avoidanceDistance)
    {
        return Mathf.Clamp01(1f - distanceFromWall / Mathf.Max(0.01f, avoidanceDistance));
    }

    public void ConfigureSpawning(h980220_EnemySpawnSettings settings)
    {
        spawnSettings = settings ?? new h980220_EnemySpawnSettings();
        spawnSettings.Sanitize();
    }

    public void Initialize(
        Transform playerTransform, h980220_RoomController[] fixedRooms)
    {
        player = playerTransform;
        CaptureCureProjectile();
        CaptureFloorMaterial();
        DisableFixedRooms(fixedRooms);
        BuildChunkPool();
        LayoutFixedArena();
        BuildBoundaryWalls();
        BuildStageTwoObstacles();
        gameCompleted = false;
        simulationEnabled = false;
    }

    public void SetSimulationEnabled(bool enabled)
    {
        if (enabled && gameCompleted)
            return;

        simulationEnabled = enabled;
        if (enabled)
        {
            elapsed = 0f;
            survivalCompleted = false;
            currentStage = 1;
            SetStageTwoObstaclesActive(false);
            nextOffscreenSpawnTime = spawnSettings.offscreenSpawnInterval;
            gameplayCamera = Camera.main;
            if (spawnedEnemies.Count == 0)
                SpawnInitialEnemies();
        }

        foreach (h980220_EnemyController enemy in spawnedEnemies)
        {
            if (enemy != null)
                enemy.SetCombatEnabled(enabled);
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        CleanupEnemies();

        if (!simulationEnabled)
            return;

        elapsed += Time.deltaTime;
        if (currentStage == 1 && elapsed >= spawnSettings.stageDuration)
            BeginStageTwo();
        if (!survivalCompleted && elapsed >= spawnSettings.stageDuration * 2f)
        {
            survivalCompleted = true;
            gameCompleted = true;
            simulationEnabled = false;
            Survived?.Invoke();
            return;
        }

        TrySpawnOffscreenWave();
    }

    private void BuildChunkPool()
    {
        if (chunkRoot != null)
            return;

        chunkRoot = new GameObject("h980220_InfiniteMap").transform;
        enemyRoot = new GameObject("h980220_EndlessEnemies").transform;
        int diameter = chunkRadius * 2 + 1;
        for (int i = 0; i < diameter * diameter; i++)
            chunks.Add(CreateChunk(i));
    }

    private Transform CreateChunk(int index)
    {
        var chunk = new GameObject($"Map Chunk {index + 1}");
        chunk.transform.SetParent(chunkRoot, false);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(chunk.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(chunkSize, 0.5f, chunkSize);
        floor.GetComponent<Renderer>().sharedMaterial = darkFloorMaterial;

        int cells = Mathf.CeilToInt(chunkSize / checkerSize);
        float start = -chunkSize * 0.5f + checkerSize * 0.5f;
        for (int z = 0; z < cells; z++)
        {
            for (int x = 0; x < cells; x++)
            {
                if ((x + z) % 2 == 0)
                    continue;

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "Checker Tile";
                tile.transform.SetParent(chunk.transform, false);
                tile.transform.localPosition = new Vector3(
                    start + x * checkerSize, 0.008f, start + z * checkerSize);
                tile.transform.localScale = new Vector3(
                    checkerSize, 0.012f, checkerSize);
                tile.GetComponent<Renderer>().sharedMaterial = lightFloorMaterial;
                DisableAndDestroyCollider(tile.GetComponent<Collider>());
            }
        }

        return chunk.transform;
    }

    private void LayoutFixedArena()
    {
        arenaCenter = player == null
            ? Vector3.zero
            : new Vector3(player.position.x, 0f, player.position.z);
        int diameter = chunkRadius * 2 + 1;
        arenaHalfExtent = diameter * chunkSize * 0.5f;
        int index = 0;
        for (int z = -chunkRadius; z <= chunkRadius; z++)
        {
            for (int x = -chunkRadius; x <= chunkRadius; x++)
            {
                chunks[index++].position = arenaCenter +
                    new Vector3(x * chunkSize, 0f, z * chunkSize);
            }
        }
    }

    private void BuildBoundaryWalls()
    {
        if (chunkRoot == null || chunkRoot.Find("Arena Walls") != null)
            return;

        Transform walls = new GameObject("Arena Walls").transform;
        walls.SetParent(chunkRoot, false);
        walls.position = arenaCenter;
        float size = arenaHalfExtent * 2f;
        CreateWall(walls, "North Wall", new Vector3(0f, 4f, arenaHalfExtent),
            new Vector3(size + 2f, 8f, 1f));
        CreateWall(walls, "South Wall", new Vector3(0f, 4f, -arenaHalfExtent),
            new Vector3(size + 2f, 8f, 1f));
        CreateWall(walls, "East Wall", new Vector3(arenaHalfExtent, 4f, 0f),
            new Vector3(1f, 8f, size + 2f));
        CreateWall(walls, "West Wall", new Vector3(-arenaHalfExtent, 4f, 0f),
            new Vector3(1f, 8f, size + 2f));
    }

    private void CreateWall(Transform parent, string wallName,
        Vector3 localPosition, Vector3 localScale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPosition;
        wall.transform.localScale = localScale;
        wall.GetComponent<Renderer>().sharedMaterial = lightFloorMaterial;
    }

    private void BuildStageTwoObstacles()
    {
        if (stageTwoObstacleRoot != null)
            return;

        stageTwoObstacleRoot = new GameObject("Stage 2 Obstacles").transform;
        stageTwoObstacleRoot.SetParent(chunkRoot, false);
        stageTwoObstacleRoot.position = arenaCenter;
        int count = spawnSettings.stageTwoObstacleCount;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2f * i / count + (i % 2) * 0.18f;
            float radius = i % 2 == 0 ? arenaHalfExtent * 0.38f : arenaHalfExtent * 0.68f;
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obstacle.name = $"장애물 {i + 1}";
            obstacle.transform.SetParent(stageTwoObstacleRoot, false);
            obstacle.transform.localPosition = new Vector3(
                Mathf.Sin(angle) * radius, 3f, Mathf.Cos(angle) * radius);
            float width = 3.2f + (i % 3) * 0.55f;
            obstacle.transform.localScale = new Vector3(width, 3f, width);
            obstacle.GetComponent<Renderer>().sharedMaterial = lightFloorMaterial;
        }
        stageTwoObstacleRoot.gameObject.SetActive(false);
    }

    private void BeginStageTwo()
    {
        if (gameCompleted || currentStage != 1)
            return;
        currentStage = 2;
        SetStageTwoObstaclesActive(true);
        StageTwoStarted?.Invoke();
    }

    private void SetStageTwoObstaclesActive(bool active)
    {
        if (stageTwoObstacleRoot == null)
            return;
        stageTwoObstacleRoot.gameObject.SetActive(active);
        if (!active || player == null)
            return;

        foreach (Transform obstacle in stageTwoObstacleRoot)
        {
            Vector3 offset = obstacle.position - player.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 36f)
                obstacle.gameObject.SetActive(false);
        }
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < spawnSettings.initialEnemyCount; i++)
        {
            float angle = (Mathf.PI * 2f * i / Mathf.Max(1, spawnSettings.initialEnemyCount)) +
                          Random.Range(-0.2f, 0.2f);
            float radius = Random.Range(spawnSettings.spawnRadiusMin, spawnSettings.spawnRadiusMax);
            Vector3 position = player.position + new Vector3(
                Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
            SpawnEnemy(ChooseEnemyType(), position);
        }
    }

    private int CurrentEnemyLimit()
    {
        return Mathf.Min(spawnSettings.maximumEnemies,
            spawnSettings.initialMaximumEnemies +
            Mathf.FloorToInt(elapsed / spawnSettings.enemyIncreaseInterval) *
            spawnSettings.enemyLimitIncreasePerInterval);
    }

    private void TrySpawnOffscreenWave()
    {
        if (elapsed < nextOffscreenSpawnTime)
            return;

        nextOffscreenSpawnTime = elapsed + spawnSettings.offscreenSpawnInterval;
        int availableHostiles = Mathf.Max(0, CurrentEnemyLimit() - CountActiveHostiles());
        bool guaranteedSpecialDue = spawnSettings.guaranteeFirstSpecialSpawn &&
            ((!hasSpawnedMedic && elapsed >= spawnSettings.medicStartTime) ||
             (!hasSpawnedPolice && elapsed >= spawnSettings.policeStartTime));
        if (guaranteedSpecialDue)
            availableHostiles = Mathf.Max(1, availableHostiles);
        int availableCharacters = Mathf.Max(0,
            spawnSettings.maximumTotalCharacters - spawnedEnemies.Count);
        int growth = Mathf.FloorToInt(elapsed / spawnSettings.enemyIncreaseInterval);
        int requested = spawnSettings.offscreenEnemiesPerWave + growth;
        SpawnOffscreenEnemies(Mathf.Min(requested,
            Mathf.Min(availableHostiles, availableCharacters)));
    }

    private void SpawnOffscreenEnemies(int requestedCount)
    {
        if (requestedCount <= 0 || player == null)
            return;

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        Plane[] cameraPlanes = gameplayCamera == null
            ? null
            : GeometryUtility.CalculateFrustumPlanes(gameplayCamera);

        int spawned = 0;
        int failedCycles = 0;
        while (spawned < requestedCount && failedCycles < 4)
        {
            int wallIndex = nextWallSpawnIndex++ % 4;
            float laneOffset = ((spawned % 5) - 2) * 1.6f;
            Vector3 position = WallSpawnPosition(wallIndex, laneOffset);
            if (!IsOutsideCamera(position, cameraPlanes))
            {
                failedCycles++;
                continue;
            }

            SpawnEnemy(ChooseGuaranteedEnemyType(), position);
            spawned++;
            failedCycles = 0;
        }
    }

    private Vector3 WallSpawnPosition(int wallIndex, float laneOffset)
    {
        float inside = Mathf.Max(2f, arenaHalfExtent - 2.5f);
        switch (wallIndex)
        {
            case 0:
                return arenaCenter + new Vector3(laneOffset, 0f, inside);
            case 1:
                return arenaCenter + new Vector3(inside, 0f, laneOffset);
            case 2:
                return arenaCenter + new Vector3(laneOffset, 0f, -inside);
            default:
                return arenaCenter + new Vector3(-inside, 0f, laneOffset);
        }
    }

    private bool IsOutsideCamera(Vector3 position, Plane[] cameraPlanes)
    {
        if (cameraPlanes == null)
            return true;
        Bounds enemyBounds = new Bounds(position + Vector3.up * 1.75f,
            new Vector3(1.6f, 3.5f, 1.6f));
        return !GeometryUtility.TestPlanesAABB(cameraPlanes, enemyBounds);
    }

    private h980220_EnemyType ChooseGuaranteedEnemyType()
    {
        if (spawnSettings.guaranteeFirstSpecialSpawn)
        {
            if (!hasSpawnedMedic && elapsed >= spawnSettings.medicStartTime)
                return h980220_EnemyType.Ranged;
            if (!hasSpawnedPolice && elapsed >= spawnSettings.policeStartTime)
                return h980220_EnemyType.Elite;
        }
        return ChooseEnemyType();
    }

    private void SpawnEnemy(h980220_EnemyType type, Vector3 position)
    {

        var enemyObject = new GameObject($"Spawned {DisplayName(type)}");
        enemyObject.transform.SetParent(enemyRoot, true);
        enemyObject.transform.position = position;

        CharacterController controller = enemyObject.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.75f, 0f);
        controller.height = 3.5f;
        controller.radius = 0.6f;

        h980220_EnemyController enemy =
            enemyObject.AddComponent<h980220_EnemyController>();
        enemy.InitializeRuntime(type, player, cureProjectilePrefab, this,
            lightFloorMaterial);
        if (type == h980220_EnemyType.Ranged)
        {
            enemy.ConfigureMedicAttack(
                spawnSettings.medicShotsPerSecond,
                spawnSettings.medicBaseProjectileSpeed,
                spawnSettings.medicProjectileRange);
        }
        spawnedEnemies.Add(enemy);
        if (type == h980220_EnemyType.Ranged)
            hasSpawnedMedic = true;
        else if (type == h980220_EnemyType.Elite)
            hasSpawnedPolice = true;
        EnemySpawned?.Invoke(enemy);
    }

    private h980220_EnemyType ChooseEnemyType()
    {
        float policeChance = elapsed < spawnSettings.policeStartTime
            ? 0f
            : Mathf.Min(spawnSettings.policeMaximumChance,
                spawnSettings.policeStartingChance +
                (elapsed - spawnSettings.policeStartTime) *
                spawnSettings.policeChanceIncreasePerSecond);
        float medicChance = elapsed < spawnSettings.medicStartTime
            ? 0f
            : Mathf.Min(spawnSettings.medicMaximumChance,
                spawnSettings.medicStartingChance +
                (elapsed - spawnSettings.medicStartTime) *
                spawnSettings.medicChanceIncreasePerSecond);
        float roll = Random.value;
        if (roll < policeChance)
            return h980220_EnemyType.Elite;
        if (roll < policeChance + medicChance)
            return h980220_EnemyType.Ranged;
        return h980220_EnemyType.Basic;
    }

    private void CleanupEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            h980220_EnemyController enemy = spawnedEnemies[i];
            bool remove = enemy == null || !enemy.gameObject.activeSelf;

            if (!remove && spawnedEnemies.Count <= spawnSettings.maximumTotalCharacters)
                continue;

            spawnedEnemies.RemoveAt(i);
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
    }

    private int CountActiveHostiles()
    {
        int count = 0;
        foreach (h980220_EnemyController enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf && !enemy.IsInfected)
                count++;
        }
        return count;
    }

    private void CaptureCureProjectile()
    {
        foreach (h980220_EnemyController enemy in
                 Object.FindObjectsByType<h980220_EnemyController>(FindObjectsSortMode.None))
        {
            if (enemy != null && enemy.CureProjectilePrefab != null)
            {
                cureProjectilePrefab = enemy.CureProjectilePrefab;
                return;
            }
        }
    }

    private void CaptureFloorMaterial()
    {
        Material source = null;
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (renderer != null && renderer.gameObject.name == "Floor")
            {
                source = renderer.sharedMaterial;
                break;
            }
        }

        Shader shader = source != null
            ? source.shader
            : Shader.Find("Universal Render Pipeline/Lit");
        darkFloorMaterial = source != null ? new Material(source) : new Material(shader);
        lightFloorMaterial = source != null ? new Material(source) : new Material(shader);
        SetColor(darkFloorMaterial, DarkFloor);
        SetColor(lightFloorMaterial, LightFloor);
    }

    private static void DisableFixedRooms(h980220_RoomController[] fixedRooms)
    {
        if (fixedRooms == null)
            return;
        foreach (h980220_RoomController room in fixedRooms)
        {
            if (room != null)
                room.gameObject.SetActive(false);
        }
    }

    private static string DisplayName(h980220_EnemyType type)
    {
        switch (type)
        {
            case h980220_EnemyType.Ranged: return "메딕";
            case h980220_EnemyType.Elite: return "경찰";
            default: return "시민";
        }
    }

    private static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void DisableAndDestroyCollider(Collider collider)
    {
        if (collider == null)
            return;
        collider.enabled = false;
        Destroy(collider);
    }

    private void OnValidate()
    {
        chunkSize = Mathf.Max(4f, chunkSize);
        chunkRadius = Mathf.Max(1, chunkRadius);
        checkerSize = Mathf.Clamp(checkerSize, 1f, chunkSize);
        spawnSettings?.Sanitize();
    }
}
