using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StorageProviders.Abstractions.Extensions;

namespace StorageProviders.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection services, Action<IServiceProvider, AzureStorageSettings> configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        services.AddScoped(provider =>
        {
            var azureStorageSettings = new AzureStorageSettings();
            configuration.Invoke(provider, azureStorageSettings);

            return azureStorageSettings;
        });

        services.AddStorageProvider<AzureStorageProvider>();
        return services;
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection services, Action<AzureStorageSettings> configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var azureStorageSettings = new AzureStorageSettings();
        configuration.Invoke(azureStorageSettings);

        services.AddSingleton(azureStorageSettings);
        services.AddStorageProvider<AzureStorageProvider>();

        return services;
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection services, IConfiguration configuration, string sectionName = "AzureStorageSettings")
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName), "the section can't be null");
        }

        var azureStorageSection = configuration.GetSection(sectionName);
        var azureStorageSettings = azureStorageSection.Get<AzureStorageSettings>();

        services.AddSingleton(azureStorageSettings ?? throw new InvalidOperationException("settings are required"));
        services.AddStorageProvider<AzureStorageProvider>();

        return services;
    }
}