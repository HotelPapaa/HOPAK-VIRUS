using UnityEngine;

public readonly struct h980220_LegPose
{
    public readonly float LeftThighX;
    public readonly float LeftShinX;
    public readonly float RightThighX;
    public readonly float RightShinX;

    public h980220_LegPose(float leftThighX, float leftShinX, float rightThighX, float rightShinX)
    {
        LeftThighX = leftThighX;
        LeftShinX = leftShinX;
        RightThighX = rightThighX;
        RightShinX = rightShinX;
    }
}

public static class h980220_HopakPose
{
    public static h980220_LegPose Evaluate(h980220_Leg activeLeg, float normalizedStep)
    {
        if (activeLeg == h980220_Leg.None)
            return new h980220_LegPose(0f, 0f, 0f, 0f);

        float lift = Mathf.Sin(Mathf.Clamp01(normalizedStep) * Mathf.PI);
        float liftedThigh = -70f * lift;
        float bentShin = 90f * lift;

        if (activeLeg == h980220_Leg.Left)
            return new h980220_LegPose(liftedThigh, bentShin, 0f, 0f);

        return new h980220_LegPose(0f, 0f, liftedThigh, bentShin);
    }
}
