using Microsoft.Extensions.Configuration;
using StorageProviders.FileSystem;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemStorage(this IServiceCollection services, Action<FileSystemStorageSettings> configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var fileSystemStorageSettings = new FileSystemStorageSettings();
        configuration.Invoke(fileSystemStorageSettings);

        services.AddSingleton(fileSystemStorageSettings);
        services.AddStorageProvider<FileSystemStorageProvider>();

        return services;
    }

    public static IServiceCollection AddFileSystemStorage(this IServiceCollection services, IConfiguration configuration, string sectionName = "FileSystemStorageSettings")
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName), "the section is required");
        }

        IConfigurationSection section = configuration.GetSection(sectionName);
        FileSystemStorageSettings? fileSystemStorageSettings = section.Get<FileSystemStorageSettings>();

        services.AddSingleton(fileSystemStorageSettings ?? throw new InvalidOperationException("settings are required"));
        services.AddStorageProvider<FileSystemStorageProvider>();

        return services;
    }
}