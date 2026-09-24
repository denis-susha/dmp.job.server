namespace DMP.BL.Services;

/// <summary>
/// A unit of work executed by a Hangfire recurring job.
/// </summary>
public interface IJobService
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
