using Kreisverkehr.NetSsdp;

namespace Microsoft.Extensions.DependencyInjection;

public static class SsdpServiceCollectionExtensions
{
    public static IServiceCollection AddSsdp(this IServiceCollection services) => services
        .AddSingleton<ISsdpClient, SsdpClient>()
        .AddSingleton<IInternalSsdpServiceCollection, SsdpServiceCollection>()
        .AddSingleton<ISsdpServiceCollection>(provider => provider.GetRequiredService<IInternalSsdpServiceCollection>())
        .AddSingleton<IReadOnlyDictionary<string, SsdpService>>(provider => provider.GetRequiredService<IInternalSsdpServiceCollection>())
        .AddSingleton<IReadOnlyCollection<SsdpService>>(provider => provider.GetRequiredService<IInternalSsdpServiceCollection>())
        .AddHostedService<SsdpServiceCleanupService>()
    ;
}
