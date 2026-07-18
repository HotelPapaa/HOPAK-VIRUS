using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;

public sealed class h980220_GameFlowTests
{
    private h980220_GameFlowFixture fixture;

    [TearDown]
    public void TearDown()
    {
        fixture?.Dispose();
    }

    [Test]
    public void FollowCameraUsesTargetLocalOffsetAndSmoothsPositionAndRotation()
    {
        var target = new GameObject("Target");
        var cameraObject = new GameObject("Camera");
        try
        {
            target.transform.SetPositionAndRotation(
                new Vector3(10f, 2f, -3f), Quaternion.Euler(0f, 90f, 0f));
            cameraObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            h980220_FollowCamera camera = cameraObject.AddComponent<h980220_FollowCamera>();
            camera.SetTarget(target.transform);

            Vector3 desiredPosition = target.transform.TransformPoint(new Vector3(0f, 7f, -9f));
            Vector3 expectedPosition = Vector3.Lerp(Vector3.zero, desiredPosition, 0.8f);
            camera.Follow(0.1f);

            Assert.That(cameraObject.transform.position, Is.EqualTo(expectedPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Quaternion expectedRotation = Quaternion.LookRotation(
                target.transform.position + Vector3.up * 1.5f - expectedPosition);
            Assert.That(Quaternion.Angle(cameraObject.transform.rotation, expectedRotation), Is.LessThan(0.01f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void VictoryViewUsesHigherWiderOffset()
    {
        var target = new GameObject("Target");
        var cameraObject = new GameObject("Camera");
        try
        {
            target.transform.SetPositionAndRotation(
                new Vector3(4f, 1f, 6f), Quaternion.Euler(0f, 35f, 0f));
            h980220_FollowCamera camera = cameraObject.AddComponent<h980220_FollowCamera>();
            camera.SetTarget(target.transform);
            camera.SetVictoryView();

            camera.Follow(1f);

            Assert.That(cameraObject.transform.position,
                Is.EqualTo(target.transform.TransformPoint(new Vector3(0f, 10f, -14f)))
                    .Using(Vector3ComparerWithEqualsOperator.Instance));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void AwakeStartsAtTitleAndDisablesPlayerAndEveryRoom()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Title));
        Assert.That(fixture.TitlePanel.activeSelf, Is.True);
        Assert.That(fixture.HudPanel.activeSelf, Is.False);
        Assert.That(fixture.ResultPanel.activeSelf, Is.False);
        Assert.That(fixture.Enemies, Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsCombatEnabled));

        Quaternion rotation = fixture.Player.transform.rotation;
        fixture.Rhythm.ProcessFrame(0.5f, false, false, 1f);
        Assert.That(fixture.Player.transform.rotation, Is.EqualTo(rotation).Using(QuaternionEqualityComparer.Instance));
        Assert.That(fixture.Combat.ProcessInputAtTime(true, 0f), Is.False);
    }

    [Test]
    public void StartGameShowsHudEnablesPlayerAndOnlyFirstRoom()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();

        fixture.Manager.StartGame();

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Playing));
        Assert.That(fixture.TitlePanel.activeSelf, Is.False);
        Assert.That(fixture.HudPanel.activeSelf, Is.True);
        Assert.That(fixture.ResultPanel.activeSelf, Is.False);
        Assert.That(fixture.RoomText.text, Is.EqualTo("ROOM 1/3"));
        Assert.That(fixture.Enemies[0].IsCombatEnabled, Is.True);
        Assert.That(fixture.Enemies[1].IsCombatEnabled, Is.False);
        Assert.That(fixture.Enemies[2].IsCombatEnabled, Is.False);

        fixture.Rhythm.ProcessFrame(0.5f, false, false, 1f);
        Assert.That(Quaternion.Angle(fixture.Player.transform.rotation, Quaternion.identity), Is.GreaterThan(1f));
        Assert.That(fixture.Combat.ProcessInputAtTime(true, 0f), Is.True);
    }

    [Test]
    public void CurrentRoomCompletionAdvancesOnceAndFinalRoomWinsWithLocks()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();

        fixture.Enemies[0].ReceiveVirusHit();
        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Playing));
        Assert.That(fixture.RoomText.text, Is.EqualTo("ROOM 2/3"));
        Assert.That(fixture.Enemies[1].IsCombatEnabled, Is.True);
        Assert.That(fixture.Enemies[2].IsCombatEnabled, Is.False);

        fixture.Enemies[1].ReceiveVirusHit();
        Assert.That(fixture.RoomText.text, Is.EqualTo("ROOM 3/3"));
        Assert.That(fixture.Enemies[2].IsCombatEnabled, Is.True);

        fixture.Enemies[2].ReceiveVirusHit();

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Won));
        Assert.That(fixture.HudPanel.activeSelf, Is.False);
        Assert.That(fixture.ResultPanel.activeSelf, Is.True);
        Assert.That(fixture.ResultText.text, Does.Contain("HOPAK VIRUS SPREAD COMPLETE"));
        Assert.That(fixture.ResultText.text, Does.Not.Contain("R: RESTART"));
        fixture.Camera.Follow(1f);
        Assert.That(fixture.Camera.transform.position,
            Is.EqualTo(fixture.Player.transform.TransformPoint(new Vector3(0f, 10f, -14f)))
                .Using(Vector3ComparerWithEqualsOperator.Instance));

        Quaternion lockedRotation = fixture.Player.transform.rotation;
        fixture.Rhythm.ProcessFrame(0.5f, false, false, 1f);
        Assert.That(fixture.Player.transform.rotation,
            Is.EqualTo(lockedRotation).Using(QuaternionEqualityComparer.Instance));
        Assert.That(fixture.Combat.ProcessInputAtTime(true, 10f), Is.False);
    }

    [Test]
    public void OutOfOrderRoomCompletionIsRememberedAndConsumedWhenProgressionReachesIt()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();

        fixture.Enemies[1].ReceiveVirusHit();

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Playing));
        Assert.That(fixture.RoomText.text, Is.EqualTo("ROOM 1/3"));
        Assert.That(fixture.Enemies[2].IsCombatEnabled, Is.False);

        fixture.Enemies[0].ReceiveVirusHit();

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Playing));
        Assert.That(fixture.RoomText.text, Is.EqualTo("ROOM 3/3"));
        Assert.That(fixture.Enemies[2].IsCombatEnabled, Is.True);

        fixture.Enemies[2].ReceiveVirusHit();

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Won));
    }

    [Test]
    public void PlayerCuredEventLosesOnlyOnceAndDisplaysCuredResult()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();

        fixture.Infection.ReceiveCureAtTime(Vector3.left, 0f);
        fixture.Infection.ReceiveCureAtTime(Vector3.left, 1f);
        fixture.Infection.ReceiveCureAtTime(Vector3.left, 2f);

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Cured));
        Assert.That(fixture.ResultText.text, Does.Contain("CURED..."));
        Assert.That(fixture.ResultText.text, Does.Not.Contain("R: RESTART"));
        Assert.That(fixture.ResultPanel.activeSelf, Is.True);
        Assert.That(fixture.HudPanel.activeSelf, Is.False);

        fixture.ResultText.text = "FIRST LOSS ALREADY HANDLED";
        fixture.Manager.Lose();
        Assert.That(fixture.ResultText.text, Is.EqualTo("FIRST LOSS ALREADY HANDLED"));
    }

    [Test]
    public void RestartInputAtResultRequestsActiveSceneReloadThroughSeam()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();
        fixture.Manager.Lose();
        int requestedBuildIndex = int.MinValue;
        int requestCount = 0;
        fixture.Manager.SceneLoader = buildIndex =>
        {
            requestedBuildIndex = buildIndex;
            requestCount++;
        };

        fixture.Manager.ProcessInput(false, true);
        fixture.Manager.ProcessInput(true, false);

        Assert.That(requestCount, Is.EqualTo(1));
        Assert.That(requestedBuildIndex, Is.EqualTo(SceneManager.GetActiveScene().buildIndex));
    }

    [Test]
    public void StartGameAfterWonPreservesFinishedStateAndLocks()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();
        fixture.Enemies[0].ReceiveVirusHit();
        fixture.Enemies[1].ReceiveVirusHit();
        fixture.Enemies[2].ReceiveVirusHit();
        string result = fixture.ResultText.text;
        string room = fixture.RoomText.text;

        fixture.Manager.StartGame();

        AssertFinishedStateAndLocks(h980220_GameState.Won, result, room);
    }

    [Test]
    public void StartGameAfterCuredPreservesFinishedStateAndLocks()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();
        fixture.Manager.Lose();
        string result = fixture.ResultText.text;
        string room = fixture.RoomText.text;

        fixture.Manager.StartGame();

        AssertFinishedStateAndLocks(h980220_GameState.Cured, result, room);
    }

    [Test]
    public void SetCurrentRoomDuringTitleCannotChangeRoomUiOrActivateCombat()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        string room = fixture.RoomText.text;

        fixture.Manager.SetCurrentRoom(1);

        Assert.That(fixture.Manager.State, Is.EqualTo(h980220_GameState.Title));
        Assert.That(fixture.RoomText.text, Is.EqualTo(room));
        Assert.That(fixture.Enemies, Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsCombatEnabled));
    }

    [Test]
    public void SetCurrentRoomAfterWonCannotChangeRoomUiOrUnlockInput()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();
        fixture.Enemies[0].ReceiveVirusHit();
        fixture.Enemies[1].ReceiveVirusHit();
        fixture.Enemies[2].ReceiveVirusHit();
        string result = fixture.ResultText.text;
        string room = fixture.RoomText.text;

        fixture.Manager.SetCurrentRoom(0);

        AssertFinishedStateAndLocks(h980220_GameState.Won, result, room);
    }

    [Test]
    public void SetCurrentRoomAfterCuredCannotChangeRoomUiOrActivateCombat()
    {
        fixture = new h980220_GameFlowFixture();
        fixture.Activate();
        fixture.Manager.StartGame();
        fixture.Manager.Lose();
        string result = fixture.ResultText.text;
        string room = fixture.RoomText.text;

        fixture.Manager.SetCurrentRoom(1);

        AssertFinishedStateAndLocks(h980220_GameState.Cured, result, room);
    }

    private void AssertFinishedStateAndLocks(
        h980220_GameState expectedState, string expectedResult, string expectedRoom)
    {
        Assert.That(fixture.Manager.State, Is.EqualTo(expectedState));
        Assert.That(fixture.ResultText.text, Is.EqualTo(expectedResult));
        Assert.That(fixture.RoomText.text, Is.EqualTo(expectedRoom));
        Assert.That(fixture.TitlePanel.activeSelf, Is.False);
        Assert.That(fixture.HudPanel.activeSelf, Is.False);
        Assert.That(fixture.ResultPanel.activeSelf, Is.True);
        Assert.That(fixture.Enemies, Has.All.Matches<h980220_EnemyController>(enemy => !enemy.IsCombatEnabled));

        Quaternion lockedRotation = fixture.Player.transform.rotation;
        fixture.Rhythm.ProcessFrame(0.5f, false, false, 1f);
        Assert.That(fixture.Player.transform.rotation,
            Is.EqualTo(lockedRotation).Using(QuaternionEqualityComparer.Instance));
        Assert.That(fixture.Combat.ProcessInputAtTime(true, 100f), Is.False);
    }
}

internal sealed class h980220_GameFlowFixture : IDisposable
{
    public readonly GameObject Root;
    public readonly GameObject Player;
    public readonly GameObject TitlePanel;
    public readonly GameObject HudPanel;
    public readonly GameObject ResultPanel;
    public readonly Text ResultText;
    public readonly Text RoomText;
    public readonly h980220_PlayerRhythmController Rhythm;
    public readonly h980220_PlayerCombat Combat;
    public readonly h980220_PlayerInfection Infection;
    public readonly h980220_FollowCamera Camera;
    public readonly h980220_RoomController[] Rooms = new h980220_RoomController[3];
    public readonly h980220_EnemyController[] Enemies = new h980220_EnemyController[3];
    public readonly h980220_GameManager Manager;

    public h980220_GameFlowFixture()
    {
        Root = new GameObject("Game Flow Fixture");
        Root.SetActive(false);

        Player = Child("Player");
        Player.AddComponent<CharacterController>();
        Rhythm = Player.AddComponent<h980220_PlayerRhythmController>();
        Combat = Player.AddComponent<h980220_PlayerCombat>();
        Infection = Player.AddComponent<h980220_PlayerInfection>();

        GameObject projectileObject = Child("Virus Projectile Prefab");
        h980220_Projectile projectile = projectileObject.AddComponent<h980220_Projectile>();
        SetObjectReference(Combat, "projectilePrefab", projectile);
        SetObjectReference(Combat, "firePoint", Player.transform);

        GameObject cameraObject = Child("Camera");
        Camera = cameraObject.AddComponent<h980220_FollowCamera>();
        Camera.SetTarget(Player.transform);

        TitlePanel = Child("Title Panel");
        HudPanel = Child("HUD Panel");
        ResultPanel = Child("Result Panel");
        ResultText = TextChild("Result Text");
        RoomText = TextChild("Room Text");

        for (int i = 0; i < Rooms.Length; i++)
        {
            GameObject roomObject = Child($"Room {i + 1}");
            Rooms[i] = roomObject.AddComponent<h980220_RoomController>();
            GameObject enemyObject = Child($"Enemy {i + 1}");
            enemyObject.AddComponent<CharacterController>();
            Enemies[i] = enemyObject.AddComponent<h980220_EnemyController>();
            Enemies[i].Configure(h980220_EnemyType.Basic, 1);
            Rooms[i].Initialize(i, new[] { Enemies[i] }, null);
        }

        GameObject managerObject = Child("Game Manager");
        Manager = managerObject.AddComponent<h980220_GameManager>();
        SetObjectReference(Manager, "titlePanel", TitlePanel);
        SetObjectReference(Manager, "hudPanel", HudPanel);
        SetObjectReference(Manager, "resultPanel", ResultPanel);
        SetObjectReference(Manager, "resultText", ResultText);
        SetObjectReference(Manager, "roomText", RoomText);
        SetObjectReference(Manager, "playerRhythmController", Rhythm);
        SetObjectReference(Manager, "playerCombat", Combat);
        SetObjectReference(Manager, "playerInfection", Infection);
        SetObjectReference(Manager, "followCamera", Camera);
        SetObjectArray(Manager, "rooms", Rooms);

        TitlePanel.SetActive(false);
        HudPanel.SetActive(true);
        ResultPanel.SetActive(true);
    }

    public void Activate()
    {
        Root.SetActive(true);
        Rhythm.Awake();
        Infection.ResetInfection();
        Manager.Awake();
    }

    public void Dispose()
    {
        foreach (h980220_Projectile projectile in
                 UnityEngine.Object.FindObjectsByType<h980220_Projectile>(FindObjectsSortMode.None))
        {
            if (projectile != null && projectile.transform.root != Root.transform)
                UnityEngine.Object.DestroyImmediate(projectile.gameObject);
        }

        UnityEngine.Object.DestroyImmediate(Root);
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(Root.transform);
        return child;
    }

    private Text TextChild(string name)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        child.transform.SetParent(Root.transform);
        return child.GetComponent<Text>();
    }

    private static void SetObjectReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        var serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(fieldName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray<T>(UnityEngine.Object target, string fieldName, T[] values)
        where T : UnityEngine.Object
    {
        var serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
