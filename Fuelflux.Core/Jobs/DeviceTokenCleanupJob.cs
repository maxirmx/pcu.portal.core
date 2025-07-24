using Quartz;
using Fuelflux.Core.Services;

namespace Fuelflux.Core.Jobs;

public class DeviceTokenCleanupJob(IDeviceAuthService authService, ILogger<DeviceTokenCleanupJob> logger) : IJob
{
    private readonly IDeviceAuthService _authService = authService;
    private readonly ILogger<DeviceTokenCleanupJob> _logger = logger;

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Running device token cleanup job");
        _authService.RemoveExpiredTokens();
        return Task.CompletedTask;
    }
}
