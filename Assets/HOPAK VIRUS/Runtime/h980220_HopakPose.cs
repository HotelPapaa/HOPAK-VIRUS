using UnityEngine;

public readonly struct h980220_LegPose
{
    public readonly float LeftThighX;
    public readonly float LeftShinX;
    public readonly float RightThighX;
    public readonly float RightShinX;
    public readonly float TorsoDip;
    public readonly float TorsoLean;

    public h980220_LegPose(
        float leftThighX, float leftShinX, float rightThighX, float rightShinX,
        float torsoDip, float torsoLean)
    {
        LeftThighX = leftThighX;
        LeftShinX = leftShinX;
        RightThighX = rightThighX;
        RightShinX = rightShinX;
        TorsoDip = torsoDip;
        TorsoLean = torsoLean;
    }
}

public static class h980220_HopakPose
{
    public static h980220_LegPose Evaluate(h980220_Leg activeLeg, float normalizedStep)
    {
        if (activeLeg == h980220_Leg.None)
            return new h980220_LegPose(0f, 0f, 0f, 0f, 0f, 0f);

        float lift = Mathf.Sin(Mathf.Clamp01(normalizedStep) * Mathf.PI);
        float liftedThigh = -70f * lift;
        float bentShin = 90f * lift;

        if (activeLeg == h980220_Leg.Left)
            return new h980220_LegPose(liftedThigh, bentShin, 0f, 0f, lift, lift);

        return new h980220_LegPose(0f, 0f, liftedThigh, bentShin, lift, -lift);
    }
}
