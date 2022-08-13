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

    public async Task<BackgroundJob?> GetNextPriorityJobAsync()
    {
        return await _unitOfWork.BackgroundJobRepository.FirstOrDefaultAsync(
            new BackgroundJobSpecification(_backgroundJobDispatcher.ActiveJobs));
    }

    //public async Task<bool> IsJobQueued()
    //{
    //    return await _unitOfWork.BackgroundJobRepository.AnyAsync();
    //}

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
