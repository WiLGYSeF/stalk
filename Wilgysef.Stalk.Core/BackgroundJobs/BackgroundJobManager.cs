using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobManager : IBackgroundJobManager, ITransientDependency
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly IBackgroundJobCollectionService _backgroundJobCollectionService;

    public BackgroundJobManager(
        IUnitOfWork unitOfWork,
        IBackgroundJobRepository backgroundJobRepository,
        IBackgroundJobCollectionService backgroundJobCollectionService)
    {
        _unitOfWork = unitOfWork;
        _backgroundJobRepository = backgroundJobRepository;
        _backgroundJobCollectionService = backgroundJobCollectionService;
    }

    public async Task<BackgroundJob> EnqueueJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default)
    {
        var entity = await _backgroundJobRepository.AddAsync(job, cancellationToken);
        if (saveChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return entity;
    }

    public async Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default)
    {
        return await EnqueueOrReplaceJobAsync(job, saveChanges, (a, b) => a.GetType() == b.GetType(), cancellationToken);
    }

    public async Task<BackgroundJob> EnqueueOrReplaceJobAsync(
        BackgroundJob job,
        bool saveChanges,
        Func<BackgroundJobArgs, BackgroundJobArgs, bool> compareTo,
        CancellationToken cancellationToken = default)
    {
        var queuedJobs = await _backgroundJobRepository.ListAsync(
            new BackgroundJobQuerySpecification(_backgroundJobCollectionService.ActiveJobs, job),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var jobArgs = job.DeserializeArgs();

        var matchingJobs = queuedJobs.Where(j => compareTo(jobArgs, j.DeserializeArgs()));

        cancellationToken.ThrowIfCancellationRequested();

        if (matchingJobs.Any())
        {
            _backgroundJobRepository.RemoveRange(matchingJobs);
        }

        return await EnqueueJobAsync(job, saveChanges, cancellationToken);
    }

    public async Task<BackgroundJob?> FindJobAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _backgroundJobRepository.FindAsync(new object?[] { id }, cancellationToken: cancellationToken);
    }

    public async Task<List<BackgroundJob>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _backgroundJobRepository.ListAsync(cancellationToken);
    }

    public async Task<BackgroundJob?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default)
    {
        return await _backgroundJobRepository.FirstOrDefaultAsync(
            new QueuedBackgroundJobSpecification(_backgroundJobCollectionService.ActiveJobs),
            cancellationToken);
    }

    public async Task<List<BackgroundJob>> AbandonExpiredJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _backgroundJobRepository.ListAsync(
            new ExpiredBackgroundJobSpecification(_backgroundJobCollectionService.ActiveJobs),
            cancellationToken);

        foreach (var job in jobs)
        {
            job.Abandon();
        }

        _backgroundJobRepository.UpdateRange(jobs);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return jobs;
    }

    public async Task UpdateJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        _backgroundJobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        _backgroundJobRepository.Remove(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
