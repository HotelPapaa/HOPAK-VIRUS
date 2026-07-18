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
    public float CurrentSpeed => baseSpeed + speedPerSuccess * SuccessStreak;
    public float CurrentStepDuration =>
        stepDuration / (1f + SuccessStreak * cadenceAccelerationPerSuccess);
    public float CurrentSuccessWindow => CurrentStepDuration * successWindowRatio;

    private readonly float stepDuration;
    private readonly float successWindowRatio;
    private readonly float baseSpeed;
    private readonly float speedPerSuccess;
    private readonly float cadenceAccelerationPerSuccess;

    public h980220_RhythmState(float stepDuration, float successWindow,
        float baseSpeed, float referenceSpeed, int successesToReferenceSpeed,
        float cadenceAccelerationPerSuccess = 0.15f)
    {
        this.stepDuration = Mathf.Max(0.05f, stepDuration);
        float clampedWindow = Mathf.Clamp(successWindow, 0.01f, this.stepDuration);
        successWindowRatio = clampedWindow / this.stepDuration;
        this.baseSpeed = Mathf.Max(0f, baseSpeed);
        float clampedReferenceSpeed = Mathf.Max(this.baseSpeed, referenceSpeed);
        int clampedReferenceSuccesses = Mathf.Max(1, successesToReferenceSpeed);
        speedPerSuccess =
            (clampedReferenceSpeed - this.baseSpeed) / clampedReferenceSuccesses;
        this.cadenceAccelerationPerSuccess = Mathf.Max(0f, cadenceAccelerationPerSuccess);
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
        float currentDuration = CurrentStepDuration;
        bool inWindow = StepElapsed >= currentDuration - CurrentSuccessWindow &&
                        StepElapsed <= currentDuration;
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
        if (StepElapsed > CurrentStepDuration)
            Reset();
    }

    public float NormalizedStep => ActiveLeg == h980220_Leg.None
        ? 0f : Mathf.Clamp01(StepElapsed / CurrentStepDuration);

    public void Reset()
    {
        ActiveLeg = h980220_Leg.None;
        SuccessStreak = 0;
        StepElapsed = 0f;
        IsMoving = false;
    }
}
