using Kreisverkehr.NetSsdp;

namespace Microsoft.Extensions.DependencyInjection;

public static class SsdpServiceCollectionExtensions
{
    public static IServiceCollection AddSsdp(this IServiceCollection services) => services
        .AddSingleton<ISsdpClient, SsdpClient>()
        .AddSingleton<ISsdpServiceCollection, SsdpServiceCollection>()
        .AddSingleton<IReadOnlyDictionary<string, SsdpService>>(provider => provider.GetRequiredService<ISsdpServiceCollection>())
        .AddSingleton<IReadOnlyCollection<SsdpService>>(provider => provider.GetRequiredService<ISsdpServiceCollection>())
    ;
}
