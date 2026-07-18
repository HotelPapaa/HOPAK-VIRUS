using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class h980220_GameplaySmokeTests
{
    private const string SceneName = "HOPAK VIRUS";

    [UnitySetUp]
    public IEnumerator LoadGeneratedSceneByBuildName()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, $"{SceneName} must be registered in build settings.");
        yield return load;
        yield return null;
    }

    [UnityTest]
    public IEnumerator UnityAwakeLocksTitleAndEnterEquivalentStartsOnlyRoomOne()
    {
        h980220_GameManager manager = Find<h980220_GameManager>();
        h980220_PlayerRhythmController rhythm = Find<h980220_PlayerRhythmController>();
        h980220_PlayerCombat combat = Find<h980220_PlayerCombat>();
        h980220_PlayerInfection infection = Find<h980220_PlayerInfection>();
        h980220_EnemyController[] enemies = FindAll<h980220_EnemyController>();
        Quaternion titleRotation = rhythm.transform.rotation;

        Assert.That(manager.State, Is.EqualTo(h980220_GameState.Title));
        Assert.That(FindObject("Canvas/TitlePanel").activeSelf, Is.True);
        Assert.That(FindObject("Canvas/HudPanel").activeSelf, Is.False);
        Assert.That(FindObject("Canvas/ResultPanel").activeSelf, Is.False);
        Assert.That(enemies, Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsCombatEnabled));
        ProcessRhythm(rhythm, 0.1f, false, false, 1f);
        Assert.That(Quaternion.Angle(titleRotation, rhythm.transform.rotation), Is.LessThan(0.001f));
        Assert.That(ProcessCombat(combat, true, 0f), Is.False);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back * 10f, 0f), Is.False,
            "Title state must reject cure damage just like movement and combat input.");

        ProcessManagerInput(manager, true, false);

        Assert.That(manager.State, Is.EqualTo(h980220_GameState.Playing));
        Assert.That(FindObject("Canvas/TitlePanel").activeSelf, Is.False);
        Assert.That(FindObject("Canvas/HudPanel").activeSelf, Is.True);
        Assert.That(FindObject("Canvas/ResultPanel").activeSelf, Is.False);
        Assert.That(FindObject("Canvas/HudPanel/RoomText").GetComponent<Text>().text,
            Is.EqualTo("ROOM 1/3"));
        Assert.That(EnemiesIn("Room 1 Plaza"),
            Has.All.Matches<h980220_EnemyController>(enemy => enemy.IsCombatEnabled));
        Assert.That(EnemiesIn("Room 2 Zigzag").Concat(EnemiesIn("Room 3 Arena")),
            Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsCombatEnabled));
        yield return null;
    }

    [UnityTest]
    public IEnumerator SpaceEquivalentFiresWithoutRhythmAndAlternationMovesCharacterController()
    {
        h980220_GameManager manager = Find<h980220_GameManager>();
        h980220_PlayerRhythmController rhythm = Find<h980220_PlayerRhythmController>();
        h980220_PlayerCombat combat = Find<h980220_PlayerCombat>();
        ProcessManagerInput(manager, true, false);

        Assert.That(rhythm.SuccessStreak, Is.Zero);
        Assert.That(ProcessCombat(combat, true, 0f), Is.True,
            "Space-equivalent fire must not depend on rhythm success.");
        h980220_Projectile projectile = FindAll<h980220_Projectile>()
            .Single(instance => instance.gameObject.scene.IsValid());
        Assert.That(projectile.Kind, Is.EqualTo(h980220_ProjectileKind.Virus));
        Assert.That(projectile.MaximumRange, Is.EqualTo(4f).Within(0.001f));
        Assert.That(EnemiesIn("Room 1 Plaza").Min(enemy =>
                Vector3.Distance(projectile.transform.position, enemy.transform.position)),
            Is.GreaterThan(projectile.MaximumRange),
            "Short range must require approaching the first room enemies.");

        CharacterController controller = rhythm.GetComponent<CharacterController>();
        Vector3 start = rhythm.transform.position;
        Assert.That(controller, Is.Not.Null);
        ProcessRhythm(rhythm, 0f, true, false, 0f);
        Assert.That(rhythm.transform.position, Is.EqualTo(start));
        ProcessRhythm(rhythm, 0.32f, false, true, 0f);

        Assert.That(rhythm.SuccessStreak, Is.EqualTo(1));
        Assert.That(Vector3.Distance(start, rhythm.transform.position), Is.GreaterThan(0.1f));
        Assert.That(rhythm.transform.position.z, Is.GreaterThan(start.z));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ThreeCureHitsReachTerminalCuredStateAndLockGameplay()
    {
        h980220_GameManager manager = Find<h980220_GameManager>();
        h980220_PlayerInfection infection = Find<h980220_PlayerInfection>();
        h980220_PlayerRhythmController rhythm = Find<h980220_PlayerRhythmController>();
        h980220_PlayerCombat combat = Find<h980220_PlayerCombat>();
        ProcessManagerInput(manager, true, false);

        Assert.That(infection.ReceiveCureAtTime(Vector3.back * 10f, 0f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back * 10f, 1f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back * 10f, 2f), Is.True);

        Assert.That(infection.RemainingInfection, Is.Zero);
        Assert.That(manager.State, Is.EqualTo(h980220_GameState.Cured));
        Assert.That(FindObject("Canvas/HudPanel").activeSelf, Is.False);
        Assert.That(FindObject("Canvas/ResultPanel").activeSelf, Is.True);
        Assert.That(FindObject("Canvas/ResultPanel/ResultText").GetComponent<Text>().text,
            Is.EqualTo("CURED..."));
        Assert.That(FindAll<h980220_EnemyController>(),
            Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsCombatEnabled));
        Vector3 lockedPosition = rhythm.transform.position;
        ProcessRhythm(rhythm, 0.32f, true, false, 1f);
        Assert.That(rhythm.transform.position, Is.EqualTo(lockedPosition));
        Assert.That(ProcessCombat(combat, true, 100f), Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RestartEquivalentReloadsFreshTitleInfectionEnemiesAndGates()
    {
        h980220_GameManager oldManager = Find<h980220_GameManager>();
        h980220_PlayerInfection infection = Find<h980220_PlayerInfection>();
        ProcessManagerInput(oldManager, true, false);
        infection.ReceiveCureAtTime(Vector3.back * 10f, 0f);
        infection.ReceiveCureAtTime(Vector3.back * 10f, 1f);
        infection.ReceiveCureAtTime(Vector3.back * 10f, 2f);
        Assert.That(oldManager.State, Is.EqualTo(h980220_GameState.Cured));

        ProcessManagerInput(oldManager, false, true);
        yield return null;

        h980220_GameManager newManager = Find<h980220_GameManager>();
        Assert.That(newManager, Is.Not.SameAs(oldManager));
        Assert.That(newManager.State, Is.EqualTo(h980220_GameState.Title));
        Assert.That(Find<h980220_PlayerInfection>().RemainingInfection, Is.EqualTo(3));
        Assert.That(FindAll<h980220_EnemyController>(),
            Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsInfected && !enemy.IsCombatEnabled));
        Assert.That(FindObject("Room 1 Plaza/Gate 1").transform.position.y,
            Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(FindObject("Room 2 Zigzag/Gate 2").transform.position.y,
            Is.EqualTo(1.5f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator RealEnemiesTakeConfiguredHitsOpenGatesReachWonAndRejectTerminalCures()
    {
        h980220_GameManager manager = Find<h980220_GameManager>();
        h980220_PlayerInfection infection = Find<h980220_PlayerInfection>();
        h980220_PlayerCombat combat = Find<h980220_PlayerCombat>();
        h980220_PlayerRhythmController rhythm = Find<h980220_PlayerRhythmController>();
        ProcessManagerInput(manager, true, false);

        InfectRoomThroughConfiguredHits("Room 1 Plaza");
        Assert.That(Find<h980220_RoomController>("Room 1 Plaza").RemainingEnemies, Is.Zero);
        Assert.That(FindObject("Room 1 Plaza/Gate 1").transform.position.y,
            Is.EqualTo(5.5f).Within(0.001f));
        Assert.That(FindObject("Canvas/HudPanel/RoomText").GetComponent<Text>().text,
            Is.EqualTo("ROOM 2/3"));

        InfectRoomThroughConfiguredHits("Room 2 Zigzag");
        Assert.That(Find<h980220_RoomController>("Room 2 Zigzag").RemainingEnemies, Is.Zero);
        Assert.That(FindObject("Room 2 Zigzag/Gate 2").transform.position.y,
            Is.EqualTo(5.5f).Within(0.001f));
        Assert.That(FindObject("Canvas/HudPanel/RoomText").GetComponent<Text>().text,
            Is.EqualTo("ROOM 3/3"));

        InfectRoomThroughConfiguredHits("Room 3 Arena");
        Assert.That(Find<h980220_RoomController>("Room 3 Arena").RemainingEnemies, Is.Zero);
        Assert.That(manager.State, Is.EqualTo(h980220_GameState.Won));
        Assert.That(FindObject("Canvas/ResultPanel/ResultText").GetComponent<Text>().text,
            Is.EqualTo("HOPAK VIRUS SPREAD COMPLETE"));
        Assert.That(FindAll<h980220_EnemyController>(),
            Has.All.Matches<h980220_EnemyController>(enemy => enemy.IsInfected && !enemy.IsCombatEnabled));
        Assert.That(ProcessCombat(combat, true, 100f), Is.False);
        Vector3 lockedPosition = rhythm.transform.position;
        ProcessRhythm(rhythm, 0.32f, true, false, 1f);
        Assert.That(rhythm.transform.position, Is.EqualTo(lockedPosition));

        Assert.That(infection.ReceiveCureAtTime(Vector3.back * 10f, 100f), Is.False,
            "Won is terminal: cure projectiles already in flight must not mutate player state.");
        Assert.That(infection.RemainingInfection, Is.EqualTo(3));
        yield return null;
    }

    [UnityTest]
    public IEnumerator GeneratedSceneHasCompleteDependenciesAndNoForbiddenComponents()
    {
        GameObject player = FindObject("Player");
        MeshRenderer[] visuals = player.GetComponentsInChildren<MeshRenderer>(true);
        Assert.That(visuals, Has.Length.EqualTo(6));
        Assert.That(visuals.All(renderer =>
            renderer.GetComponent<MeshFilter>()?.sharedMesh?.name == "Cube"), Is.True);
        Assert.That(visuals.All(renderer => renderer.GetComponent<Collider>() == null), Is.True);

        string[] prohibitedTypes =
        {
            "Audio" + "Source", "Particle" + "System", "Trail" + "Renderer",
            "Line" + "Renderer", "Visual" + "Effect"
        };
        foreach (Component component in SceneManager.GetActiveScene().GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Component>(true)))
        {
            Assert.That(prohibitedTypes, Does.Not.Contain(component.GetType().Name),
                component.GetType().Name);
        }

        foreach (MonoBehaviour behaviour in FindAll<MonoBehaviour>()
                     .Where(candidate => candidate.GetType().Assembly.GetName().Name ==
                                         "h980220_HopakVirus.Runtime"))
        {
            foreach (FieldInfo field in behaviour.GetType().GetFields(
                         BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttribute<SerializeField>() == null)
                    continue;

                bool optionalFinalExit = behaviour is h980220_RoomController &&
                                         behaviour.name == "Room 3 Arena" &&
                                         field.Name == "exitDoor";
                object value = field.GetValue(behaviour);
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    if (!optionalFinalExit)
                        Assert.That(value as UnityEngine.Object, Is.Not.Null,
                            $"{behaviour.name}/{behaviour.GetType().Name}.{field.Name}");
                }
                else if (field.FieldType.IsArray &&
                         typeof(UnityEngine.Object).IsAssignableFrom(
                             field.FieldType.GetElementType()))
                {
                    var values = value as Array;
                    Assert.That(values, Is.Not.Null.And.Length.GreaterThan(0),
                        $"{behaviour.name}/{behaviour.GetType().Name}.{field.Name}");
                    foreach (object element in values)
                    {
                        Assert.That(element as UnityEngine.Object, Is.Not.Null,
                            $"{behaviour.name}/{behaviour.GetType().Name}.{field.Name}");
                    }
                }
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator FiveCadenceProfilesEstimateNoviceAdvanceAndSubFiveMinuteCompletion()
    {
        h980220_PlayerRhythmController controller = Find<h980220_PlayerRhythmController>();
        float stepDuration = PrivateField<float>(controller, "stepDuration");
        float successWindow = PrivateField<float>(controller, "successWindow");
        float baseSpeed = PrivateField<float>(controller, "baseMoveSpeed");
        float maxSpeed = PrivateField<float>(controller, "maxMoveSpeed");
        int successesToMax = PrivateField<int>(controller, "successesToMaxSpeed");
        h980220_CadenceProfile[] profiles =
        {
            new h980220_CadenceProfile("expert", 3f, 5f, 5f,
                new[] { -0.05f, -0.02f, -0.10f, -0.04f }),
            new h980220_CadenceProfile("steady", 6f, 8f, 10f,
                new[] { -0.12f, -0.05f, -0.18f, -0.02f, 0.03f }),
            new h980220_CadenceProfile("novice", 10f, 11f, 14f,
                new[] { -0.18f, -0.10f, 0.04f, -0.16f, -0.06f }),
            new h980220_CadenceProfile("jittery", 14f, 12f, 16f,
                new[] { -0.20f, 0.06f, -0.12f, -0.04f, 0.02f }),
            new h980220_CadenceProfile("cautious", 18f, 13f, 18f,
                new[] { -0.16f, -0.08f, -0.04f, 0.03f, -0.12f, 0.05f })
        };

        var estimates = new List<h980220_TimingEstimate>();
        foreach (h980220_CadenceProfile profile in profiles)
        {
            h980220_TimingEstimate estimate = EstimateCompletion(
                profile, stepDuration, successWindow, baseSpeed, maxSpeed, successesToMax);
            estimates.Add(estimate);
            TestContext.WriteLine(
                $"AUTOMATED {profile.Name}: first advance {estimate.FirstAdvanceSeconds:F2}s, " +
                $"completion {estimate.CompletionSeconds:F2}s, failures {estimate.RhythmFailures}, " +
                $"peak {estimate.PeakSpeed:F2}u/s");
            Assert.That(estimate.RhythmFailures, Is.GreaterThan(0), profile.Name);
            Assert.That(estimate.PeakSpeed, Is.GreaterThan(baseSpeed), profile.Name);
            Assert.That(estimate.CompletionSeconds, Is.LessThan(300f), profile.Name);
        }

        h980220_TimingEstimate novice = estimates.Single(estimate => estimate.Name == "novice");
        Assert.That(novice.FirstAdvanceSeconds, Is.LessThan(30f));
        float median = estimates.Select(estimate => estimate.CompletionSeconds)
            .OrderBy(seconds => seconds).ElementAt(estimates.Count / 2);
        Assert.That(median, Is.InRange(180f, 240f),
            "Automated target median should remain near three to four minutes.");
        yield return null;
    }

    private static h980220_TimingEstimate EstimateCompletion(
        h980220_CadenceProfile profile, float stepDuration, float successWindow,
        float baseSpeed, float maxSpeed, int successesToMax)
    {
        var rhythm = new h980220_RhythmState(
            stepDuration, successWindow, baseSpeed, maxSpeed, successesToMax);
        float elapsed = profile.ReactionSeconds;
        float firstAdvance = float.PositiveInfinity;
        float peakSpeed = baseSpeed;
        int failures = 0;
        int cadenceIndex = 0;
        h980220_Leg leg = h980220_Leg.Left;
        float[] pathSegments = { 28f, 38f, 30f };
        int[] roomHits = { 3, 4, 5 };

        for (int room = 0; room < pathSegments.Length; room++)
        {
            rhythm.Reset();
            rhythm.RegisterInput(leg);
            float covered = 0f;
            while (covered < pathSegments[room] && elapsed < 600f)
            {
                float interval = stepDuration +
                                 profile.JitterSeconds[cadenceIndex % profile.JitterSeconds.Length];
                cadenceIndex++;
                float intervalRemaining = interval;
                while (intervalRemaining > 0f && covered < pathSegments[room])
                {
                    float delta = Mathf.Min(0.01f, intervalRemaining);
                    rhythm.Tick(delta);
                    if (rhythm.IsMoving)
                    {
                        covered += rhythm.CurrentSpeed * delta;
                        peakSpeed = Mathf.Max(peakSpeed, rhythm.CurrentSpeed);
                    }
                    elapsed += delta;
                    intervalRemaining -= delta;
                }

                leg = leg == h980220_Leg.Left ? h980220_Leg.Right : h980220_Leg.Left;
                h980220_RhythmInputResult result = rhythm.RegisterInput(leg);
                if (result == h980220_RhythmInputResult.Success &&
                    float.IsPositiveInfinity(firstAdvance))
                {
                    firstAdvance = elapsed;
                }
                else if (result == h980220_RhythmInputResult.Failed)
                {
                    failures++;
                }
            }

            elapsed += roomHits[room] * profile.CombatSecondsPerHit;
            if (room < pathSegments.Length - 1)
                elapsed += profile.RoomTransitionSeconds;
        }

        return new h980220_TimingEstimate(
            profile.Name, firstAdvance, elapsed, failures, peakSpeed);
    }

    private static void InfectRoomThroughConfiguredHits(string roomName)
    {
        foreach (h980220_EnemyController enemy in EnemiesIn(roomName))
        {
            for (int hit = 1; hit < enemy.RequiredHits; hit++)
            {
                enemy.ReceiveVirusHit();
                Assert.That(enemy.IsInfected, Is.False,
                    $"{enemy.name} infected before configured hit {enemy.RequiredHits}.");
            }

            enemy.ReceiveVirusHit();
            Assert.That(enemy.IsInfected, Is.True, enemy.name);
        }
    }

    private static h980220_EnemyController[] EnemiesIn(string roomName)
    {
        return FindObject(roomName).GetComponentsInChildren<h980220_EnemyController>(true);
    }

    private static T Find<T>() where T : UnityEngine.Object
    {
        T found = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        Assert.That(found, Is.Not.Null, typeof(T).Name);
        return found;
    }

    private static T Find<T>(string objectName) where T : Component
    {
        T found = FindObject(objectName).GetComponent<T>();
        Assert.That(found, Is.Not.Null, $"{objectName}/{typeof(T).Name}");
        return found;
    }

    private static T[] FindAll<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private static GameObject FindObject(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects()
            .SingleOrDefault(candidate => candidate.name == parts[0]);
        Assert.That(root, Is.Not.Null, path);
        Transform current = root.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            Assert.That(current, Is.Not.Null, path);
        }

        return current.gameObject;
    }

    private static void ProcessManagerInput(
        h980220_GameManager manager, bool startPressed, bool restartPressed)
    {
        Invoke(manager, "ProcessInput", startPressed, restartPressed);
    }

    private static void ProcessRhythm(
        h980220_PlayerRhythmController rhythm, float deltaTime,
        bool leftDown, bool rightDown, float turnAxis)
    {
        Invoke(rhythm, "ProcessFrame", deltaTime, leftDown, rightDown, turnAxis);
    }

    private static bool ProcessCombat(h980220_PlayerCombat combat, bool firePressed, float now)
    {
        return (bool)Invoke(combat, "ProcessInputAtTime", firePressed, now);
    }

    private static object Invoke(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing production seam {methodName}.");
        return method.Invoke(target, arguments);
    }

    private static T PrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }
}

public sealed class h980220_CadenceProfile
{
    public h980220_CadenceProfile(
        string name, float reactionSeconds, float combatSecondsPerHit,
        float roomTransitionSeconds, float[] jitterSeconds)
    {
        Name = name;
        ReactionSeconds = reactionSeconds;
        CombatSecondsPerHit = combatSecondsPerHit;
        RoomTransitionSeconds = roomTransitionSeconds;
        JitterSeconds = jitterSeconds;
    }

    public string Name { get; }
    public float ReactionSeconds { get; }
    public float CombatSecondsPerHit { get; }
    public float RoomTransitionSeconds { get; }
    public float[] JitterSeconds { get; }
}

public sealed class h980220_TimingEstimate
{
    public h980220_TimingEstimate(
        string name, float firstAdvanceSeconds, float completionSeconds,
        int rhythmFailures, float peakSpeed)
    {
        Name = name;
        FirstAdvanceSeconds = firstAdvanceSeconds;
        CompletionSeconds = completionSeconds;
        RhythmFailures = rhythmFailures;
        PeakSpeed = peakSpeed;
    }

    public string Name { get; }
    public float FirstAdvanceSeconds { get; }
    public float CompletionSeconds { get; }
    public int RhythmFailures { get; }
    public float PeakSpeed { get; }
}
