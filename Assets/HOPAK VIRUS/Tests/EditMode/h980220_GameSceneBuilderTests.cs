using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class h980220_GameSceneBuilderTests
{
    private const string ScenePath = "Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity";
    private Scene scene;

    [OneTimeSetUp]
    public void BuildAndReloadScene()
    {
        Type builder = Type.GetType(
            "h980220_GameSceneBuilder, h980220_HopakVirus.Editor",
            throwOnError: false);
        Assert.That(builder, Is.Not.Null, "Task 7 editor scene builder assembly is missing.");

        MethodInfo buildScene = builder.GetMethod(
            "BuildScene", BindingFlags.Public | BindingFlags.Static);
        Assert.That(buildScene, Is.Not.Null, "BuildScene entry point is missing.");
        Assert.DoesNotThrow(() => buildScene.Invoke(null, null));

        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
    }

    [OneTimeTearDown]
    public void LeaveCleanEditorScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void PlayerIsExactlySixColliderFreeCubeVisualsWithRootController()
    {
        GameObject player = Find("Player");
        MeshRenderer[] visuals = player.GetComponentsInChildren<MeshRenderer>(true);

        Assert.That(visuals.Select(renderer => renderer.gameObject.name), Is.EquivalentTo(new[]
        {
            "Head", "Torso", "LeftThigh", "LeftShin", "RightThigh", "RightShin"
        }));
        Assert.That(visuals, Has.Length.EqualTo(6));
        Assert.That(visuals.All(renderer =>
            renderer.GetComponent<MeshFilter>()?.sharedMesh?.name == "Cube"), Is.True);
        Assert.That(visuals.All(renderer => renderer.GetComponent<Collider>() == null), Is.True);
        Assert.That(player.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));

        CharacterController controller = player.GetComponent<CharacterController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(controller.center, Is.EqualTo(new Vector3(0f, 1.75f, 0f)));
        Assert.That(controller.height, Is.EqualTo(3.5f).Within(0.001f));
        Assert.That(controller.radius, Is.EqualTo(0.6f).Within(0.001f));
    }

    [Test]
    public void ThreeConnectedRoomsHaveExpectedLayoutEnemiesHitsAndGates()
    {
        h980220_RoomController[] rooms = UnityEngine.Object.FindObjectsByType<h980220_RoomController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(rooms.Select(room => room.name), Is.EquivalentTo(new[]
        {
            "Room 1 Plaza", "Room 2 Zigzag", "Room 3 Arena"
        }));

        Assert.That(Find("Room 1 Plaza/Floor").transform.localScale,
            Is.EqualTo(new Vector3(20f, 0.5f, 16f)));
        Assert.That(Find("Room 2 Zigzag/Zigzag Walls").transform.childCount, Is.EqualTo(3));
        Assert.That(Find("Room 3 Arena/Pillars").transform.childCount, Is.EqualTo(4));
        Assert.That(Find("Room 3 Arena/Boundary").transform.childCount, Is.EqualTo(15));

        h980220_EnemyController[] enemies = UnityEngine.Object.FindObjectsByType<h980220_EnemyController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(enemies, Has.Length.EqualTo(10));
        Assert.That(enemies.Count(enemy => enemy.EnemyType == h980220_EnemyType.Basic), Is.EqualTo(7));
        Assert.That(enemies.Count(enemy => enemy.EnemyType == h980220_EnemyType.Ranged), Is.EqualTo(2));
        Assert.That(enemies.Count(enemy => enemy.EnemyType == h980220_EnemyType.Elite), Is.EqualTo(1));
        Assert.That(enemies.Where(enemy => enemy.EnemyType != h980220_EnemyType.Elite),
            Has.All.Matches<h980220_EnemyController>(enemy => enemy.RequiredHits == 1));
        Assert.That(enemies.Single(enemy => enemy.EnemyType == h980220_EnemyType.Elite).RequiredHits,
            Is.EqualTo(3));

        Assert.That(Find("Room 1 Plaza/Gate 1"), Is.Not.Null);
        Assert.That(Find("Room 2 Zigzag/Gate 2"), Is.Not.Null);
        Assert.That(FindOptional("Room 3 Arena/Gate 3"), Is.Null);
    }

    [Test]
    public void EveryRuntimeSerializedDependencyIsAssignedAndRoomsSurviveReload()
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours.Where(IsRuntimeComponent))
        {
            var serializedObject = new SerializedObject(behaviour);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath != "m_Script" &&
                    property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    bool optionalFinalExit = behaviour is h980220_RoomController &&
                                             behaviour.name == "Room 3 Arena" &&
                                             property.propertyPath == "exitDoor";
                    if (optionalFinalExit)
                        continue;

                    Assert.That(property.objectReferenceValue, Is.Not.Null,
                        $"{behaviour.name}/{behaviour.GetType().Name}.{property.propertyPath}");
                }
            }
        }

        h980220_RoomController[] rooms = UnityEngine.Object.FindObjectsByType<h980220_RoomController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(room => room.name).ToArray();
        int[] expectedEnemyCounts = { 3, 4, 3 };
        for (int i = 0; i < rooms.Length; i++)
        {
            var serializedRoom = new SerializedObject(rooms[i]);
            SerializedProperty enemies = serializedRoom.FindProperty("roomEnemies");
            SerializedProperty roomIndex = serializedRoom.FindProperty("roomIndex");
            Assert.That(enemies, Is.Not.Null, "Room enemy wiring must be serialized.");
            Assert.That(enemies.arraySize, Is.EqualTo(expectedEnemyCounts[i]));
            Assert.That(roomIndex, Is.Not.Null);
            Assert.That(roomIndex.intValue, Is.EqualTo(i));
        }
    }

    [Test]
    public void ProjectilesMaterialsCameraAndBuildSettingsAreRegistered()
    {
        foreach (string prefabName in new[] { "VirusProjectile", "CureProjectile" })
        {
            string path = $"Assets/HOPAK VIRUS/Prefabs/{prefabName}.prefab";
            h980220_Projectile projectile = AssetDatabase.LoadAssetAtPath<h980220_Projectile>(path);
            Assert.That(projectile, Is.Not.Null, path);
            Assert.That(projectile.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("Sphere"));
            Assert.That(projectile.GetComponent<SphereCollider>().isTrigger, Is.True);
            Assert.That(projectile.GetComponent<Rigidbody>().useGravity, Is.False);
            Assert.That(projectile.GetComponent<Rigidbody>().isKinematic, Is.True);
        }

        string[] materials = { "Purple", "Cyan", "White", "GrayFloor", "GrayWall", "Skin" };
        foreach (string material in materials)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(
                $"Assets/HOPAK VIRUS/Materials/{material}.mat"), Is.Not.Null);
        }

        Camera camera = Find("Main Camera").GetComponent<Camera>();
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.GetComponent<h980220_FollowCamera>(), Is.Not.Null);
        Assert.That(EditorBuildSettings.scenes, Has.Length.EqualTo(1));
        Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(ScenePath));
        Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
        string buildSettings = File.ReadAllText(Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "ProjectSettings", "EditorBuildSettings.asset")));
        Assert.That(buildSettings, Does.Not.Contain("com.unity.input.settings.actions"));
    }

    [Test]
    public void SceneUsesMinimalUiAndNoProhibitedComponentsOrIndicators()
    {
        Assert.That(UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        Assert.That(Find("Canvas/TitlePanel"), Is.Not.Null);
        Assert.That(Find("Canvas/HudPanel"), Is.Not.Null);
        Assert.That(Find("Canvas/ResultPanel"), Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType<Image>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(3));
        Assert.That(UnityEngine.Object.FindObjectsByType<Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(3));
        Assert.That(UnityEngine.Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        Assert.That(UnityEngine.Object.FindObjectsByType<Slider>(
            FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

        string[] prohibitedTypes =
        {
            "Audio" + "Source", "Particle" + "System",
            "Trail" + "Renderer", "Line" + "Renderer"
        };
        foreach (Component component in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Component>(true)))
        {
            Assert.That(prohibitedTypes, Does.Not.Contain(component.GetType().Name),
                component.GetType().Name);
        }

        string[] prohibitedNames =
            { "ring", "range indicator", "aim indicator", "foot", "feet" };
        foreach (GameObject gameObject in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                     .Select(transform => transform.gameObject))
        {
            Assert.That(prohibitedNames.Any(term =>
                gameObject.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0), Is.False,
                gameObject.name);
        }
    }

    private static bool IsRuntimeComponent(MonoBehaviour behaviour)
    {
        return behaviour != null &&
               behaviour.GetType().Assembly.GetName().Name == "h980220_HopakVirus.Runtime";
    }

    private static GameObject Find(string path)
    {
        GameObject found = FindOptional(path);
        Assert.That(found, Is.Not.Null, path);
        return found;
    }

    private static GameObject FindOptional(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(candidate => candidate.name == parts[0]);
        if (root == null)
            return null;

        Transform current = root.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null)
                return null;
        }

        return current.gameObject;
    }
}
