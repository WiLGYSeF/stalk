using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.TestBase;

public static class JobBuilderExtensions
{
    public static JobBuilder WithRandomId(this JobBuilder builder)
    {
        return builder.WithId(RandomValues.RandomJobId());
    }

    public static JobBuilder WithRandomInitializedState(this JobBuilder builder, JobState state)
    {
        return builder.WithRandomId()
            .WithState(state)
            .WithRandomStartFinishTimes();
    }

    public static JobBuilder WithRandomStartFinishTimes(this JobBuilder builder)
    {
        var curTime = DateTime.Now;
        DateTime? startTime = RandomValues.RandomDateTime(curTime.AddDays(-1), curTime.AddMinutes(-1));
        DateTime? endTime = RandomValues.RandomDateTime(startTime.Value, startTime.Value.AddHours(12));

        switch (builder.State)
        {
            case JobState.Active:
            case JobState.Cancelling:
            case JobState.Paused:
            case JobState.Pausing:
                endTime = null;
                break;
            case JobState.Inactive:
                startTime = null;
                endTime = null;
                break;
            case JobState.Completed:
            case JobState.Failed:
            case JobState.Cancelled:
                break;
            default:
                throw new NotImplementedException();
        }

        return builder
            .WithStartedTime(startTime)
            .WithFinishedTime(endTime);
    }

    public static JobBuilder WithRandomTasks(this JobBuilder builder, JobTaskState taskState, int count, JobTaskType type = JobTaskType.Extract)
    {
        for (var i = 0; i < count; i++)
        {
            var taskBuilder = new JobTaskBuilder();
            builder.WithTasks(taskBuilder
                .WithRandomInitializedState(taskState)
                .WithType(type)
                .Create());
        }

        return builder;
    }
}
