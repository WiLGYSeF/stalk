using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.TestBase;

public static class JobTaskBuilderExtensions
{
    public static JobTaskBuilder WithRandomId(this JobTaskBuilder builder)
    {
        return builder.WithId(RandomValues.RandomJobTaskId());
    }

    public static JobTaskBuilder WithRandomInitializedState(this JobTaskBuilder builder, JobTaskState state)
    {
        return builder.WithRandomId()
            .WithState(state)
            .WithUri(RandomValues.RandomUri())
            .WithRandomStartFinishTimes()
            .WithRandomResult();
    }

    public static JobTaskBuilder WithRandomStartFinishTimes(this JobTaskBuilder builder)
    {
        var curTime = DateTime.Now;
        DateTime? startTime = RandomValues.RandomDateTime(curTime.AddDays(-1), curTime.AddMinutes(-1));
        DateTime? endTime = RandomValues.RandomDateTime(startTime.Value, startTime.Value.AddHours(12));

        switch (builder.State)
        {
            case JobTaskState.Active:
            case JobTaskState.Cancelling:
            case JobTaskState.Paused:
            case JobTaskState.Pausing:
                endTime = null;
                break;
            case JobTaskState.Inactive:
                startTime = null;
                endTime = null;
                break;
            case JobTaskState.Completed:
            case JobTaskState.Failed:
            case JobTaskState.Cancelled:
                break;
            default:
                throw new NotImplementedException();
        }

        return builder
            .WithStartedTime(startTime)
            .WithFinishedTime(endTime);
    }

    public static JobTaskBuilder WithRandomResult(this JobTaskBuilder builder)
    {
        switch (builder.State)
        {
            case JobTaskState.Completed:
                builder.WithRandomSuccessResult();
                break;
            case JobTaskState.Failed:
                builder.WithRandomFailedResult();
                break;
            case JobTaskState.Active:
            case JobTaskState.Cancelling:
            case JobTaskState.Paused:
            case JobTaskState.Pausing:
            case JobTaskState.Inactive:
            case JobTaskState.Cancelled:
                break;
            default:
                throw new NotImplementedException();
        }

        return builder;
    }

    public static JobTaskBuilder WithRandomSuccessResult(this JobTaskBuilder builder)
    {
        return builder.WithResult(JobTaskResult.Create(success: true));
    }

    public static JobTaskBuilder WithRandomFailedResult(this JobTaskBuilder builder)
    {
        return builder.WithResult(JobTaskResult.Create(
            success: false,
            errorCode: RandomValues.RandomString(4),
            errorMessage: RandomValues.RandomString(10),
            errorDetail: RandomValues.RandomString(20)));
    }
}
