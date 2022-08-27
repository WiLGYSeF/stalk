using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Specifications;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTaskManager : IJobTaskManager
{
    private readonly IUnitOfWork _unitOfWork;

    public JobTaskManager(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<JobTask> CreateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.JobTaskRepository.AddAsync(jobTask, cancellationToken);

        // TODO: events?

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task CreateJobTasksAsync(IEnumerable<JobTask> jobTasks, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.JobTaskRepository.AddRangeAsync(jobTasks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobTask> GetJobTaskAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.JobTaskRepository.FirstOrDefaultAsync(
            new JobTaskSingleSpecification(jobTaskId: id),
            cancellationToken);
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(JobTask), id);
        }

        return entity;
    }

    public async Task<JobTask> UpdateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        var entity = _unitOfWork.JobTaskRepository.Update(jobTask);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        if (jobTask.IsActive)
        {
            throw new JobTaskActiveException();
        }

        _unitOfWork.JobTaskRepository.Remove(jobTask);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetJobTaskActiveAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        if (jobTask.IsActive)
        {
            return;
        }

        jobTask.ChangeState(JobTaskState.Active);

        _unitOfWork.JobTaskRepository.Update(jobTask);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
