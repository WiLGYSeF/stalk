﻿using Wilgysef.Stalk.Core.Models;

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

    public async Task<BackgroundJob> EnqueueJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BackgroundJobRepository.AddAsync(job, cancellationToken);
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
        var queuedJobs = await _unitOfWork.BackgroundJobRepository.ListAsync(
            new BackgroundJobQuerySpecification(_backgroundJobDispatcher.ActiveJobs, job),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var jobArgs = job.DeserializeArgs();

        BackgroundJob? existingJob = null;

        foreach (var queuedJob in queuedJobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var args = queuedJob.DeserializeArgs();
            if (compareTo(jobArgs, args))
            {
                existingJob = queuedJob;
                break;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (existingJob != null)
        {
            _unitOfWork.BackgroundJobRepository.Remove(existingJob);
        }

        return await EnqueueJobAsync(job, saveChanges, cancellationToken);
    }

    public async Task<BackgroundJob?> FindJob(long id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.BackgroundJobRepository.FindAsync(new object?[] { id }, cancellationToken: cancellationToken);
    }

    public async Task<BackgroundJob?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.BackgroundJobRepository.FirstOrDefaultAsync(
            new QueuedBackgroundJobSpecification(_backgroundJobDispatcher.ActiveJobs),
            cancellationToken);
    }

    public async Task UpdateJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        _unitOfWork.BackgroundJobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteJobAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        _unitOfWork.BackgroundJobRepository.Remove(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
