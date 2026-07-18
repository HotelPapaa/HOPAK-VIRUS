using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class h980220_HopakPoseTests
{
    [Test]
    public void LeftLegAtMidStepRaisesOnlyLeftSegments()
    {
        h980220_LegPose pose = h980220_HopakPose.Evaluate(h980220_Leg.Left, 0.5f);

        Assert.That(pose.LeftThighX, Is.LessThan(-50f));
        Assert.That(pose.LeftShinX, Is.GreaterThan(60f));
        Assert.That(pose.RightThighX, Is.EqualTo(0f).Within(0.01f));
        Assert.That(pose.RightShinX, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void NeutralPoseHasStraightLegs()
    {
        h980220_LegPose pose = h980220_HopakPose.Evaluate(h980220_Leg.None, 0f);

        Assert.That(pose.LeftThighX, Is.Zero);
        Assert.That(pose.LeftShinX, Is.Zero);
        Assert.That(pose.RightThighX, Is.Zero);
        Assert.That(pose.RightShinX, Is.Zero);
    }
}

public sealed class h980220_PlayerRhythmControllerTests
{
    private GameObject player;
    private h980220_PlayerRhythmController controller;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("player");
        controller = player.AddComponent<h980220_PlayerRhythmController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
    }

    [Test]
    public void StartsAtConfiguredBaseSpeedWithNoSuccesses()
    {
        Assert.That(controller.CurrentSpeed, Is.EqualTo(2f).Within(0.001f));
        Assert.That(controller.SuccessStreak, Is.Zero);
    }

    [Test]
    public void InspectorTuningIsClampedToValidRhythmSettings()
    {
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("baseMoveSpeed").floatValue = 4f;
        serializedController.FindProperty("maxMoveSpeed").floatValue = 2f;
        serializedController.FindProperty("successesToMaxSpeed").intValue = 0;
        serializedController.FindProperty("stepDuration").floatValue = 0.01f;
        serializedController.FindProperty("successWindow").floatValue = 1f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        serializedController.Update();

        Assert.That(serializedController.FindProperty("stepDuration").floatValue,
            Is.EqualTo(0.05f).Within(0.001f));
        Assert.That(serializedController.FindProperty("successWindow").floatValue,
            Is.EqualTo(0.05f).Within(0.001f));
        Assert.That(serializedController.FindProperty("maxMoveSpeed").floatValue,
            Is.EqualTo(4f).Within(0.001f));
        Assert.That(serializedController.FindProperty("successesToMaxSpeed").intValue, Is.EqualTo(1));
    }
}
