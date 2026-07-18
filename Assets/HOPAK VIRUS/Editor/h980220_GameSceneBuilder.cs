using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class h980220_GameSceneBuilder
{
    public const string ScenePath = "Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity";

    private const string RootPath = "Assets/HOPAK VIRUS";
    private const string MaterialsPath = RootPath + "/Materials";
    private const string PrefabsPath = RootPath + "/Prefabs";
    private const string ScenesPath = RootPath + "/Scenes";

    [MenuItem("HOPAK VIRUS/Build Game Scene")]
    public static void BuildScene()
    {
        EnsureFolders();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material purple = MaterialAt("Purple", new Color(0.55f, 0.10f, 0.75f));
        Material cyan = MaterialAt("Cyan", new Color(0.20f, 0.85f, 0.90f));
        Material white = MaterialAt("White", new Color(0.95f, 0.95f, 1f));
        Material grayFloor = MaterialAt("GrayFloor", new Color(0.18f, 0.20f, 0.23f));
        Material grayWall = MaterialAt("GrayWall", new Color(0.30f, 0.32f, 0.36f));
        Material skin = MaterialAt("Skin", new Color(0.76f, 0.65f, 0.55f));

        ProjectilePrefab("VirusProjectile", purple);
        ProjectilePrefab("CureProjectile", white);
        h980220_Projectile virusProjectile = LoadProjectilePrefab("VirusProjectile");
        h980220_Projectile cureProjectile = LoadProjectilePrefab("CureProjectile");

        h980220_PlayerRhythmController rhythm;
        h980220_PlayerCombat combat;
        h980220_PlayerInfection infection;
        Renderer[] playerRenderers;
        GameObject player = CreatePlayer(
            skin, virusProjectile, out rhythm, out combat, out infection, out playerRenderers);

        h980220_RoomController[] rooms = CreateRooms(
            player.transform, cureProjectile, cyan, grayFloor, grayWall);
        h980220_FollowCamera followCamera = CreateCamera(player.transform);
        CreateLighting();

        GameObject titlePanel;
        GameObject hudPanel;
        GameObject resultPanel;
        Text resultText;
        Text roomText;
        Image[] infectionMarks;
        CreateUi(out titlePanel, out hudPanel, out resultPanel,
            out resultText, out roomText, out infectionMarks);

        SetArray(infection, "bodyRenderers", playerRenderers);
        SetArray(infection, "hudIndicators", infectionMarks);
        CreateGameManager(titlePanel, hudPanel, resultPanel, resultText, roomText,
            rhythm, combat, infection, followCamera, rooms);

        ValidateGeneratedScene();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "HOPAK VIRUS");
        EnsureFolder(RootPath, "Materials");
        EnsureFolder(RootPath, "Prefabs");
        EnsureFolder(RootPath, "Scenes");
        EnsureFolder(RootPath, "Editor");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static Material MaterialAt(string fileName, Color color)
    {
        string path = $"{MaterialsPath}/{fileName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable.");

            material = new Material(shader) { name = fileName };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static h980220_Projectile ProjectilePrefab(string name, Material material)
    {
        GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        temporary.name = name;
        temporary.transform.localScale = Vector3.one * 0.3f;
        temporary.GetComponent<Renderer>().sharedMaterial = material;

        SphereCollider sphereCollider = temporary.GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        Rigidbody body = temporary.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        temporary.AddComponent<h980220_Projectile>();

        string path = $"{PrefabsPath}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path);
        UnityEngine.Object.DestroyImmediate(temporary);
        if (prefab == null)
            throw new InvalidOperationException($"Could not create projectile prefab at {path}.");

        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        h980220_Projectile projectile = savedPrefab == null
            ? null
            : savedPrefab.GetComponent<h980220_Projectile>();
        if (projectile == null)
            throw new InvalidOperationException($"Projectile component is missing from {path}.");
        return projectile;
    }

    private static h980220_Projectile LoadProjectilePrefab(string name)
    {
        string path = $"{PrefabsPath}/{name}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        h980220_Projectile projectile = prefab == null
            ? null
            : prefab.GetComponent<h980220_Projectile>();
        if (projectile == null)
            throw new InvalidOperationException($"Projectile component is missing from {path}.");
        return projectile;
    }

    private static GameObject CreatePlayer(
        Material skin,
        h980220_Projectile virusProjectile,
        out h980220_PlayerRhythmController rhythm,
        out h980220_PlayerCombat combat,
        out h980220_PlayerInfection infection,
        out Renderer[] renderers)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, -6f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.75f, 0f);
        controller.height = 3.5f;
        controller.radius = 0.6f;

        GameObject head = VisualCube("Head", player.transform,
            new Vector3(0f, 3.2f, 0f), new Vector3(0.9f, 0.9f, 0.9f), skin);
        GameObject torso = VisualCube("Torso", player.transform,
            new Vector3(0f, 2.1f, 0f), new Vector3(1.2f, 1.4f, 0.8f), skin);
        GameObject leftThigh = VisualCube("LeftThigh", player.transform,
            new Vector3(-0.4f, 1.2f, 0f), new Vector3(0.35f, 0.9f, 0.35f), skin);
        GameObject leftShin = VisualCube("LeftShin", leftThigh.transform,
            new Vector3(0f, -0.85f, 0f), new Vector3(0.9f, 0.9f, 0.9f), skin);
        GameObject rightThigh = VisualCube("RightThigh", player.transform,
            new Vector3(0.4f, 1.2f, 0f), new Vector3(0.35f, 0.9f, 0.35f), skin);
        GameObject rightShin = VisualCube("RightShin", rightThigh.transform,
            new Vector3(0f, -0.85f, 0f), new Vector3(0.9f, 0.9f, 0.9f), skin);

        rhythm = player.AddComponent<h980220_PlayerRhythmController>();
        combat = player.AddComponent<h980220_PlayerCombat>();
        infection = player.AddComponent<h980220_PlayerInfection>();
        SetReference(rhythm, "leftThigh", leftThigh.transform);
        SetReference(rhythm, "leftShin", leftShin.transform);
        SetReference(rhythm, "rightThigh", rightThigh.transform);
        SetReference(rhythm, "rightShin", rightShin.transform);

        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 2f, 0.8f);
        SetReference(combat, "projectilePrefab", virusProjectile);
        SetReference(combat, "firePoint", firePoint.transform);

        renderers = new[]
        {
            head.GetComponent<Renderer>(), torso.GetComponent<Renderer>(),
            leftThigh.GetComponent<Renderer>(), leftShin.GetComponent<Renderer>(),
            rightThigh.GetComponent<Renderer>(), rightShin.GetComponent<Renderer>()
        };
        return player;
    }

    private static GameObject VisualCube(
        string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static h980220_RoomController[] CreateRooms(
        Transform player, h980220_Projectile cureProjectile,
        Material enemyMaterial, Material floorMaterial, Material wallMaterial)
    {
        var room1 = new GameObject("Room 1 Plaza");
        Cube("Floor", room1.transform, new Vector3(0f, -0.25f, 0f),
            new Vector3(20f, 0.5f, 16f), floorMaterial);
        Transform room1Walls = Child("Walls", room1.transform);
        Cube("West Wall", room1Walls, new Vector3(-10f, 1.5f, 0f),
            new Vector3(0.5f, 3f, 16f), wallMaterial);
        Cube("East Wall", room1Walls, new Vector3(10f, 1.5f, 0f),
            new Vector3(0.5f, 3f, 16f), wallMaterial);
        Cube("South Wall", room1Walls, new Vector3(0f, 1.5f, -8f),
            new Vector3(20f, 3f, 0.5f), wallMaterial);
        Cube("North West Wall", room1Walls, new Vector3(-6f, 1.5f, 8f),
            new Vector3(8f, 3f, 0.5f), wallMaterial);
        Cube("North East Wall", room1Walls, new Vector3(6f, 1.5f, 8f),
            new Vector3(8f, 3f, 0.5f), wallMaterial);
        Transform gate1 = Cube("Gate 1", room1.transform, new Vector3(0f, 1.5f, 8f),
            new Vector3(4f, 3f, 0.5f), wallMaterial).transform;
        h980220_EnemyController[] room1Enemies =
        {
            CreateEnemy("Basic 1A", room1.transform, new Vector3(-5f, 0f, 1f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Basic 1B", room1.transform, new Vector3(0f, 0f, 5f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Basic 1C", room1.transform, new Vector3(5f, 0f, 1f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial)
        };
        h980220_RoomController controller1 = room1.AddComponent<h980220_RoomController>();
        controller1.Initialize(0, room1Enemies, gate1);

        var room2 = new GameObject("Room 2 Zigzag");
        Cube("Floor", room2.transform, new Vector3(0f, -0.25f, 21f),
            new Vector3(12f, 0.5f, 26f), floorMaterial);
        Transform room2Walls = Child("Outer Walls", room2.transform);
        Cube("West Wall", room2Walls, new Vector3(-6f, 1.5f, 21f),
            new Vector3(0.5f, 3f, 26f), wallMaterial);
        Cube("East Wall", room2Walls, new Vector3(6f, 1.5f, 21f),
            new Vector3(0.5f, 3f, 26f), wallMaterial);
        Transform zigzag = Child("Zigzag Walls", room2.transform);
        Cube("Zigzag 1", zigzag, new Vector3(-2f, 1.5f, 15f),
            new Vector3(8f, 3f, 1f), wallMaterial);
        Cube("Zigzag 2", zigzag, new Vector3(2f, 1.5f, 22f),
            new Vector3(8f, 3f, 1f), wallMaterial);
        Cube("Zigzag 3", zigzag, new Vector3(-2f, 1.5f, 29f),
            new Vector3(8f, 3f, 1f), wallMaterial);
        Transform gate2 = Cube("Gate 2", room2.transform, new Vector3(0f, 1.5f, 34f),
            new Vector3(4f, 3f, 0.5f), wallMaterial).transform;
        h980220_EnemyController[] room2Enemies =
        {
            CreateEnemy("Basic 2A", room2.transform, new Vector3(-4f, 0f, 18f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Basic 2B", room2.transform, new Vector3(4f, 0f, 27f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Ranged 2A", room2.transform, new Vector3(4f, 0f, 13f),
                h980220_EnemyType.Ranged, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Ranged 2B", room2.transform, new Vector3(-4f, 0f, 31f),
                h980220_EnemyType.Ranged, 1, player, cureProjectile, enemyMaterial)
        };
        h980220_RoomController controller2 = room2.AddComponent<h980220_RoomController>();
        controller2.Initialize(1, room2Enemies, gate2);

        var room3 = new GameObject("Room 3 Arena");
        GameObject arenaFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arenaFloor.name = "Floor";
        arenaFloor.transform.SetParent(room3.transform);
        arenaFloor.transform.position = new Vector3(0f, -0.25f, 48f);
        arenaFloor.transform.localScale = new Vector3(28f, 0.25f, 28f);
        arenaFloor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        Transform pillars = Child("Pillars", room3.transform);
        foreach (Vector3 position in new[]
                 {
                     new Vector3(-4f, 1.5f, 44f), new Vector3(4f, 1.5f, 44f),
                     new Vector3(-4f, 1.5f, 52f), new Vector3(4f, 1.5f, 52f)
                 })
        {
            Cube($"Pillar {pillars.childCount + 1}", pillars, position,
                new Vector3(2f, 3f, 2f), wallMaterial);
        }

        Transform boundary = Child("Boundary", room3.transform);
        for (int i = 0; i < 16; i++)
        {
            if (i == 8)
                continue;
            float angle = i * Mathf.PI * 2f / 16f;
            Vector3 position = new Vector3(
                Mathf.Sin(angle) * 13f, 1.5f, 48f + Mathf.Cos(angle) * 13f);
            GameObject segment = Cube($"Boundary {i + 1}", boundary, position,
                new Vector3(5.1f, 3f, 0.5f), wallMaterial);
            segment.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
        }

        h980220_EnemyController[] room3Enemies =
        {
            CreateEnemy("Basic 3A", room3.transform, new Vector3(-7f, 0f, 48f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Basic 3B", room3.transform, new Vector3(7f, 0f, 48f),
                h980220_EnemyType.Basic, 1, player, cureProjectile, enemyMaterial),
            CreateEnemy("Elite 3", room3.transform, new Vector3(0f, 0f, 48f),
                h980220_EnemyType.Elite, 3, player, cureProjectile, enemyMaterial)
        };
        h980220_RoomController controller3 = room3.AddComponent<h980220_RoomController>();
        controller3.Initialize(2, room3Enemies, null);

        return new[] { controller1, controller2, controller3 };
    }

    private static h980220_EnemyController CreateEnemy(
        string name, Transform parent, Vector3 position,
        h980220_EnemyType type, int hits, Transform player,
        h980220_Projectile cureProjectile, Material material)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        CharacterController controller = root.AddComponent<CharacterController>();
        controller.center = Vector3.up;
        controller.height = type == h980220_EnemyType.Elite ? 2.8f : 2f;
        controller.radius = type == h980220_EnemyType.Elite ? 0.8f : 0.55f;

        GameObject body = VisualCube("Body", root.transform, Vector3.up,
            type == h980220_EnemyType.Elite
                ? new Vector3(1.6f, 2.8f, 1.6f)
                : new Vector3(1f, 2f, 1f), material);
        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(root.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 1.2f, 0.7f);

        h980220_EnemyController enemy = root.AddComponent<h980220_EnemyController>();
        SetReference(enemy, "player", player);
        SetReference(enemy, "cureProjectilePrefab", cureProjectile);
        SetReference(enemy, "firePoint", firePoint.transform);
        SetArray(enemy, "bodyRenderers", new[] { body.GetComponent<Renderer>() });
        enemy.Configure(type, hits);
        return enemy;
    }

    private static h980220_FollowCamera CreateCamera(Transform player)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = player.TransformPoint(new Vector3(0f, 7f, -9f));
        cameraObject.transform.rotation = Quaternion.LookRotation(
            player.position + Vector3.up * 1.5f - cameraObject.transform.position);
        cameraObject.AddComponent<Camera>();
        h980220_FollowCamera followCamera = cameraObject.AddComponent<h980220_FollowCamera>();
        followCamera.SetTarget(player);
        return followCamera;
    }

    private static void CreateLighting()
    {
        var lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateUi(
        out GameObject titlePanel, out GameObject hudPanel, out GameObject resultPanel,
        out Text resultText, out Text roomText, out Image[] infectionMarks)
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            throw new InvalidOperationException("LegacyRuntime.ttf is unavailable.");

        titlePanel = Panel("TitlePanel", canvasObject.transform);
        Text title = UiText("TitleText", titlePanel.transform, font, 44, TextAnchor.MiddleCenter);
        title.text = "HOPAK VIRUS\n\nA / D: HOPAK STEPS\nLEFT / RIGHT: TURN\nSPACE: SPREAD VIRUS\nENTER: START";
        Stretch(title.rectTransform, new Vector2(300f, 180f), new Vector2(-300f, -180f));

        hudPanel = Panel("HudPanel", canvasObject.transform);
        infectionMarks = new Image[3];
        for (int i = 0; i < infectionMarks.Length; i++)
        {
            var markObject = new GameObject($"Infection Mark {i + 1}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markObject.transform.SetParent(hudPanel.transform, false);
            Image mark = markObject.GetComponent<Image>();
            mark.color = new Color(0.55f, 0.10f, 0.75f, 1f);
            mark.rectTransform.anchorMin = new Vector2(0f, 1f);
            mark.rectTransform.anchorMax = new Vector2(0f, 1f);
            mark.rectTransform.pivot = new Vector2(0f, 1f);
            mark.rectTransform.anchoredPosition = new Vector2(40f + i * 54f, -40f);
            mark.rectTransform.sizeDelta = new Vector2(38f, 38f);
            infectionMarks[i] = mark;
        }

        roomText = UiText("RoomText", hudPanel.transform, font, 30, TextAnchor.UpperRight);
        roomText.text = "ROOM 1/3";
        roomText.rectTransform.anchorMin = new Vector2(1f, 1f);
        roomText.rectTransform.anchorMax = new Vector2(1f, 1f);
        roomText.rectTransform.pivot = new Vector2(1f, 1f);
        roomText.rectTransform.anchoredPosition = new Vector2(-40f, -35f);
        roomText.rectTransform.sizeDelta = new Vector2(340f, 80f);

        resultPanel = Panel("ResultPanel", canvasObject.transform);
        resultText = UiText("ResultText", resultPanel.transform, font, 44, TextAnchor.MiddleCenter);
        resultText.text = "R: RESTART";
        Stretch(resultText.rectTransform, new Vector2(260f, 220f), new Vector2(-260f, -220f));

        titlePanel.SetActive(true);
        hudPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    private static GameObject Panel(string name, Transform parent)
    {
        var panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        return panel;
    }

    private static Text UiText(
        string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        var textObject = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
    }

    private static void CreateGameManager(
        GameObject titlePanel, GameObject hudPanel, GameObject resultPanel,
        Text resultText, Text roomText,
        h980220_PlayerRhythmController rhythm, h980220_PlayerCombat combat,
        h980220_PlayerInfection infection, h980220_FollowCamera followCamera,
        h980220_RoomController[] rooms)
    {
        var managerObject = new GameObject("Game Manager");
        h980220_GameManager manager = managerObject.AddComponent<h980220_GameManager>();
        SetReference(manager, "titlePanel", titlePanel);
        SetReference(manager, "hudPanel", hudPanel);
        SetReference(manager, "resultPanel", resultPanel);
        SetReference(manager, "resultText", resultText);
        SetReference(manager, "roomText", roomText);
        SetReference(manager, "playerRhythmController", rhythm);
        SetReference(manager, "playerCombat", combat);
        SetReference(manager, "playerInfection", infection);
        SetReference(manager, "followCamera", followCamera);
        SetArray(manager, "rooms", rooms);
    }

    private static Transform Child(string name, Transform parent)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    private static GameObject Cube(
        string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static void SetReference(
        UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (value == null)
            throw new InvalidOperationException($"Cannot assign null to {target.name}.{propertyName}.");
        var serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property {target.GetType().Name}.{propertyName} is missing.");
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(UnityEngine.Object target, string propertyName, T[] values)
        where T : UnityEngine.Object
    {
        if (values == null || values.Length == 0 || values.Any(value => value == null))
            throw new InvalidOperationException($"{target.name}.{propertyName} must be fully assigned.");
        var serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
            throw new InvalidOperationException(
                $"Serialized array {target.GetType().Name}.{propertyName} is missing.");
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateGeneratedScene()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            throw new InvalidOperationException("Generated scene has no Player root.");

        MeshRenderer[] playerVisuals = player.GetComponentsInChildren<MeshRenderer>(true);
        if (playerVisuals.Length != 6 ||
            playerVisuals.Any(renderer => renderer.GetComponent<Collider>() != null) ||
            playerVisuals.Any(renderer => renderer.GetComponent<MeshFilter>()?.sharedMesh?.name != "Cube"))
        {
            throw new InvalidOperationException(
                "Player must contain exactly six collider-free Cube visuals.");
        }

        h980220_RoomController[] rooms = UnityEngine.Object.FindObjectsByType<h980220_RoomController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        h980220_EnemyController[] enemies = UnityEngine.Object.FindObjectsByType<h980220_EnemyController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (rooms.Length != 3 || enemies.Length != 10)
            throw new InvalidOperationException("Generated scene requires three rooms and ten enemies.");

        string[] prohibited =
        {
            "Audio" + "Source", "Particle" + "System",
            "Trail" + "Renderer", "Line" + "Renderer"
        };
        foreach (Component component in SceneManager.GetActiveScene().GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Component>(true)))
        {
            if (prohibited.Contains(component.GetType().Name))
            {
                throw new InvalidOperationException(
                    $"Prohibited component generated: {component.GetType().Name}.");
            }
        }

        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None)
                 .Where(component => component.GetType().Assembly.GetName().Name ==
                                     "h980220_HopakVirus.Runtime"))
        {
            var serializedObject = new SerializedObject(behaviour);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script" ||
                    property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                bool optionalFinalExit = behaviour is h980220_RoomController &&
                                         behaviour.name == "Room 3 Arena" &&
                                         property.propertyPath == "exitDoor";
                if (!optionalFinalExit && property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        $"Missing serialized dependency: {behaviour.name}/" +
                        $"{behaviour.GetType().Name}.{property.propertyPath}.");
                }
            }
        }
    }
}
