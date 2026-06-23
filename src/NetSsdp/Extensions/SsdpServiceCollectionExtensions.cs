using Kreisverkehr.NetSsdp;
using Kreisverkehr.NetSsdp.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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

    public static IServiceCollection AddSsdp(this IServiceCollection services, Action<SsdpOptions>? configure) => services
        .AddSsdp()
        .AddSsdpOptions()
            .Configure(configure ?? (_ => { })).Services
    ;

    public static IServiceCollection AddSsdp(this IServiceCollection services, Action<SsdpOptions, IServiceProvider>? configure) => services
        .AddSsdp()
        .AddSsdpOptions()
            .Configure(configure ?? ((_, _) => { })).Services
    ;

    public static IServiceCollection AddSsdp(this IServiceCollection services, IConfiguration configuration) => services
        .AddSsdp()
        .AddSsdpOptions()
            .Bind(configuration)
            .ValidateOnStart()
            .Services
    ;

    private static OptionsBuilder<SsdpOptions> AddSsdpOptions(this IServiceCollection services) =>
        services.AddOptions<SsdpOptions>()
            .Validate(options => options.UseIPv4 || options.UseIPv6, "At least one of UseIPv4 or UseIPv6 must be enabled.");

}