namespace DMP.BL.Services;

// The interface type (not the implementation) is serialized into Hangfire storage for the recurring job,
// so its name, namespace and assembly must stay stable.
public interface IMailService : IJobService;
