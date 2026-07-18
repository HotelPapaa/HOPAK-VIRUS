using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class h980220_EnemyAndRoomTests
{
    private readonly List<GameObject> roots = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (h980220_Projectile projectile in
                 Object.FindObjectsByType<h980220_Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (projectile != null && !roots.Contains(projectile.gameObject))
                Object.DestroyImmediate(projectile.gameObject);
        }

        for (int i = roots.Count - 1; i >= 0; i--)
        {
            if (roots[i] != null)
                Object.DestroyImmediate(roots[i]);
        }

        roots.Clear();
    }

    [Test]
    public void BasicInfectsInOneHitAndEliteInThreeWithOneEventEach()
    {
        h980220_EnemyController basic = CreateEnemy("Basic", h980220_EnemyType.Basic, 0);
        h980220_EnemyController elite = CreateEnemy("Elite", h980220_EnemyType.Elite, 3);
        int basicEvents = 0;
        int eliteEvents = 0;
        basic.Infected += _ => basicEvents++;
        elite.Infected += _ => eliteEvents++;

        basic.ReceiveVirusHit();
        basic.ReceiveVirusHit();
        elite.ReceiveVirusHit();
        elite.ReceiveVirusHit();

        Assert.That(basic.IsInfected, Is.True);
        Assert.That(basic.RequiredHits, Is.EqualTo(1));
        Assert.That(basicEvents, Is.EqualTo(1));
        Assert.That(elite.IsInfected, Is.False);
        Assert.That(eliteEvents, Is.Zero);

        elite.ReceiveVirusHit();
        elite.ReceiveVirusHit();

        Assert.That(elite.IsInfected, Is.True);
        Assert.That(eliteEvents, Is.EqualTo(1));
    }

    [Test]
    public void VirusHitsProgressRendererFromTealToPurple()
    {
        h980220_EnemyController enemy = CreateEnemy("Visual Elite", h980220_EnemyType.Elite, 3);
        Renderer renderer = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
        roots.Add(renderer.gameObject);
        renderer.transform.SetParent(enemy.transform);
        Color teal = new Color(0f, 0.8f, 0.7f, 1f);
        Color purple = new Color(0.65f, 0.1f, 0.85f, 1f);
        SetPrivate(enemy, "bodyRenderers", new[] { renderer });
        SetPrivate(enemy, "healthyColor", teal);
        SetPrivate(enemy, "infectedColor", purple);
        enemy.Configure(h980220_EnemyType.Elite, 3);

        AssertRendererColor(renderer, teal);
        enemy.ReceiveVirusHit();
        AssertRendererColor(renderer, Color.Lerp(teal, purple, 1f / 3f));
        enemy.ReceiveVirusHit();
        AssertRendererColor(renderer, Color.Lerp(teal, purple, 2f / 3f));
        enemy.ReceiveVirusHit();
        AssertRendererColor(renderer, purple);
    }

    [Test]
    public void CombatDisabledStopsBasicMovementAndContactCure()
    {
        h980220_PlayerInfection infection = CreatePlayer(new Vector3(0.75f, 0f, 0f));
        h980220_EnemyController enemy = CreateEnemy("Gated Basic", h980220_EnemyType.Basic, 1);
        SetPrivate(enemy, "player", infection.transform);
        SetPrivate(enemy, "movementSpeed", 1f);
        SetPrivate(enemy, "contactRange", 2f);
        Vector3 start = enemy.transform.position;

        enemy.SetCombatEnabled(false);
        enemy.Tick(0f, 0.5f);

        AssertVector(enemy.transform.position, start);
        Assert.That(infection.RemainingInfection, Is.EqualTo(3));

        enemy.SetCombatEnabled(true);
        enemy.Tick(0.1f, 0.25f);

        Assert.That(enemy.transform.position.x, Is.GreaterThan(start.x));
        Assert.That(infection.RemainingInfection, Is.EqualTo(2));
    }

    [Test]
    public void BasicUsesCharacterControllerAndRequiresClearContactLine()
    {
        h980220_PlayerInfection infection = CreatePlayer(new Vector3(3f, 0f, 0f));
        infection.gameObject.AddComponent<BoxCollider>();
        h980220_EnemyController enemy = CreateEnemy("Blocked Basic", h980220_EnemyType.Basic, 1);
        SetPrivate(enemy, "player", infection.transform);
        SetPrivate(enemy, "movementSpeed", 4f);
        SetPrivate(enemy, "contactRange", 4f);
        GameObject wall = CreateRoot("Wall");
        wall.transform.position = new Vector3(1.5f, 0f, 0f);
        BoxCollider wallCollider = wall.AddComponent<BoxCollider>();
        wallCollider.size = new Vector3(0.2f, 3f, 3f);
        Physics.SyncTransforms();

        enemy.Tick(0f, 1f);

        Assert.That(enemy.GetComponent<CharacterController>(), Is.Not.Null);
        Assert.That(enemy.transform.position.x, Is.LessThan(1.4f));
        Assert.That(infection.RemainingInfection, Is.EqualTo(3));

        Object.DestroyImmediate(wallCollider);
        Physics.SyncTransforms();
        enemy.Tick(1.1f, 0.1f);

        Assert.That(infection.RemainingInfection, Is.EqualTo(2));
    }

    [TestCase(h980220_EnemyType.Ranged, false)]
    [TestCase(h980220_EnemyType.Elite, true)]
    public void RangedAttackersFacePlayerAndFireCureOnCadence(h980220_EnemyType type, bool moves)
    {
        h980220_PlayerInfection infection = CreatePlayer(new Vector3(4f, 0f, 0f));
        h980220_Projectile prefab = CreateCureProjectilePrefab();
        h980220_EnemyController enemy = CreateEnemy($"{type} Shooter", type, type == h980220_EnemyType.Elite ? 3 : 1);
        SetPrivate(enemy, "player", infection.transform);
        SetPrivate(enemy, "cureProjectilePrefab", prefab);
        SetPrivate(enemy, "fireInterval", 1f);
        SetPrivate(enemy, "movementSpeed", 1f);
        Vector3 start = enemy.transform.position;

        enemy.Tick(0f, 0.25f);

        Assert.That(Vector3.Dot(enemy.transform.forward, Vector3.right), Is.GreaterThan(0.999f));
        Assert.That(enemy.transform.position.x, moves ? Is.GreaterThan(start.x) : Is.EqualTo(start.x).Within(0.001f));
        List<h980220_Projectile> firstVolley = SpawnedProjectiles(prefab);
        Assert.That(firstVolley, Has.Count.EqualTo(1));
        Assert.That(firstVolley[0].Kind, Is.EqualTo(h980220_ProjectileKind.Cure));
        Assert.That(firstVolley[0].Direction, Is.EqualTo(Vector3.right));
        Assert.That(firstVolley[0].Speed, Is.EqualTo(7f));
        Assert.That(firstVolley[0].MaximumRange, Is.EqualTo(12f));
        AssertRendererColor(firstVolley[0].GetComponent<Renderer>(), Color.white);

        enemy.Tick(0.5f, 0f);
        Assert.That(SpawnedProjectiles(prefab), Has.Count.EqualTo(1));
        enemy.Tick(1f, 0f);
        Assert.That(SpawnedProjectiles(prefab), Has.Count.EqualTo(2));
    }

    [Test]
    public void InfectedEnemyDancesWithoutMovingTowardOrAttackingPlayer()
    {
        h980220_PlayerInfection infection = CreatePlayer(new Vector3(4f, 0f, 0f));
        h980220_Projectile prefab = CreateCureProjectilePrefab();
        h980220_EnemyController enemy = CreateEnemy("Dancing Elite", h980220_EnemyType.Elite, 1);
        SetPrivate(enemy, "player", infection.transform);
        SetPrivate(enemy, "cureProjectilePrefab", prefab);
        Vector3 start = enemy.transform.position;
        enemy.ReceiveVirusHit();

        enemy.Tick(Mathf.PI * 0.5f, 1f);

        Assert.That(enemy.transform.position.x, Is.EqualTo(start.x).Within(0.001f));
        Assert.That(enemy.transform.position.y, Is.GreaterThan(start.y));
        Assert.That(Quaternion.Angle(enemy.transform.rotation, Quaternion.identity), Is.GreaterThan(1f));
        Assert.That(SpawnedProjectiles(prefab), Is.Empty);
        Assert.That(infection.RemainingInfection, Is.EqualTo(3));

        enemy.Tick(Mathf.PI * 1.5f, 1f);
        Assert.That(enemy.transform.position.y, Is.LessThan(start.y));
        Assert.That(SpawnedProjectiles(prefab), Is.Empty);
    }

    [Test]
    public void RoomDeduplicatesEnemiesGatesCombatAndCompletesOnceByRaisingDoor()
    {
        h980220_EnemyController first = CreateEnemy("First", h980220_EnemyType.Basic, 1);
        h980220_EnemyController second = CreateEnemy("Second", h980220_EnemyType.Basic, 1);
        Transform door = CreateRoot("Exit Door").transform;
        door.position = new Vector3(2f, 1f, 3f);
        h980220_RoomController room = CreateRoot("Room").AddComponent<h980220_RoomController>();
        int completionCount = 0;
        int completedIndex = -1;
        room.Completed += index =>
        {
            completionCount++;
            completedIndex = index;
        };

        room.Initialize(7, new[] { first, first, null, second }, door);
        room.SetCombatEnabled(false);

        Assert.That(room.RemainingEnemies, Is.EqualTo(2));
        Assert.That(first.IsCombatEnabled, Is.False);
        Assert.That(second.IsCombatEnabled, Is.False);

        first.ReceiveVirusHit();
        Assert.That(room.RemainingEnemies, Is.EqualTo(1));
        AssertVector(door.position, new Vector3(2f, 1f, 3f));

        second.ReceiveVirusHit();
        second.ReceiveVirusHit();

        Assert.That(room.RemainingEnemies, Is.Zero);
        Assert.That(completionCount, Is.EqualTo(1));
        Assert.That(completedIndex, Is.EqualTo(7));
        AssertVector(door.position, new Vector3(2f, 5f, 3f));
    }

    [Test]
    public void EmptyRoomCompletesImmediatelyOnce()
    {
        Transform door = CreateRoot("Empty Exit").transform;
        h980220_RoomController room = CreateRoot("Empty Room").AddComponent<h980220_RoomController>();
        int completionCount = 0;
        room.Completed += _ => completionCount++;

        room.Initialize(4, null, door);
        room.Initialize(4, null, door);

        Assert.That(room.RemainingEnemies, Is.Zero);
        Assert.That(completionCount, Is.EqualTo(1));
        Assert.That(door.position.y, Is.EqualTo(4f).Within(0.001f));
    }

    private h980220_EnemyController CreateEnemy(string name, h980220_EnemyType type, int requiredHits)
    {
        GameObject gameObject = CreateRoot(name);
        h980220_EnemyController enemy = gameObject.AddComponent<h980220_EnemyController>();
        enemy.Configure(type, requiredHits);
        return enemy;
    }

    private h980220_PlayerInfection CreatePlayer(Vector3 position)
    {
        GameObject gameObject = CreateRoot("Player");
        gameObject.transform.position = position;
        h980220_PlayerInfection infection = gameObject.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();
        return infection;
    }

    private h980220_Projectile CreateCureProjectilePrefab()
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = "Cure Prefab";
        roots.Add(gameObject);
        return gameObject.AddComponent<h980220_Projectile>();
    }

    private GameObject CreateRoot(string name)
    {
        GameObject gameObject = new GameObject(name);
        roots.Add(gameObject);
        return gameObject;
    }

    private static List<h980220_Projectile> SpawnedProjectiles(h980220_Projectile prefab)
    {
        var spawned = new List<h980220_Projectile>();
        foreach (h980220_Projectile projectile in
                 Object.FindObjectsByType<h980220_Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (projectile != prefab)
                spawned.Add(projectile);
        }

        return spawned;
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing serialized field {fieldName}");
        field.SetValue(target, value);
    }

    private static void AssertRendererColor(Renderer renderer, Color expected)
    {
        Assert.That(renderer, Is.Not.Null);
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        Color actual = properties.GetColor(Shader.PropertyToID("_BaseColor"));
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
    }

    private static void AssertVector(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f));
    }
}
