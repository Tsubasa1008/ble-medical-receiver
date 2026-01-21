using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Services
{
    /// <summary>
    /// 控制台界面服務實現（佔位符，將在後續任務中完整實現）
    /// </summary>
    public class ConsoleInterfaceService : IConsoleInterface
    {
        private readonly ILogger<ConsoleInterfaceService> _logger;

        public ConsoleInterfaceService(ILogger<ConsoleInterfaceService> logger)
        {
            _logger = logger;
        }

        public Task DisplayWelcomeAsync()
        {
            Console.WriteLine("=== BLE Data Receiver ===");
            Console.WriteLine("Initializing...");
            _logger.LogInformation("Welcome message displayed");
            // 完整實現將在後續任務中添加
            return Task.CompletedTask;
        }

        public Task DisplayDataAsync(MedicalData data)
        {
            Console.WriteLine($"[{data.Timestamp:HH:mm:ss}] Data received from {data.DeviceType}");
            _logger.LogInformation("Data displayed: {DeviceType}", data.DeviceType);
            // 完整實現將在後續任務中添加
            return Task.CompletedTask;
        }

        public Task DisplayStatusAsync(string status)
        {
            Console.WriteLine($"Status: {status}");
            _logger.LogInformation("Status displayed: {Status}", status);
            return Task.CompletedTask;
        }

        public Task DisplayErrorAsync(string error)
        {
            Console.WriteLine($"Error: {error}");
            _logger.LogError("Error displayed: {Error}", error);
            return Task.CompletedTask;
        }

        public Task HandleUserInputAsync(CancellationToken cancellationToken)
        {
            // 完整實現將在後續任務中添加
            return Task.CompletedTask;
        }
    }
}