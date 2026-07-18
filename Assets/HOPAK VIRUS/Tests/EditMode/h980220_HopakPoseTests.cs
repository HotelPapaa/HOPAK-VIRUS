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
        Assert.That(pose.TorsoDip, Is.EqualTo(1f).Within(0.01f));
        Assert.That(pose.TorsoLean, Is.EqualTo(1f).Within(0.01f));
    }

    [Test]
    public void NeutralPoseHasStraightLegs()
    {
        h980220_LegPose pose = h980220_HopakPose.Evaluate(h980220_Leg.None, 0f);

        Assert.That(pose.LeftThighX, Is.Zero);
        Assert.That(pose.LeftShinX, Is.Zero);
        Assert.That(pose.RightThighX, Is.Zero);
        Assert.That(pose.RightShinX, Is.Zero);
        Assert.That(pose.TorsoDip, Is.Zero);
        Assert.That(pose.TorsoLean, Is.Zero);
    }

    [Test]
    public void RightLegAtMidStepRaisesOnlyRightSegments()
    {
        h980220_LegPose pose = h980220_HopakPose.Evaluate(h980220_Leg.Right, 0.5f);

        Assert.That(pose.LeftThighX, Is.EqualTo(0f).Within(0.01f));
        Assert.That(pose.LeftShinX, Is.EqualTo(0f).Within(0.01f));
        Assert.That(pose.RightThighX, Is.LessThan(-50f));
        Assert.That(pose.RightShinX, Is.GreaterThan(60f));
        Assert.That(pose.TorsoDip, Is.EqualTo(1f).Within(0.01f));
        Assert.That(pose.TorsoLean, Is.EqualTo(-1f).Within(0.01f));
    }
}

public sealed class h980220_PlayerRhythmControllerTests
{
    private GameObject player;
    private h980220_PlayerRhythmController controller;
    private Transform leftThigh;
    private Transform leftShin;
    private Transform rightThigh;
    private Transform rightShin;
    private Transform torso;
    private Vector3 torsoBasePosition;
    private Quaternion torsoBaseRotation;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("player");
        controller = player.AddComponent<h980220_PlayerRhythmController>();
        controller.Awake();
        leftThigh = CreateLegSegment("left thigh");
        leftShin = CreateLegSegment("left shin");
        rightThigh = CreateLegSegment("right thigh");
        rightShin = CreateLegSegment("right shin");
        torso = CreateLegSegment("Torso");
        torsoBasePosition = new Vector3(0.2f, 2.3f, -0.1f);
        torsoBaseRotation = Quaternion.Euler(3f, 4f, 5f);
        torso.localPosition = torsoBasePosition;
        torso.localRotation = torsoBaseRotation;

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("leftThigh").objectReferenceValue = leftThigh;
        serializedController.FindProperty("leftShin").objectReferenceValue = leftShin;
        serializedController.FindProperty("rightThigh").objectReferenceValue = rightThigh;
        serializedController.FindProperty("rightShin").objectReferenceValue = rightShin;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
    }

    [Test]
    public void AwakeInitializesStateAndRequiredCharacterController()
    {
        Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);
        Assert.That(controller.CurrentSpeed, Is.EqualTo(2f).Within(0.001f));
        Assert.That(controller.SuccessStreak, Is.Zero);
    }

    [Test]
    public void SuccessfulAlternatingInputMovesThroughCharacterController()
    {
        controller.ProcessFrame(0f, true, false, 0f);
        controller.ProcessFrame(0.35f, false, true, 0f);

        Assert.That(controller.SuccessStreak, Is.EqualTo(1));
        Assert.That(controller.CurrentSpeed, Is.EqualTo(3f).Within(0.001f));
        Assert.That(player.transform.position.z, Is.EqualTo(1.05f).Within(0.001f));
    }

    [Test]
    public void EarlyRepeatedInputStopsPropulsionAndResetsStreak()
    {
        controller.ProcessFrame(0f, true, false, 0f);
        controller.ProcessFrame(0.35f, false, true, 0f);
        Vector3 positionAfterSuccess = player.transform.position;

        controller.ProcessFrame(0.1f, false, true, 0f);
        controller.ProcessFrame(0.4f, false, false, 0f);

        Assert.That(controller.SuccessStreak, Is.Zero);
        Assert.That(controller.CurrentSpeed, Is.EqualTo(2f).Within(0.001f));
        Assert.That(player.transform.position, Is.EqualTo(positionAfterSuccess));
    }

    [Test]
    public void ActiveStepAppliesExpectedPoseToAllFourSegments()
    {
        controller.ProcessFrame(0f, true, false, 0f);
        controller.ProcessFrame(0.25f, false, false, 0f);

        Assert.That(Mathf.DeltaAngle(0f, leftThigh.localEulerAngles.x), Is.EqualTo(-70f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(0f, leftShin.localEulerAngles.x), Is.EqualTo(90f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(0f, rightThigh.localEulerAngles.x), Is.EqualTo(0f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(0f, rightShin.localEulerAngles.x), Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void LeftStepDipsAutoFoundTorsoAndLeansTowardLiftedLeg()
    {
        controller.ProcessFrame(0f, true, false, 0f);
        controller.ProcessFrame(0.25f, false, false, 0f);

        Assert.That(torso.localPosition.x, Is.EqualTo(torsoBasePosition.x).Within(0.001f));
        Assert.That(torso.localPosition.y, Is.EqualTo(torsoBasePosition.y - 0.18f).Within(0.001f));
        Assert.That(torso.localPosition.z, Is.EqualTo(torsoBasePosition.z).Within(0.001f));
        Quaternion relativeRotation = Quaternion.Inverse(torsoBaseRotation) * torso.localRotation;
        Assert.That(Mathf.DeltaAngle(0f, relativeRotation.eulerAngles.z),
            Is.EqualTo(12f).Within(0.01f));
    }

    [Test]
    public void RightStepUsesSameDipAndOppositeTorsoLean()
    {
        controller.ProcessFrame(0f, false, true, 0f);
        controller.ProcessFrame(0.25f, false, false, 0f);

        Assert.That(torso.localPosition.y, Is.EqualTo(torsoBasePosition.y - 0.18f).Within(0.001f));
        Quaternion relativeRotation = Quaternion.Inverse(torsoBaseRotation) * torso.localRotation;
        Assert.That(Mathf.DeltaAngle(0f, relativeRotation.eulerAngles.z),
            Is.EqualTo(-12f).Within(0.01f));
    }

    [Test]
    public void DisablingInputResetsAndPreventsFurtherProcessing()
    {
        controller.ProcessFrame(0f, true, false, 0f);
        controller.ProcessFrame(0.35f, false, true, 0f);
        controller.ProcessFrame(0.25f, false, false, 0f);
        controller.SetInputEnabled(false);
        Vector3 disabledPosition = player.transform.position;
        Quaternion disabledRotation = player.transform.rotation;

        controller.ProcessFrame(1f, true, false, 1f);

        Assert.That(controller.SuccessStreak, Is.Zero);
        Assert.That(controller.CurrentSpeed, Is.EqualTo(2f).Within(0.001f));
        Assert.That(player.transform.position, Is.EqualTo(disabledPosition));
        Assert.That(player.transform.rotation, Is.EqualTo(disabledRotation));
        Assert.That(Mathf.DeltaAngle(0f, leftThigh.localEulerAngles.x), Is.EqualTo(0f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(0f, leftShin.localEulerAngles.x), Is.EqualTo(0f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(0f, rightThigh.localEulerAngles.x), Is.EqualTo(0f).Within(0.01f));
        Assert.That(Mathf.DeltaAngle(0f, rightShin.localEulerAngles.x), Is.EqualTo(0f).Within(0.01f));
        Assert.That(torso.localPosition, Is.EqualTo(torsoBasePosition));
        Assert.That(torso.localRotation, Is.EqualTo(torsoBaseRotation));
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
        serializedController.FindProperty("torsoBobHeight").floatValue = -1f;
        serializedController.FindProperty("torsoLeanDegrees").floatValue = -1f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        serializedController.Update();

        Assert.That(serializedController.FindProperty("stepDuration").floatValue,
            Is.EqualTo(0.05f).Within(0.001f));
        Assert.That(serializedController.FindProperty("successWindow").floatValue,
            Is.EqualTo(0.05f).Within(0.001f));
        Assert.That(serializedController.FindProperty("maxMoveSpeed").floatValue,
            Is.EqualTo(4f).Within(0.001f));
        Assert.That(serializedController.FindProperty("successesToMaxSpeed").intValue, Is.EqualTo(1));
        Assert.That(serializedController.FindProperty("torsoBobHeight").floatValue, Is.Zero);
        Assert.That(serializedController.FindProperty("torsoLeanDegrees").floatValue, Is.Zero);
    }

    private Transform CreateLegSegment(string name)
    {
        Transform segment = new GameObject(name).transform;
        segment.SetParent(player.transform, false);
        return segment;
    }
}
