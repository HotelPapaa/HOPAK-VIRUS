using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class h980220_VirusReceiverTarget : MonoBehaviour, h980220_IVirusHitReceiver
{
    public int HitCount { get; private set; }

    public void ReceiveVirusHit()
    {
        HitCount++;
    }
}

public sealed class h980220_PlayerInfectionTests
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
    public void ThirdAcceptedCureHitRemovesInfectionAndRaisesCuredOnce()
    {
        h980220_PlayerInfection infection = CreateInfection();
        int curedCount = 0;
        infection.Cured += () => curedCount++;

        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 1.1f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 2.2f), Is.True);

        Assert.That(infection.RemainingInfection, Is.Zero);
        Assert.That(curedCount, Is.EqualTo(1));
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 4f), Is.False);
        Assert.That(curedCount, Is.EqualTo(1));
    }

    [Test]
    public void HitDuringInvulnerabilityIsIgnored()
    {
        h980220_PlayerInfection infection = CreateInfection();

        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0.2f), Is.False);

        Assert.That(infection.RemainingInfection, Is.EqualTo(2));
    }

    [Test]
    public void CureAppliesConfiguredPlanarCharacterControllerKnockback()
    {
        GameObject player = CreateRoot("Task4 Knockback Player");
        player.AddComponent<CharacterController>();
        h980220_PlayerInfection infection = player.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();

        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0f), Is.True);

        AssertVector(player.transform.position, Vector3.forward * 1.5f);
    }

    [Test]
    public void ResetRestoresFullInfectionAndClearsInvulnerability()
    {
        GameObject player = CreateRoot("Task4 Reset Player");
        h980220_PlayerInfection infection = player.AddComponent<h980220_PlayerInfection>();
        Renderer body = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
        body.transform.SetParent(player.transform);
        Image[] indicators = new Image[3];
        for (int i = 0; i < indicators.Length; i++)
        {
            GameObject indicator = new GameObject($"Reset Indicator {i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            indicator.transform.SetParent(player.transform);
            indicators[i] = indicator.GetComponent<Image>();
        }

        Color infected = new Color(0.6f, 0.1f, 0.8f, 1f);
        SetPrivate(infection, "bodyRenderers", new[] { body });
        SetPrivate(infection, "hudIndicators", indicators);
        SetPrivate(infection, "infectedColor", infected);
        infection.ResetInfection();
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0f), Is.True);
        Assert.That(infection.RemainingInfection, Is.EqualTo(2));

        infection.ResetInfection();

        Assert.That(infection.RemainingInfection, Is.EqualTo(3));
        AssertRendererColor(body, infected);
        Assert.That(indicators[0].gameObject.activeSelf, Is.True);
        Assert.That(indicators[1].gameObject.activeSelf, Is.True);
        Assert.That(indicators[2].gameObject.activeSelf, Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0.2f), Is.True);
        Assert.That(infection.RemainingInfection, Is.EqualTo(2));
    }

    [Test]
    public void ConfiguredBodyAndHudReflectRemainingInfection()
    {
        GameObject player = CreateRoot("Task4 Visual Player");
        h980220_PlayerInfection infection = player.AddComponent<h980220_PlayerInfection>();
        Renderer body = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
        body.transform.SetParent(player.transform);
        Image[] indicators = new Image[3];
        for (int i = 0; i < indicators.Length; i++)
        {
            GameObject indicator = new GameObject($"Indicator {i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            indicator.transform.SetParent(player.transform);
            indicators[i] = indicator.GetComponent<Image>();
        }

        Color normal = Color.white;
        Color infected = new Color(0.6f, 0.1f, 0.8f, 1f);
        SetPrivate(infection, "bodyRenderers", new[] { body });
        SetPrivate(infection, "hudIndicators", indicators);
        SetPrivate(infection, "normalColor", normal);
        SetPrivate(infection, "infectedColor", infected);
        infection.ResetInfection();

        AssertRendererColor(body, infected);
        Assert.That(indicators[0].gameObject.activeSelf, Is.True);
        Assert.That(indicators[1].gameObject.activeSelf, Is.True);
        Assert.That(indicators[2].gameObject.activeSelf, Is.True);

        infection.ReceiveCureAtTime(Vector3.back, 0f);

        AssertRendererColor(body, Color.Lerp(normal, infected, 2f / 3f));
        Assert.That(indicators[0].gameObject.activeSelf, Is.True);
        Assert.That(indicators[1].gameObject.activeSelf, Is.True);
        Assert.That(indicators[2].gameObject.activeSelf, Is.False);

        infection.ReceiveCureAtTime(Vector3.back, 1.1f);
        infection.ReceiveCureAtTime(Vector3.back, 2.2f);

        AssertRendererColor(body, normal);
        Assert.That(indicators[0].gameObject.activeSelf, Is.False);
        Assert.That(indicators[1].gameObject.activeSelf, Is.False);
        Assert.That(indicators[2].gameObject.activeSelf, Is.False);
    }

    [Test]
    public void ProjectileInitializesMovesAndExpiresAtMaximumRange()
    {
        h980220_Projectile projectile = CreateProjectile("Task4 Moving Projectile");

        projectile.Initialize(h980220_ProjectileKind.Virus, Vector3.right * 2f, 2f, 3f);

        Assert.That(projectile.Kind, Is.EqualTo(h980220_ProjectileKind.Virus));
        Assert.That(projectile.Direction, Is.EqualTo(Vector3.right));
        Assert.That(projectile.Speed, Is.EqualTo(2f));
        Assert.That(projectile.MaximumRange, Is.EqualTo(3f));

        projectile.Tick(0.5f);
        AssertVector(projectile.transform.position, Vector3.right);
        Assert.That(projectile.IsExpired, Is.False);

        projectile.Tick(1f);
        AssertVector(projectile.transform.position, Vector3.right * 3f);
        Assert.That(projectile.IsExpired, Is.True);
        Assert.That(projectile.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void ProjectileWithNoMotionExpiresDuringInitialization()
    {
        h980220_Projectile projectile = CreateProjectile("Task4 Stationary Projectile");

        projectile.Initialize(h980220_ProjectileKind.Virus, Vector3.zero, 0f, 4f);

        Assert.That(projectile.IsExpired, Is.True);
        Assert.That(projectile.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void ProjectileConfiguresProductionPhysicsTriggerComponents()
    {
        h980220_Projectile projectile = CreateProjectile("Task4 Physics Projectile");
        projectile.Awake();
        SphereCollider projectileCollider = projectile.GetComponent<SphereCollider>();
        Rigidbody projectileBody = projectile.GetComponent<Rigidbody>();

        Assert.That(projectileCollider.isTrigger, Is.True);
        Assert.That(projectileBody.useGravity, Is.False);
        Assert.That(projectileBody.isKinematic, Is.True);
        Assert.That(projectileBody.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.ContinuousSpeculative));
    }

    [Test]
    public void VirusProjectileDispatchesOnlyToVirusReceiver()
    {
        h980220_Projectile projectile = CreateProjectile("Task4 Virus Projectile");
        projectile.Initialize(h980220_ProjectileKind.Virus, Vector3.forward, 10f, 4f);
        GameObject target = CreateRoot("Task4 Virus Target");
        BoxCollider collider = target.AddComponent<BoxCollider>();
        h980220_VirusReceiverTarget receiver = target.AddComponent<h980220_VirusReceiverTarget>();
        h980220_PlayerInfection infection = target.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();

        projectile.HandleCollision(collider);

        Assert.That(receiver.HitCount, Is.EqualTo(1));
        Assert.That(infection.RemainingInfection, Is.EqualTo(3));
        Assert.That(projectile.IsExpired, Is.True);
    }

    [Test]
    public void CureProjectileDispatchesOnlyToPlayerInfection()
    {
        h980220_Projectile projectile = CreateProjectile("Task4 Cure Projectile");
        projectile.Initialize(h980220_ProjectileKind.Cure, Vector3.forward, 10f, 4f);
        GameObject target = CreateRoot("Task4 Cure Target");
        BoxCollider collider = target.AddComponent<BoxCollider>();
        h980220_VirusReceiverTarget receiver = target.AddComponent<h980220_VirusReceiverTarget>();
        h980220_PlayerInfection infection = target.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();

        projectile.HandleCollision(collider);

        Assert.That(infection.RemainingInfection, Is.EqualTo(2));
        Assert.That(receiver.HitCount, Is.Zero);
        Assert.That(projectile.IsExpired, Is.True);
    }

    [Test]
    public void ProjectileExpiresWhenItHitsWallWithoutReceiver()
    {
        h980220_Projectile projectile = CreateProjectile("Task4 Wall Projectile");
        projectile.Initialize(h980220_ProjectileKind.Virus, Vector3.forward, 10f, 4f);
        GameObject wall = CreateRoot("Task4 Wall");
        BoxCollider collider = wall.AddComponent<BoxCollider>();

        projectile.HandleCollision(collider);

        Assert.That(projectile.IsExpired, Is.True);
        Assert.That(projectile.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void PlayerCombatProcessesFireInputOnlyWhileEnabled()
    {
        h980220_Projectile prefab = CreateProjectile("Task4 Projectile Prefab");
        h980220_PlayerCombat combat = CreateCombat(prefab);
        combat.SetInputEnabled(false);

        Assert.That(combat.ProcessInputAtTime(true, 0f), Is.False);
        Assert.That(SpawnedProjectiles(prefab), Is.Empty);

        combat.SetInputEnabled(true);
        Assert.That(combat.ProcessInputAtTime(true, 0f), Is.True);
        Assert.That(SpawnedProjectiles(prefab), Has.Count.EqualTo(1));
    }

    [Test]
    public void PlayerCombatRespectsCooldownAndInitializesVirusProjectile()
    {
        h980220_Projectile prefab = CreateProjectile("Task4 Cooldown Prefab");
        h980220_PlayerCombat combat = CreateCombat(prefab);

        Assert.That(combat.ProcessInputAtTime(true, 0f), Is.True);
        Assert.That(combat.ProcessInputAtTime(true, 0.1f), Is.False);
        Assert.That(combat.ProcessInputAtTime(true, 0.5f), Is.True);

        List<h980220_Projectile> spawned = SpawnedProjectiles(prefab);
        Assert.That(spawned, Has.Count.EqualTo(2));
        Assert.That(spawned[0].Kind, Is.EqualTo(h980220_ProjectileKind.Virus));
        Assert.That(spawned[0].Speed, Is.EqualTo(10f));
        Assert.That(spawned[0].MaximumRange, Is.EqualTo(4f));
        Assert.That(spawned[0].Direction, Is.EqualTo(Vector3.forward));
    }

    [Test]
    public void PlayerCombatFiresWithoutRhythmComponent()
    {
        h980220_Projectile prefab = CreateProjectile("Task4 Independent Prefab");
        h980220_PlayerCombat combat = CreateCombat(prefab);

        Assert.That(combat.GetComponent<h980220_PlayerRhythmController>(), Is.Null);
        Assert.That(combat.Fire(), Is.True);
        Assert.That(SpawnedProjectiles(prefab), Has.Count.EqualTo(1));
    }

    [Test]
    public void CombatAndProjectileDoNotReferenceForbiddenEffectsOrRhythm()
    {
        string runtime = Path.Combine(Application.dataPath, "HOPAK VIRUS", "Runtime");
        string projectileSource = File.ReadAllText(Path.Combine(runtime, "h980220_Projectile.cs"));
        string combatSource = File.ReadAllText(Path.Combine(runtime, "h980220_PlayerCombat.cs"));
        string combined = projectileSource + combatSource;

        Assert.That(combatSource, Does.Not.Contain("Rhythm"));
        Assert.That(combined, Does.Not.Contain("AudioSource"));
        Assert.That(combined, Does.Not.Contain("ParticleSystem"));
        Assert.That(combined, Does.Not.Contain("TrailRenderer"));
        Assert.That(combined, Does.Not.Contain("LineRenderer"));
        Assert.That(combined.ToLowerInvariant(), Does.Not.Contain("rangeindicator"));
    }

    private h980220_PlayerInfection CreateInfection()
    {
        GameObject player = CreateRoot("Task4 Player");
        h980220_PlayerInfection infection = player.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();
        return infection;
    }

    private h980220_Projectile CreateProjectile(string name)
    {
        GameObject gameObject = CreateRoot(name);
        gameObject.AddComponent<SphereCollider>().isTrigger = true;
        return gameObject.AddComponent<h980220_Projectile>();
    }

    private h980220_PlayerCombat CreateCombat(h980220_Projectile prefab)
    {
        GameObject player = CreateRoot("Task4 Combat Player");
        h980220_PlayerCombat combat = player.AddComponent<h980220_PlayerCombat>();
        GameObject firePoint = new GameObject("Fire Point");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = Vector3.zero;
        firePoint.transform.localRotation = Quaternion.identity;
        SetPrivate(combat, "projectilePrefab", prefab);
        SetPrivate(combat, "firePoint", firePoint.transform);
        SetPrivate(combat, "fireCooldown", 0.5f);
        return combat;
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
