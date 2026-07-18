using System;
using UnityEngine;

public enum h980220_Leg { None, Left, Right }
public enum h980220_RhythmInputResult { Started, Success, Failed }

[Serializable]
public sealed class h980220_RhythmState
{
    public h980220_Leg ActiveLeg { get; private set; }
    public int SuccessStreak { get; private set; }
    public float StepElapsed { get; private set; }
    public bool IsMoving { get; private set; }
    public float CurrentSpeed => Mathf.Lerp(baseSpeed, maxSpeed,
        Mathf.Clamp01((float)SuccessStreak / successesToMaxSpeed));

    private readonly float stepDuration;
    private readonly float successWindow;
    private readonly float baseSpeed;
    private readonly float maxSpeed;
    private readonly int successesToMaxSpeed;

    public h980220_RhythmState(float stepDuration, float successWindow,
        float baseSpeed, float maxSpeed, int successesToMaxSpeed)
    {
        this.stepDuration = Mathf.Max(0.05f, stepDuration);
        this.successWindow = Mathf.Clamp(successWindow, 0.01f, this.stepDuration);
        this.baseSpeed = Mathf.Max(0f, baseSpeed);
        this.maxSpeed = Mathf.Max(this.baseSpeed, maxSpeed);
        this.successesToMaxSpeed = Mathf.Max(1, successesToMaxSpeed);
        Reset();
    }

    public h980220_RhythmInputResult RegisterInput(h980220_Leg leg)
    {
        if (leg == h980220_Leg.None)
            throw new ArgumentOutOfRangeException(nameof(leg));

        if (ActiveLeg == h980220_Leg.None)
        {
            ActiveLeg = leg;
            StepElapsed = 0f;
            IsMoving = false;
            return h980220_RhythmInputResult.Started;
        }

        bool opposite = ActiveLeg != leg;
        bool inWindow = StepElapsed >= stepDuration - successWindow && StepElapsed <= stepDuration;
        if (!opposite || !inWindow)
        {
            Reset();
            return h980220_RhythmInputResult.Failed;
        }

        ActiveLeg = leg;
        StepElapsed = 0f;
        SuccessStreak++;
        IsMoving = true;
        return h980220_RhythmInputResult.Success;
    }

    public void Tick(float deltaTime)
    {
        if (ActiveLeg == h980220_Leg.None)
            return;
        StepElapsed += Mathf.Max(0f, deltaTime);
        if (StepElapsed > stepDuration)
            Reset();
    }

    public float NormalizedStep => ActiveLeg == h980220_Leg.None
        ? 0f : Mathf.Clamp01(StepElapsed / stepDuration);

    public void Reset()
    {
        ActiveLeg = h980220_Leg.None;
        SuccessStreak = 0;
        StepElapsed = 0f;
        IsMoving = false;
    }
}
