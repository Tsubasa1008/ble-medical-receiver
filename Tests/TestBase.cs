using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using BLEDataReceiver.Configuration;

namespace BLEDataReceiver.Tests
{
    /// <summary>
    /// 測試基類，提供通用的測試設置
    /// </summary>
    [TestFixture]
    public abstract class TestBase
    {
        protected IServiceProvider ServiceProvider { get; private set; } = null!;
        protected ILogger Logger { get; private set; } = null!;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // 配置測試服務容器
            var services = new ServiceCollection();
            services.ConfigureServices();
            
            // 配置測試日誌
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            ServiceProvider = services.BuildServiceProvider();
            Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [SetUp]
        public virtual void SetUp()
        {
            Logger.LogInformation("Starting test: {TestName}", TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        public virtual void TearDown()
        {
            Logger.LogInformation("Completed test: {TestName}", TestContext.CurrentContext.Test.Name);
        }
    }
}