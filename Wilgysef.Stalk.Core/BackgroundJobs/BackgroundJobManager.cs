using Wilgysef.Stalk.Core.Models;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobManager : IBackgroundJobManager
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;

    public BackgroundJobManager(
        IUnitOfWork unitOfWork,
        IBackgroundJobDispatcher backgroundJobDispatcher)
    {
        _unitOfWork = unitOfWork;
        _backgroundJobDispatcher = backgroundJobDispatcher;
    }

    public async Task<BackgroundJob> EnqueueJobAsync(BackgroundJob job, bool saveChanges)
    {
        var entity = await _unitOfWork.BackgroundJobRepository.AddAsync(job);
        if (saveChanges)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return entity;
    }

    public async Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges)
    {
        return await EnqueueOrReplaceJobAsync(job, saveChanges, (a, b) => a.GetType() == b.GetType());
    }

    public async Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges, Func<BackgroundJobArgs, BackgroundJobArgs, bool> compareTo)
    {
        var queuedJobs = await _unitOfWork.BackgroundJobRepository.ListAsync(
            new BackgroundJobQuerySpecification(_backgroundJobDispatcher.ActiveJobs, job));
        var jobArgs = job.DeserializeArgs();

        BackgroundJob? existingJob = null;

        foreach (var queuedJob in queuedJobs)
        {
            var args = queuedJob.DeserializeArgs();
            if (compareTo(jobArgs, args))
            {
                existingJob = queuedJob;
                break;
            }
        }

        if (existingJob != null)
        {
            _unitOfWork.BackgroundJobRepository.Remove(existingJob);
        }

        return await EnqueueJobAsync(job, saveChanges);
    }

    public async Task<BackgroundJob?> FindJob(long id)
    {
        return await _unitOfWork.BackgroundJobRepository.FindAsync(id);
    }

    public async Task<BackgroundJob?> GetNextPriorityJobAsync()
    {
        return await _unitOfWork.BackgroundJobRepository.FirstOrDefaultAsync(
            new QueuedBackgroundJobSpecification(_backgroundJobDispatcher.ActiveJobs));
    }

    public async Task UpdateJobAsync(BackgroundJob job)
    {
        _unitOfWork.BackgroundJobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteJobAsync(BackgroundJob job)
    {
        _unitOfWork.BackgroundJobRepository.Remove(job);
        await _unitOfWork.SaveChangesAsync();
    }
}
