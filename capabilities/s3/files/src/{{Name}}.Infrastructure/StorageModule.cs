using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using {{Name}}.Application.Storage;
using {{Name}}.Infrastructure.Storage;

namespace {{Name}}.Infrastructure;

internal static class StorageModule
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<StorageOptions>(config.GetSection("Storage"));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var s3 = new AmazonS3Config
            {
                ServiceURL = opts.Endpoint,
                ForcePathStyle = true, // required for MinIO
            };
            return new AmazonS3Client(opts.AccessKey, opts.SecretKey, s3);
        });

        services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        return services;
    }
}
