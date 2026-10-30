using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SupplierMaintenanceTests
{
    private static readonly HashSet<DateOnly> NoHolidays = [];
    private static SupplierMaintenanceSchedule Schedule(DateOnly start, DateOnly end, SupplierMaintenanceFrequency frequency = SupplierMaintenanceFrequency.MONTHLY) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Elevator inspection", "Monthly check", MaintenanceType.INSPECTION, frequency, start, end, 7, SupplierNonWorkingDayRule.NEXT_WORKING_DAY);
    [Fact] public void MonthlyDatesStepFromStartAndKeepMonthEndWithoutDrift()
    {
        var dates = SupplierMaintenanceSchedule.NominalDates(new(2027, 1, 31), new(2027, 5, 31), SupplierMaintenanceFrequency.MONTHLY);
        Assert.Equal(new DateOnly[] { new(2027, 1, 31), new(2027, 2, 28), new(2027, 3, 31), new(2027, 4, 30), new(2027, 5, 31) }, dates);
        Assert.Equal(4, SupplierMaintenanceSchedule.NominalDates(new(2027, 1, 1), new(2027, 1, 28), SupplierMaintenanceFrequency.WEEKLY).Count);
        Assert.Equal(new DateOnly[] { new(2027, 1, 15), new(2027, 4, 15), new(2027, 7, 15), new(2027, 10, 15) }, SupplierMaintenanceSchedule.NominalDates(new(2027, 1, 15), new(2027, 12, 31), SupplierMaintenanceFrequency.QUARTERLY));
        Assert.Single(SupplierMaintenanceSchedule.NominalDates(new(2027, 1, 1), new(2027, 12, 31), SupplierMaintenanceFrequency.YEARLY));
    }
    [Fact] public void WeekendAndHolidayRulesShiftInsideThePeriodOrFail()
    {
        var saturday = new DateOnly(2027, 1, 2); var holidays = new HashSet<DateOnly> { new(2027, 1, 4) };
        Assert.Equal(saturday, SupplierMaintenanceSchedule.PlannedDate(saturday, SupplierNonWorkingDayRule.KEEP, holidays, saturday, new(2027, 2, 1)));
        Assert.Equal(new DateOnly(2027, 1, 5), SupplierMaintenanceSchedule.PlannedDate(saturday, SupplierNonWorkingDayRule.NEXT_WORKING_DAY, holidays, saturday, new(2027, 2, 1)));
        Assert.Equal(new DateOnly(2027, 1, 1), SupplierMaintenanceSchedule.PlannedDate(saturday, SupplierNonWorkingDayRule.PREVIOUS_WORKING_DAY, holidays, new(2027, 1, 1), new(2027, 2, 1)));
        Assert.Throws<ArgumentException>(() => SupplierMaintenanceSchedule.PlannedDate(saturday, SupplierNonWorkingDayRule.PREVIOUS_WORKING_DAY, holidays, saturday, new(2027, 2, 1)));
        Assert.Throws<ArgumentException>(() => SupplierMaintenanceSchedule.PlannedDate(saturday, SupplierNonWorkingDayRule.NEXT_WORKING_DAY, holidays, saturday, new(2027, 1, 4)));
    }
    [Fact] public void PeriodReminderTextAndOccurrenceLimitsAreValidated()
    {
        var start = new DateOnly(2027, 1, 1);
        Assert.Throws<ArgumentException>(() => Schedule(start, start.AddDays(-1)));
        Assert.Throws<ArgumentException>(() => Schedule(start, start.AddDays(SupplierMaintenanceSchedule.MaxSpanDays + 1)));
        Assert.Throws<ArgumentException>(() => new SupplierMaintenanceSchedule(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Title", null, MaintenanceType.OTHER, SupplierMaintenanceFrequency.WEEKLY, start, start, 61, SupplierNonWorkingDayRule.KEEP));
        Assert.Throws<ArgumentException>(() => new SupplierMaintenanceSchedule(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Title", null, MaintenanceType.OTHER, SupplierMaintenanceFrequency.WEEKLY, start, start, 1, SupplierNonWorkingDayRule.KEEP));
        Assert.Throws<ArgumentException>(() => new SupplierMaintenanceSchedule(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Bad\u0000", null, MaintenanceType.OTHER, SupplierMaintenanceFrequency.WEEKLY, start, start, 1, SupplierNonWorkingDayRule.KEEP));
        Assert.Equal(105, SupplierMaintenanceSchedule.NominalDates(start, start.AddDays(SupplierMaintenanceSchedule.MaxSpanDays), SupplierMaintenanceFrequency.WEEKLY).Count);
    }
    [Fact] public void OccurrenceCompletionRecordsDelayOnceAndRejectsFutureOrReopening()
    {
        var schedule = Schedule(new(2027, 1, 1), new(2027, 6, 30)); var occurrence = new SupplierMaintenanceOccurrence(schedule, 1, new(2027, 1, 2), new(2027, 1, 4), Guid.NewGuid());
        Assert.True(occurrence.IsOverdue(new(2027, 1, 5))); Assert.False(occurrence.IsOverdue(new(2027, 1, 4)));
        Assert.True(occurrence.IsReminderDue(new(2026, 12, 28), 7)); Assert.False(occurrence.IsReminderDue(new(2026, 12, 27), 7));
        Assert.Throws<ArgumentException>(() => occurrence.Complete(new(2027, 1, 9), "Done", null, new(2027, 1, 8)));
        Assert.Throws<ArgumentException>(() => occurrence.Complete(new(2027, 1, 8), " ", null, new(2027, 1, 8)));
        Assert.Equal(SupplierMaintenanceOccurrenceStatus.PLANNED, occurrence.Status);
        occurrence.Complete(new(2027, 1, 8), "Inspection report signed", null, new(2027, 1, 8));
        Assert.Equal(4, occurrence.DelayDays); Assert.False(occurrence.IsOverdue(new(2027, 2, 1)));
        Assert.Throws<InvalidOperationException>(() => occurrence.Complete(new(2027, 1, 8), "Again", null, new(2027, 1, 8)));
        Assert.Throws<InvalidOperationException>(() => occurrence.Skip("Too late"));
        var early = new SupplierMaintenanceOccurrence(schedule, 2, new(2027, 2, 2), new(2027, 2, 2), Guid.NewGuid()); early.Complete(new(2027, 1, 30), "Done early", null, new(2027, 1, 30)); Assert.Equal(0, early.DelayDays);
        Assert.Throws<ArgumentException>(() => new SupplierMaintenanceOccurrence(schedule, 3, new(2027, 7, 1), new(2027, 7, 1), Guid.NewGuid()));
    }
    [Fact] public void EndingRequiresReasonAndHappensOnce()
    {
        var schedule = Schedule(new(2027, 1, 1), new(2027, 3, 1)); Assert.Throws<ArgumentException>(() => schedule.End(" "));
        schedule.End("Contract terminated"); Assert.Equal(SupplierMaintenanceScheduleStatus.ENDED, schedule.Status); Assert.Throws<InvalidOperationException>(() => schedule.End("Again"));
        Assert.Throws<ArgumentException>(() => new SupplierHoliday(Guid.NewGuid(), new(1999, 1, 1), "Old"));
    }
}
