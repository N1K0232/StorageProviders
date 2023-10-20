using Microsoft.Extensions.Configuration;
using StorageProviders.Azure;

namespace Microsoft.Extensions.DependencyInjection;

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
            throw new ArgumentNullException(nameof(sectionName), "the section is required");
        }

        IConfigurationSection section = configuration.GetSection(sectionName);
        AzureStorageSettings? azureStorageSettings = section.Get<AzureStorageSettings>();

        services.AddSingleton(azureStorageSettings ?? throw new InvalidOperationException("settings are required"));
        services.AddStorageProvider<AzureStorageProvider>();

        return services;
    }
}