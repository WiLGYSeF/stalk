namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public class JobTaskWorkerException : Exception
{
    public string Code { get; }

    public string? Details { get; }

    public JobTaskWorkerException(
        string code,
        string? message = null,
        string? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Details = details;
    }
}
