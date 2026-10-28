using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// Test Merkezi koşum durum makinesi: yalnız izinli geçişler, iptal/yeniden deneme ve deneme sınırı.
public sealed class QaRunTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 17, 0, 0, TimeSpan.Zero);
    private static QaRun Run(QaRun? retryOf = null) => new("qa-test", "db.migrations", Guid.NewGuid(), 60, Now, retryOf);

    [Fact] public void PassesOnlyWhenEveryRecordedStepPassed()
    {
        var run = Run(); run.Start(Now); run.Record(QaStepStatus.PASSED); run.Record(QaStepStatus.SKIPPED); run.Complete(Now);
        Assert.Equal(QaRunStatus.PASSED, run.Status); Assert.Null(run.ResultCode); Assert.Equal(1, run.PassedSteps);
        var failed = Run(); failed.Start(Now); failed.Record(QaStepStatus.PASSED); failed.Record(QaStepStatus.FAILED); failed.Complete(Now);
        Assert.Equal(QaRunStatus.FAILED, failed.Status); Assert.Equal("STEP_FAILED", failed.ResultCode);
    }

    [Fact] public void RunWithoutPassedStepsIsNotAPass()
    {
        var run = Run(); run.Start(Now); run.Record(QaStepStatus.SKIPPED); run.Complete(Now);
        Assert.Equal(QaRunStatus.FAILED, run.Status);
    }

    [Fact] public void CancelIsImmediateWhenQueuedAndCooperativeWhenRunning()
    {
        var queued = Run(); queued.RequestCancel(Now);
        Assert.Equal(QaRunStatus.CANCELLED, queued.Status); Assert.Equal(Now, queued.FinishedAt);
        var running = Run(); running.Start(Now); running.RequestCancel(Now);
        Assert.Equal(QaRunStatus.RUNNING, running.Status); Assert.True(running.CancelRequested);
        running.Cancel(Now); Assert.Equal(QaRunStatus.CANCELLED, running.Status);
        Assert.Throws<InvalidOperationException>(() => running.RequestCancel(Now));
    }

    [Fact] public void InvalidTransitionsAreRejected()
    {
        var run = Run();
        Assert.Throws<InvalidOperationException>(() => run.Complete(Now));
        Assert.Throws<InvalidOperationException>(() => run.Record(QaStepStatus.PASSED));
        Assert.Throws<InvalidOperationException>(() => run.TimeOut(Now));
        run.Start(Now); Assert.Throws<InvalidOperationException>(() => run.Start(Now));
        run.TimeOut(Now); Assert.Equal(QaRunStatus.TIMED_OUT, run.Status); Assert.Equal("TIMEOUT", run.ResultCode);
        Assert.Throws<InvalidOperationException>(() => run.Interrupt(Now));
    }

    [Fact] public void RetryRequiresFinishedFailureAndStopsAtFiveAttempts()
    {
        var active = Run();
        Assert.Throws<InvalidOperationException>(() => Run(active));
        var current = active; current.Start(Now); current.Interrupt(Now);
        for (var attempt = 2; attempt <= QaRun.MaxAttempts; attempt++)
        {
            var next = Run(current); Assert.Equal(attempt, next.Attempt); Assert.Equal(current.Id, next.RetryOfId);
            next.Start(Now); next.TimeOut(Now); current = next;
        }
        Assert.Throws<InvalidOperationException>(() => Run(current));
        var passed = Run(); passed.Start(Now); passed.Record(QaStepStatus.PASSED); passed.Complete(Now);
        Assert.Throws<InvalidOperationException>(() => Run(passed));
    }

    [Fact] public void BoundsAreValidated()
    {
        Assert.Throws<ArgumentException>(() => new QaRun("qa-test", "db.migrations", Guid.Empty, 60, Now));
        Assert.Throws<ArgumentException>(() => new QaRun("qa-test", "db.migrations", Guid.NewGuid(), 0, Now));
        Assert.Throws<ArgumentException>(() => new QaRunStep(Guid.NewGuid(), 0, "step", QaStepStatus.PASSED, "a", "b", 0));
        Assert.Throws<ArgumentException>(() => new QaRunStep(Guid.NewGuid(), 1, "step", QaStepStatus.PASSED, "a", new string('x', 301), 0));
    }
}
