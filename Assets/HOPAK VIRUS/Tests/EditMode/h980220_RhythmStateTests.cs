using NUnit.Framework;

public sealed class h980220_RhythmStateTests
{
    private h980220_RhythmState state;

    [SetUp]
    public void SetUp() => state = new h980220_RhythmState(0.5f, 0.2f, 2f, 6f, 4);

    [Test]
    public void FirstInputStartsWithoutMovement()
    {
        Assert.That(state.RegisterInput(h980220_Leg.Left), Is.EqualTo(h980220_RhythmInputResult.Started));
        Assert.That(state.IsMoving, Is.False);
        Assert.That(state.SuccessStreak, Is.Zero);
    }

    [Test]
    public void AlternatingInsideLandingWindowRaisesSpeed()
    {
        state.RegisterInput(h980220_Leg.Left);
        state.Tick(0.35f);
        Assert.That(state.RegisterInput(h980220_Leg.Right), Is.EqualTo(h980220_RhythmInputResult.Success));
        Assert.That(state.SuccessStreak, Is.EqualTo(1));
        Assert.That(state.CurrentSpeed, Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void ReferenceSpeedIsNotACapAndCadenceKeepsAccelerating()
    {
        state.RegisterInput(h980220_Leg.Left);
        h980220_Leg next = h980220_Leg.Right;
        for (int i = 0; i < 8; i++)
        {
            state.Tick(state.CurrentStepDuration * 0.7f);
            state.RegisterInput(next);
            next = next == h980220_Leg.Left ? h980220_Leg.Right : h980220_Leg.Left;
        }

        Assert.That(state.CurrentSpeed, Is.EqualTo(10f).Within(0.001f));
        Assert.That(state.CurrentStepDuration, Is.LessThan(0.25f));
    }

    [Test]
    public void OldCadenceFailsAfterStreakMakesRequiredInputFaster()
    {
        state.RegisterInput(h980220_Leg.Left);
        h980220_Leg next = h980220_Leg.Right;
        for (int i = 0; i < 4; i++)
        {
            state.Tick(state.CurrentStepDuration * 0.7f);
            Assert.That(state.RegisterInput(next), Is.EqualTo(h980220_RhythmInputResult.Success));
            next = next == h980220_Leg.Left ? h980220_Leg.Right : h980220_Leg.Left;
        }

        Assert.That(state.CurrentStepDuration, Is.LessThan(0.35f));
        state.Tick(0.35f);

        Assert.That(state.SuccessStreak, Is.Zero);
        Assert.That(state.IsMoving, Is.False);
    }

    [Test]
    public void EarlyOrRepeatedFootResetsStreak()
    {
        state.RegisterInput(h980220_Leg.Left);
        state.Tick(0.35f);
        state.RegisterInput(h980220_Leg.Right);
        state.Tick(0.1f);
        Assert.That(state.RegisterInput(h980220_Leg.Left), Is.EqualTo(h980220_RhythmInputResult.Failed));
        Assert.That(state.SuccessStreak, Is.Zero);
        Assert.That(state.IsMoving, Is.False);
    }

    [Test]
    public void MissingLandingWindowStopsMovement()
    {
        state.RegisterInput(h980220_Leg.Left);
        state.Tick(0.51f);
        Assert.That(state.ActiveLeg, Is.EqualTo(h980220_Leg.None));
        Assert.That(state.IsMoving, Is.False);
    }
}
