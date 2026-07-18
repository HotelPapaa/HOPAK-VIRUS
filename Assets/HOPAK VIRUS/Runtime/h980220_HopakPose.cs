using UnityEngine;

public readonly struct h980220_LegPose
{
    public readonly float LeftThighX;
    public readonly float LeftShinX;
    public readonly float RightThighX;
    public readonly float RightShinX;
    public readonly float TorsoDip;
    public readonly float TorsoLean;
    public readonly h980220_Leg ActiveLeg;
    public readonly float Weight;

    public h980220_LegPose(
        float leftThighX, float leftShinX, float rightThighX, float rightShinX,
        float torsoDip, float torsoLean,
        h980220_Leg activeLeg = h980220_Leg.None, float weight = 0f)
    {
        LeftThighX = leftThighX;
        LeftShinX = leftShinX;
        RightThighX = rightThighX;
        RightShinX = rightShinX;
        TorsoDip = torsoDip;
        TorsoLean = torsoLean;
        ActiveLeg = activeLeg;
        Weight = weight;
    }
}

public static class h980220_HopakPose
{
    public static h980220_LegPose Evaluate(h980220_Leg activeLeg, float normalizedStep)
    {
        if (activeLeg == h980220_Leg.None)
            return new h980220_LegPose(0f, 0f, 0f, 0f, 0f, 0f);

        float lift = Mathf.Sin(Mathf.Clamp01(normalizedStep) * Mathf.PI);
        if (activeLeg == h980220_Leg.Left)
            return new h980220_LegPose(
                -95.676f * lift, -96.41f * lift,
                -109.086f * lift, 17.513f * lift,
                lift, -lift, activeLeg, lift);

        return new h980220_LegPose(
            -109.086f * lift, 17.513f * lift,
            -95.676f * lift, -96.41f * lift,
            lift, lift, activeLeg, lift);
    }

    public static Vector3 LeftThighTarget(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Left
            ? new Vector3(-0.4f, 0.942f, 0.282f)
            : new Vector3(-0.314f, 0.899f, 0.347f);

    public static Vector3 LeftShinTarget(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Left
            ? new Vector3(-0.4f, 1.023f, 1.029f)
            : new Vector3(-0.26f, 0.435f, 0.676f);

    public static Vector3 RightThighTarget(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Right
            ? new Vector3(0.4f, 0.942f, 0.282f)
            : new Vector3(0.314f, 0.899f, 0.347f);

    public static Vector3 RightShinTarget(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Right
            ? new Vector3(0.4f, 1.023f, 1.029f)
            : new Vector3(0.26f, 0.435f, 0.676f);

    public static Quaternion LeftThighRotation(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Left
            ? Quaternion.Euler(-95.676f, 0f, 0f)
            : Quaternion.Euler(-109.086f, 6.005f, -3.579f);

    public static Quaternion LeftShinRotation(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Left
            ? Quaternion.Euler(-96.41f, 0f, 0f)
            : Quaternion.Euler(17.513f, 0f, 0f);

    public static Quaternion RightThighRotation(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Right
            ? Quaternion.Euler(-95.676f, 0f, 0f)
            : Quaternion.Euler(-109.086f, -6.005f, 3.579f);

    public static Quaternion RightShinRotation(h980220_Leg activeLeg) =>
        activeLeg == h980220_Leg.Right
            ? Quaternion.Euler(-96.41f, 0f, 0f)
            : Quaternion.Euler(17.513f, 0f, 0f);

    public static Quaternion TorsoRotation(h980220_Leg activeLeg, float strength)
    {
        float side = activeLeg == h980220_Leg.Left ? -1f : 1f;
        return Quaternion.Euler(
            2f * strength,
            4.334f * side * strength,
            14.331f * side * strength);
    }
}
