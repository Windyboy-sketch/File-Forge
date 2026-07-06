using FileConverter.Core.Interfaces;
using FileConverter.Infrastructure.Converters;
using FileConverter.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace FileConverter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<Ffmpeg.FfmpegLocator>();
        services.AddSingleton<IConversionCoordinator, Services.ConversionCoordinator>();
        services.AddSingleton<IConversionQueueService, Services.ConversionQueueService>();

        services.AddSingleton<IConversionService, ImageConversionService>();
        services.AddSingleton<IConversionService, AudioConversionService>();
        services.AddSingleton<IConversionService, VideoConversionService>();
        services.AddSingleton<IConversionService, DocumentConversionService>();

        return services;
    }
}
