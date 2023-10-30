using Microsoft.Extensions.Configuration;
using StorageProviders;
using StorageProviders.FileSystem;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemStorage(this IServiceCollection services,
        Action<FileSystemStorageSettings> configuration,
        ServiceLifetime storageProviderSettingsLifetime = ServiceLifetime.Scoped,
        ServiceLifetime storageProviderLifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var fileSystemStorageSettings = new FileSystemStorageSettings();
        configuration.Invoke(fileSystemStorageSettings);

        services.AddFileSystemStorageSettings(fileSystemStorageSettings, storageProviderSettingsLifetime);
        services.AddFileSystemStorageCore(storageProviderLifetime);

        return services;
    }

    public static IServiceCollection AddFileSystemStorage(this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "FileSystemStorageSettings",
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
        FileSystemStorageSettings fileSystemStorageSettings = section.Get<FileSystemStorageSettings>() ?? throw new InvalidOperationException("settings are required");

        services.AddFileSystemStorageSettings(fileSystemStorageSettings, storageProviderSettingsLifetime);
        services.AddFileSystemStorageCore(storageProviderLifetime);

        return services;
    }

    private static IServiceCollection AddFileSystemStorageSettings(this IServiceCollection services, FileSystemStorageSettings fileSystemStorageSettings, ServiceLifetime storageProviderSettingsLifetime)
    {
        switch (storageProviderSettingsLifetime)
        {
            case ServiceLifetime.Scoped:
                services.AddScoped(_ => fileSystemStorageSettings);
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton(fileSystemStorageSettings);
                break;
            case ServiceLifetime.Transient:
                services.AddTransient(_ => fileSystemStorageSettings);
                break;
        }

        return services;
    }

    private static IServiceCollection AddFileSystemStorageCore(this IServiceCollection services, ServiceLifetime storageProviderLifetime)
    {
        Type storageProviderType = typeof(IStorageProvider);
        Type fileSystemStorageProviderType = typeof(FileSystemStorageProvider);

        var storageProviderService = new ServiceDescriptor(storageProviderType, fileSystemStorageProviderType, storageProviderLifetime);
        services.Add(storageProviderService);

        return services;
    }
}