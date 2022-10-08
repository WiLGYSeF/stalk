namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public class JobTaskWorkerException : Exception
{
    public string Code { get; }

    public JobTaskWorkerException(
        string code,
        string? message = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }
}
