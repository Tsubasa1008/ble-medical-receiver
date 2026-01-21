using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Services;

namespace BLEDataReceiver.Configuration
{
    /// <summary>
    /// 服務配置類
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// 配置服務容器
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <returns>配置後的服務集合</returns>
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            // 註冊核心服務接口（實現類將在後續任務中創建）
            services.AddSingleton<IBLEReceiver, BLEReceiverService>();
            services.AddSingleton<IPairingManager, PairingManagerService>();
            services.AddSingleton<IConnectionManager, ConnectionManagerService>();
            services.AddSingleton<IDataProcessor, DataProcessorService>();
            services.AddSingleton<IConsoleInterface, ConsoleInterfaceService>();

            // 配置日誌
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            return services;
        }

        /// <summary>
        /// 配置Serilog日誌
        /// </summary>
        public static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/ble-receiver-.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
    }
}