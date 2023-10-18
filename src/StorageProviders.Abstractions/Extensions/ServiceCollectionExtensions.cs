using Microsoft.Extensions.DependencyInjection;

namespace StorageProviders.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageProvider<TStorage>(this IServiceCollection services)
        where TStorage : class, IStorageProvider
    {
        services.AddMemoryCache();
        services.AddSingleton<IStorageCache, StorageCache>();

        services.AddScoped<IStorageProvider, TStorage>();
        return services;
    }
}