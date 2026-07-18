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
    private h980220_Leg recoverableLeg;
    private int recoverableStreak;
    private bool recoverableMoving;
    private float recoveryRemaining;
    private float dashGraceRemaining;

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
        if (dashGraceRemaining > 0f)
        {
            if (!opposite)
                return h980220_RhythmInputResult.Failed;

            CompleteStep(leg);
            return h980220_RhythmInputResult.Success;
        }

        if (!opposite || !inWindow)
        {
            BreakRhythm();
            return h980220_RhythmInputResult.Failed;
        }

        CompleteStep(leg);
        return h980220_RhythmInputResult.Success;
    }

    public float RegisterDash(float graceSeconds)
    {
        if (ActiveLeg == h980220_Leg.None && recoveryRemaining > 0f)
        {
            ActiveLeg = recoverableLeg;
            SuccessStreak = recoverableStreak;
            IsMoving = recoverableMoving;
        }

        float dashDuration = CurrentStepDuration;
        h980220_Leg dashLeg = ActiveLeg == h980220_Leg.Left
            ? h980220_Leg.Right
            : h980220_Leg.Left;
        CompleteStep(dashLeg);
        dashGraceRemaining = Mathf.Max(dashGraceRemaining, Mathf.Max(0f, graceSeconds));
        recoveryRemaining = 0f;
        return dashDuration;
    }

    public void Tick(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        recoveryRemaining = Mathf.Max(0f, recoveryRemaining - safeDeltaTime);
        dashGraceRemaining = Mathf.Max(0f, dashGraceRemaining - safeDeltaTime);

        if (ActiveLeg == h980220_Leg.None)
            return;
        StepElapsed += safeDeltaTime;
        if (dashGraceRemaining <= 0f && StepElapsed > CurrentStepDuration)
            BreakRhythm();
    }

    public float NormalizedStep => ActiveLeg == h980220_Leg.None
        ? 0f : Mathf.Clamp01(StepElapsed / CurrentStepDuration);

    public void Reset()
    {
        ResetCore();
        recoverableLeg = h980220_Leg.None;
        recoverableStreak = 0;
        recoverableMoving = false;
        recoveryRemaining = 0f;
        dashGraceRemaining = 0f;
    }

    private void CompleteStep(h980220_Leg leg)
    {
        ActiveLeg = leg;
        StepElapsed = 0f;
        SuccessStreak++;
        IsMoving = true;
    }

    private void BreakRhythm()
    {
        if (ActiveLeg != h980220_Leg.None)
        {
            recoverableLeg = ActiveLeg;
            recoverableStreak = SuccessStreak;
            recoverableMoving = IsMoving;
            recoveryRemaining = 0.5f;
        }

        ResetCore();
    }

    private void ResetCore()
    {
        ActiveLeg = h980220_Leg.None;
        SuccessStreak = 0;
        StepElapsed = 0f;
        IsMoving = false;
    }
}
