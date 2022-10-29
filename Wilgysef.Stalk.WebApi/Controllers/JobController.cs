using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Contracts.Queries.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/job")]
[ApiController]
public class JobController : ControllerBase
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<UpdateJob, JobDto> _updateJobCommandHandler;
    private readonly ICommandHandler<StopJob, JobDto> _stopJobCommandHandler;
    private readonly ICommandHandler<DeleteJob, JobDto> _deleteJobCommandHandler;
    private readonly ICommandHandler<PauseJob, JobDto> _pauseJobCommandHandler;
    private readonly ICommandHandler<UnpauseJob, JobDto> _unpauseJobCommandHandler;

    private readonly IQueryHandler<GetJob, JobDto> _getJobCommandHandler;
    private readonly IQueryHandler<GetJobs, JobListDto> _getJobsCommandHandler;

    private readonly ICommandHandler<CreateJobTask, JobDto> _createJobTaskDtoCommandHandler;
    private readonly ICommandHandler<StopJobTask, JobDto> _stopJobTaskCommandHandler;
    private readonly ICommandHandler<DeleteJobTask, JobDto> _deleteJobTaskCommandHandler;
    private readonly ICommandHandler<PauseJobTask, JobDto> _pauseJobTaskCommandHandler;
    private readonly ICommandHandler<UnpauseJobTask, JobDto> _unpauseJobTaskCommandHandler;

    public JobController(
        ICommandHandler<CreateJob, JobDto> createJobCommandHandler,
        ICommandHandler<UpdateJob, JobDto> updateJobCommandHandler,
        ICommandHandler<StopJob, JobDto> stopJobCommandHandler,
        ICommandHandler<DeleteJob, JobDto> deleteJobCommandHandler,
        ICommandHandler<PauseJob, JobDto> pauseJobCommandHandler,
        ICommandHandler<UnpauseJob, JobDto> unpauseJobCommandHandler,

        IQueryHandler<GetJob, JobDto> getJobCommandHandler,
        IQueryHandler<GetJobs, JobListDto> getJobsCommandHandler,

        ICommandHandler<CreateJobTask, JobDto> createJobTaskDtoCommandHandler,
        ICommandHandler<StopJobTask, JobDto> stopJobTaskCommandHandler,
        ICommandHandler<DeleteJobTask, JobDto> deleteJobTaskCommandHandler,
        ICommandHandler<PauseJobTask, JobDto> pauseJobTaskCommandHandler,
        ICommandHandler<UnpauseJobTask, JobDto> unpauseJobTaskCommandHandler)
    {
        _createJobCommandHandler = createJobCommandHandler;
        _updateJobCommandHandler = updateJobCommandHandler;
        _stopJobCommandHandler = stopJobCommandHandler;
        _deleteJobCommandHandler = deleteJobCommandHandler;
        _pauseJobCommandHandler = pauseJobCommandHandler;
        _unpauseJobCommandHandler = unpauseJobCommandHandler;

        _getJobCommandHandler = getJobCommandHandler;
        _getJobsCommandHandler = getJobsCommandHandler;

        _createJobTaskDtoCommandHandler = createJobTaskDtoCommandHandler;
        _stopJobTaskCommandHandler = stopJobTaskCommandHandler;
        _deleteJobTaskCommandHandler = deleteJobTaskCommandHandler;
        _pauseJobTaskCommandHandler = pauseJobTaskCommandHandler;
        _unpauseJobTaskCommandHandler = unpauseJobTaskCommandHandler;
    }

    #region Job Endpoints

    [HttpPost]
    public async Task<JobDto> CreateJobAsync(CreateJob command)
    {
        return await _createJobCommandHandler.HandleCommandAsync(command);
    }

    [HttpPatch]
    public async Task<JobDto> UpdateJobAsync(UpdateJob command)
    {
        return await _updateJobCommandHandler.HandleCommandAsync(command);
    }

    [HttpPost("{id}/stop")]
    public async Task<JobDto> StopJobAsync(long id)
    {
        return await _stopJobCommandHandler.HandleCommandAsync(new StopJob(id));
    }

    [HttpDelete("{id}")]
    public async Task<JobDto> DeleteJobAsync(long id)
    {
        return await _deleteJobCommandHandler.HandleCommandAsync(new DeleteJob(id));
    }

    [HttpPost("{id}/pause")]
    public async Task<JobDto> PauseJobAsync(long id)
    {
        return await _pauseJobCommandHandler.HandleCommandAsync(new PauseJob(id));
    }

    [HttpPost("{id}/unpause")]
    public async Task<JobDto> UnpauseJobAsync(long id)
    {
        return await _unpauseJobCommandHandler.HandleCommandAsync(new UnpauseJob(id));
    }

    [HttpGet("{id}")]
    public async Task<JobDto> GetJobAsync(long id)
    {
        return await _getJobCommandHandler.HandleQueryAsync(new GetJob(id));
    }

    [HttpPost("list")]
    public async Task<JobListDto> GetJobsAsync(GetJobs query)
    {
        return await _getJobsCommandHandler.HandleQueryAsync(query);
    }

    #endregion

    #region Job Tasks Endpoints

    [HttpPost("{jobId}/task")]
    public async Task<JobDto> CreateJobTaskAsync(long jobId, CreateJobTaskDto input)
    {
        var command = new CreateJobTask(
            jobId,
            input.Name,
            input.Priority,
            input.Uri,
            input.DelayedUntil);
        return await _createJobTaskDtoCommandHandler.HandleCommandAsync(command);
    }

    [HttpPost("{jobId}/task/{taskId}/stop")]
    public async Task<JobDto> StopJobTaskAsync(long jobId, long taskId)
    {
        return await _stopJobTaskCommandHandler.HandleCommandAsync(new StopJobTask(taskId));
    }

    [HttpDelete("{jobId}/task/{taskId}")]
    public async Task<JobDto> DeleteJobTaskAsync(long jobId, long taskId)
    {
        return await _deleteJobTaskCommandHandler.HandleCommandAsync(new DeleteJobTask(taskId));
    }

    [HttpPost("{jobId}/task/{taskId}/pause")]
    public async Task<JobDto> PauseJobTaskAsync(long jobId, long taskId)
    {
        return await _pauseJobTaskCommandHandler.HandleCommandAsync(new PauseJobTask(taskId));
    }

    [HttpPost("{jobId}/task/{taskId}/unpause")]
    public async Task<JobDto> UnpauseJobTaskAsync(long jobId, long taskId)
    {
        return await _unpauseJobTaskCommandHandler.HandleCommandAsync(new UnpauseJobTask(taskId));
    }

    #endregion
}
