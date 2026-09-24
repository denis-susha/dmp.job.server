using System.Collections.Concurrent;
using Hangfire.Server;

namespace Job.Server;

/// <summary>
/// Global Hangfire server filter that skips a job when another job of the same type is still running
/// in this process. Recurring jobs fire every minute and may run longer than that; overlapping runs are
/// cancelled instead of queued so they do not pile up.
/// </summary>
/// <remarks>
/// The guard is in-process only (keyed by the job type name), so it assumes a single job server instance.
/// </remarks>
public sealed class DisableConcurrentExecutionFilter : IServerFilter
{
    private static readonly ConcurrentDictionary<string, byte> _runningJobs = new();

    public void OnPerforming(PerformingContext context)
    {
        if (!_runningJobs.TryAdd(GetKey(context), 0))
        {
            context.Canceled = true;
        }
    }

    // Hangfire does not call OnPerformed on the filter that cancelled the job, so this only
    // releases the key held by the run that actually executed.
    public void OnPerformed(PerformedContext context) => _runningJobs.TryRemove(GetKey(context), out _);

    private static string GetKey(PerformContext context) => context.BackgroundJob.Job.Type.Name;
}
