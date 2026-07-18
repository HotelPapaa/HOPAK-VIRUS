using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class h980220_SceneRobustnessTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = created.Count - 1; i >= 0; i--)
        {
            if (created[i] != null)
                Object.Destroy(created[i]);
        }

        created.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator ManagerFirstAwakeStillLocksEnemiesReconstructedByRoomAwake()
    {
        GameObject enemyObject = Track(new GameObject("Late Room Enemy"));
        h980220_EnemyController enemy = enemyObject.AddComponent<h980220_EnemyController>();
        enemy.Configure(h980220_EnemyType.Basic, 1);

        GameObject roomObject = Track(new GameObject("Inactive Serialized Room"));
        roomObject.SetActive(false);
        h980220_RoomController room = roomObject.AddComponent<h980220_RoomController>();
        SetPrivate(room, "roomIndex", 0);
        SetPrivate(room, "roomEnemies", new[] { enemy });

        GameObject managerObject = Track(new GameObject("Manager First"));
        managerObject.SetActive(false);
        h980220_GameManager manager = managerObject.AddComponent<h980220_GameManager>();
        SetPrivate(manager, "rooms", new[] { room });

        managerObject.SetActive(true);
        Assert.That(manager.State, Is.EqualTo(h980220_GameState.Title));
        roomObject.SetActive(true);
        yield return null;

        Assert.That(enemy.IsCombatEnabled, Is.False,
            "Title-state lock must survive manager-first Awake ordering.");
    }

    [UnityTest]
    public IEnumerator GeneratedRoomOneStartsWithClearCameraSightline()
    {
        yield return SceneManager.LoadSceneAsync("HOPAK VIRUS", LoadSceneMode.Single);
        yield return null;
        Physics.SyncTransforms();

        GameObject player = GameObject.Find("Player");
        Camera camera = Camera.main;
        Assert.That(player, Is.Not.Null);
        Assert.That(camera, Is.Not.Null);

        Collider[] obstructions = ObstructionsBetween(
            player.transform.position + Vector3.up * 1.5f,
            camera.transform.position,
            player.transform,
            camera.transform);
        Assert.That(obstructions, Is.Empty,
            "The saved gameplay camera starts behind blocking Room 1 geometry.");
    }

    [UnityTest]
    public IEnumerator GeneratedRoomOneRearOpeningBlocksPlayerWithoutBlockingCameraSightline()
    {
        yield return SceneManager.LoadSceneAsync("HOPAK VIRUS", LoadSceneMode.Single);
        yield return null;
        Physics.SyncTransforms();

        GameObject player = GameObject.Find("Player");
        Camera camera = Camera.main;
        Assert.That(player, Is.Not.Null);
        Assert.That(camera, Is.Not.Null);
        CharacterController controller = player.GetComponent<CharacterController>();
        Assert.That(controller, Is.Not.Null);

        controller.Move(Vector3.back * 5f);
        Physics.SyncTransforms();

        Assert.That(player.transform.position.z, Is.GreaterThan(-7.4f),
            "The player crossed the Room 1 south-center rear opening.");
        Collider[] obstructions = ObstructionsBetween(
            player.transform.position + Vector3.up * 1.5f,
            camera.transform.position,
            player.transform,
            camera.transform);
        Assert.That(obstructions, Is.Empty,
            "The rear escape barrier must remain below the gameplay camera sightline.");
    }

    [UnityTest]
    public IEnumerator FollowCameraResolvesInterveningWallWithPadding()
    {
        GameObject target = Track(new GameObject("Camera Target"));
        GameObject cameraObject = Track(new GameObject("Obstructed Camera"));
        cameraObject.transform.position = new Vector3(0f, 7f, -9f);
        h980220_FollowCamera followCamera = cameraObject.AddComponent<h980220_FollowCamera>();
        followCamera.SetTarget(target.transform);

        GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        wall.name = "Zigzag-Like Wall";
        wall.transform.position = new Vector3(0f, 4f, -4f);
        wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
        Physics.SyncTransforms();

        yield return null;
        Physics.SyncTransforms();

        Collider[] obstructions = ObstructionsBetween(
            target.transform.position + Vector3.up * 1.5f,
            cameraObject.transform.position,
            target.transform,
            cameraObject.transform);
        Assert.That(obstructions, Is.Empty);
        Assert.That(Vector3.Distance(cameraObject.transform.position,
            target.transform.position + Vector3.up * 1.5f), Is.GreaterThanOrEqualTo(1f));
    }

    private GameObject Track(GameObject gameObject)
    {
        created.Add(gameObject);
        return gameObject;
    }

    private static Collider[] ObstructionsBetween(
        Vector3 pivot, Vector3 cameraPosition, Transform target, Transform camera)
    {
        Vector3 offset = cameraPosition - pivot;
        return Physics.RaycastAll(
                pivot, offset.normalized, offset.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            .Select(hit => hit.collider)
            .Where(collider => collider != null &&
                               !collider.transform.IsChildOf(target) &&
                               !collider.transform.IsChildOf(camera))
            .ToArray();
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
