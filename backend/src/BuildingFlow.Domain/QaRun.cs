namespace BuildingFlow.Domain;

public enum QaRunStatus { QUEUED, RUNNING, PASSED, FAILED, CANCELLED, TIMED_OUT }
public enum QaStepStatus { PASSED, FAILED, SKIPPED }

public sealed class QaRun : BaseEntity
{
    public const int MaxAttempts = 5;
    private QaRun() { }
    public QaRun(string code, string scenarioKey, Guid requestId, int timeoutSeconds, DateTimeOffset queuedAt, QaRun? retryOf = null)
    {
        if (requestId == Guid.Empty || timeoutSeconds is < 1 or > 3600) throw new ArgumentException("Invalid QA run.");
        if (retryOf is not null && (!retryOf.CanRetry || retryOf.Attempt >= MaxAttempts || retryOf.ScenarioKey != scenarioKey))
            throw new InvalidOperationException("QA run cannot be retried.");
        Code = Required(code, nameof(code), 40);
        ScenarioKey = Required(scenarioKey, nameof(scenarioKey), 60);
        RequestId = requestId;
        TimeoutSeconds = timeoutSeconds;
        QueuedAt = queuedAt;
        RetryOfId = retryOf?.Id;
        Attempt = (retryOf?.Attempt ?? 0) + 1;
    }
    public string Code { get; private set; } = null!;
    public string ScenarioKey { get; private set; } = null!;
    public Guid RequestId { get; private set; }
    public Guid? RetryOfId { get; private set; }
    public int Attempt { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public QaRunStatus Status { get; private set; } = QaRunStatus.QUEUED;
    public bool CancelRequested { get; private set; }
    public DateTimeOffset QueuedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? ResultCode { get; private set; }
    public int PassedSteps { get; private set; }
    public int FailedSteps { get; private set; }
    public bool IsActive => Status is QaRunStatus.QUEUED or QaRunStatus.RUNNING;
    public bool CanRetry => Status is QaRunStatus.FAILED or QaRunStatus.CANCELLED or QaRunStatus.TIMED_OUT;

    public void Start(DateTimeOffset now)
    {
        Require(QaRunStatus.QUEUED);
        Status = QaRunStatus.RUNNING;
        StartedAt = now;
    }

    public void Record(QaStepStatus status)
    {
        Require(QaRunStatus.RUNNING);
        if (status == QaStepStatus.PASSED) PassedSteps++;
        else if (status == QaStepStatus.FAILED) FailedSteps++;
    }

    public void Complete(DateTimeOffset now) =>
        Finish(FailedSteps == 0 && PassedSteps > 0 ? QaRunStatus.PASSED : QaRunStatus.FAILED, FailedSteps == 0 && PassedSteps > 0 ? null : "STEP_FAILED", now);

    public void RequestCancel(DateTimeOffset now)
    {
        if (Status == QaRunStatus.QUEUED) Finish(QaRunStatus.CANCELLED, "CANCELLED", now, QaRunStatus.QUEUED);
        else if (Status == QaRunStatus.RUNNING) CancelRequested = true;
        else throw new InvalidOperationException("QA run is not active.");
    }

    public void Cancel(DateTimeOffset now) => Finish(QaRunStatus.CANCELLED, "CANCELLED", now);
    public void TimeOut(DateTimeOffset now) => Finish(QaRunStatus.TIMED_OUT, "TIMEOUT", now);
    public void Interrupt(DateTimeOffset now) => Finish(QaRunStatus.FAILED, "INTERRUPTED", now);

    private void Finish(QaRunStatus status, string? resultCode, DateTimeOffset now, QaRunStatus from = QaRunStatus.RUNNING)
    {
        Require(from);
        Status = status;
        ResultCode = resultCode;
        FinishedAt = now;
    }

    private void Require(QaRunStatus expected)
    {
        if (Status != expected) throw new InvalidOperationException($"QA run must be {expected}.");
    }
}

public sealed class QaRunStep : BaseEntity
{
    private QaRunStep() { }
    public QaRunStep(Guid runId, int sequence, string stepKey, QaStepStatus status, string expected, string actual, long durationMs)
    {
        if (runId == Guid.Empty || sequence is < 1 or > 500 || durationMs < 0) throw new ArgumentException("Invalid QA step.");
        RunId = runId;
        Sequence = sequence;
        StepKey = Required(stepKey, nameof(stepKey), 80);
        Status = status;
        Expected = Required(expected, nameof(expected), 300);
        Actual = Required(actual, nameof(actual), 300);
        DurationMs = durationMs;
    }
    public Guid RunId { get; private set; }
    public int Sequence { get; private set; }
    public string StepKey { get; private set; } = null!;
    public QaStepStatus Status { get; private set; }
    public string Expected { get; private set; } = null!;
    public string Actual { get; private set; } = null!;
    public long DurationMs { get; private set; }
}
