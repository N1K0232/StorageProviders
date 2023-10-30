using Microsoft.Extensions.Configuration;
using StorageProviders;
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

        services.AddScoped<IStorageProvider, AzureStorageProvider>();
        return services;
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection services,
        Action<AzureStorageSettings> configuration,
        ServiceLifetime storageProviderSettingsLifetime = ServiceLifetime.Scoped,
        ServiceLifetime storageProviderLifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var azureStorageSettings = new AzureStorageSettings();
        configuration.Invoke(azureStorageSettings);

        services.AddAzureStorageSettings(azureStorageSettings, storageProviderSettingsLifetime);
        services.AddAzureStorageCore(storageProviderLifetime);

        return services;
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "AzureStorageSettings",
        ServiceLifetime storageProviderSettingsLifetime = ServiceLifetime.Scoped,
        ServiceLifetime storageProviderLifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName), "the section is required");
        }

        IConfigurationSection section = configuration.GetSection(sectionName);
        AzureStorageSettings azureStorageSettings = section.Get<AzureStorageSettings>() ?? throw new InvalidOperationException("settings are required");

        services.AddAzureStorageSettings(azureStorageSettings, storageProviderSettingsLifetime);
        services.AddAzureStorageCore(storageProviderLifetime);

        return services;
    }

    private static IServiceCollection AddAzureStorageSettings(this IServiceCollection services, AzureStorageSettings azureStorageSettings, ServiceLifetime storageProviderSettingsLifetime)
    {
        switch (storageProviderSettingsLifetime)
        {
            case ServiceLifetime.Scoped:
                services.AddScoped(_ => azureStorageSettings);
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton(azureStorageSettings);
                break;
            case ServiceLifetime.Transient:
                services.AddTransient(_ => azureStorageSettings);
                break;
        }

        return services;
    }

    private static IServiceCollection AddAzureStorageCore(this IServiceCollection services, ServiceLifetime storageProviderLifetime)
    {
        Type storageProviderType = typeof(IStorageProvider);
        Type azureStorageProviderType = typeof(AzureStorageProvider);

        var storageProviderService = new ServiceDescriptor(storageProviderType, azureStorageProviderType, storageProviderLifetime);
        services.Add(storageProviderService);

        return services;
    }
}