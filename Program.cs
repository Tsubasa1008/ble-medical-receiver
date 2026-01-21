using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using BLEDataReceiver.Configuration;
using BLEDataReceiver.Interfaces;

namespace BLEDataReceiver
{
    /// <summary>
    /// 主程式入口點
    /// </summary>
    class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Starting BLE Data Receiver...");
            
            // 配置日誌
            try
            {
                ServiceConfiguration.ConfigureLogging();
                Console.WriteLine("Logging configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure logging: {ex.Message}");
                return;
            }

            try
            {
                Log.Information("BLE Data Receiver starting...");
                Console.WriteLine("Creating host builder...");

                // 建立主機
                var host = CreateHostBuilder(args).Build();
                Console.WriteLine("Host created successfully");

                // 獲取服務
                var consoleInterface = host.Services.GetRequiredService<IConsoleInterface>();
                var bleReceiver = host.Services.GetRequiredService<IBLEReceiver>();
                Console.WriteLine("Services resolved successfully");

                // 顯示歡迎信息
                await consoleInterface.DisplayWelcomeAsync();

                // 設置取消令牌
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Log.Information("Shutdown requested by user");
                };

                Console.WriteLine("Starting BLE receiver...");
                // 啟動BLE接收器
                await bleReceiver.StartAsync(cts.Token);
                Console.WriteLine("BLE receiver started successfully");

                // 啟動交互式控制台
                _ = Task.Run(() => InteractiveConsoleAsync(bleReceiver, cts.Token));

                Console.WriteLine("Press Ctrl+C to exit, or type 'help' for commands...");
                // 等待取消信號
                try
                {
                    await Task.Delay(Timeout.Infinite, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Application shutdown initiated");
                }

                // 停止BLE接收器
                await bleReceiver.StopAsync();
                await consoleInterface.DisplayStatusAsync("Application stopped");

                Log.Information("BLE Data Receiver stopped successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Log.Fatal(ex, "Application terminated unexpectedly");
                await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// 創建主機建構器
        /// </summary>
        /// <param name="args">命令行參數</param>
        /// <returns>主機建構器</returns>
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.ConfigureServices();
                });

        /// <summary>
        /// 交互式控制台
        /// </summary>
        private static async Task InteractiveConsoleAsync(IBLEReceiver bleReceiver, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write("\nBLE> ");
                    var input = Console.ReadLine()?.Trim().ToLowerInvariant();
                    
                    if (string.IsNullOrEmpty(input))
                        continue;

                    switch (input)
                    {
                        case "help":
                        case "h":
                            Console.WriteLine("\n可用命令:");
                            Console.WriteLine("  help, h          - 顯示此幫助信息");
                            Console.WriteLine("  status, s        - 顯示連接狀態");
                            Console.WriteLine("  disconnect, d    - 斷開所有設備連接");
                            Console.WriteLine("  clear, c         - 清除螢幕");
                            Console.WriteLine("  exit, quit, q    - 退出程序");
                            break;

                        case "status":
                        case "s":
                            var devices = bleReceiver.GetConnectedDevices();
                            if (devices.Count == 0)
                            {
                                Console.WriteLine("目前沒有連接的設備");
                                Console.WriteLine("\n💡 Windows BLE 提示:");
                                Console.WriteLine("   - Windows 的藍牙斷線檢測比 iOS(1秒) 和 Android(3秒) 慢很多");
                                Console.WriteLine("   - 如果設備無回應，請使用 'disconnect' 命令手動清理連接");
                            }
                            else
                            {
                                Console.WriteLine($"\n目前連接的設備 ({devices.Count}):");
                                foreach (var device in devices)
                                {
                                    Console.WriteLine($"  - {device.DeviceName} (ID: {device.DeviceId:X}, 類型: {device.DeviceType}, 訂閱: {device.ActiveSubscriptions})");
                                }
                                Console.WriteLine("\n💡 提示: 如果設備已斷開但仍顯示連接，請使用 'disconnect' 命令");
                            }
                            break;

                        case "disconnect":
                        case "d":
                            Console.WriteLine("正在斷開所有設備連接...");
                            await bleReceiver.DisconnectAllDevicesAsync();
                            Console.WriteLine("所有設備已斷開連接");
                            break;

                        case "clear":
                        case "c":
                            Console.Clear();
                            Console.WriteLine("BLE Data Receiver - 交互式控制台");
                            Console.WriteLine("輸入 'help' 查看可用命令");
                            break;

                        case "exit":
                        case "quit":
                        case "q":
                            Console.WriteLine("正在退出程序...");
                            Environment.Exit(0);
                            break;

                        default:
                            Console.WriteLine($"未知命令: {input}. 輸入 'help' 查看可用命令");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in interactive console");
            }
        }
    }
}