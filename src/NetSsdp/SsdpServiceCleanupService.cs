using System;
using System.Collections.Concurrent;
using Kreisverkehr.NetSsdp;
using Microsoft.Extensions.Hosting;

namespace NetSsdp;

public class SsdpServiceCleanupService : BackgroundService
{
    private readonly ConcurrentDictionary<string, SsdpService> _ssdpServices;

    internal SsdpServiceCleanupService(IInternalSsdpServiceCollection serviceCollection)
    {
        _ssdpServices = serviceCollection.InternalCollection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var service in _ssdpServices.Values)
            {
                if (service.Status == SsdpServiceStatus.Dead)
                    _ssdpServices.TryRemove(service.UniqueServiceName, out _);
            }
        }
    }
}
