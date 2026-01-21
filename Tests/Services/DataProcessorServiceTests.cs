using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;
using BLEDataReceiver.Services;

namespace BLEDataReceiver.Tests.Services
{
    /// <summary>
    /// 數據處理器服務測試
    /// </summary>
    [TestFixture]
    public class DataProcessorServiceTests : TestBase
    {
        private IDataProcessor _dataProcessor = null!;
        private ILogger<DataProcessorService> _logger = null!;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _logger = ServiceProvider.GetRequiredService<ILogger<DataProcessorService>>();
            _dataProcessor = new DataProcessorService(_logger);
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DataProcessorService(null!));
        }

        [Test]
        public void ValidateData_WithNullData_ShouldReturnFalse()
        {
            // Act
            var result = _dataProcessor.ValidateData(null!);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateData_WithValidBloodPressureData_ShouldReturnTrue()
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = 120,
                DiastolicPressure = 80,
                HeartRate = 70,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateData_WithInvalidBloodPressureData_ShouldReturnFalse()
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = 400, // 無效的高血壓值
                DiastolicPressure = 80,
                HeartRate = 70,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateData_WithValidTemperatureData_ShouldReturnTrue()
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = 36.5f,
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateData_WithInvalidTemperatureData_ShouldReturnFalse()
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = 60.0f, // 無效的高溫值
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void FormatDataForDisplay_WithValidBloodPressureData_ShouldReturnFormattedString()
        {
            // Arrange
            var service = _dataProcessor as DataProcessorService;
            var data = new BloodPressureData
            {
                SystolicPressure = 120,
                DiastolicPressure = 80,
                HeartRate = 70,
                Timestamp = new DateTime(2024, 1, 1, 12, 0, 0),
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor,
                IsValid = true
            };

            // Act
            var result = service!.FormatDataForDisplay(data);

            // Assert
            Assert.That(result, Does.Contain("血壓: 120.0/80.0 mmHg"));
            Assert.That(result, Does.Contain("心率: 70 bpm"));
            Assert.That(result, Does.Contain("✓"));
        }

        [Test]
        public void FormatDataForDisplay_WithValidTemperatureData_ShouldReturnFormattedString()
        {
            // Arrange
            var service = _dataProcessor as DataProcessorService;
            var data = new TemperatureData
            {
                Temperature = 36.5f,
                Unit = TemperatureUnit.Celsius,
                Timestamp = new DateTime(2024, 1, 1, 12, 0, 0),
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer,
                IsValid = true
            };

            // Act
            var result = service!.FormatDataForDisplay(data);

            // Assert
            Assert.That(result, Does.Contain("體溫: 36.5°Celsius"));
            Assert.That(result, Does.Contain("✓"));
        }

        [Test]
        public void FormatDataForDisplay_WithAbnormalValues_ShouldShowWarning()
        {
            // Arrange
            var service = _dataProcessor as DataProcessorService;
            var data = new BloodPressureData
            {
                SystolicPressure = 160, // 高血壓
                DiastolicPressure = 100, // 高血壓
                HeartRate = 70,
                Timestamp = new DateTime(2024, 1, 1, 12, 0, 0),
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor,
                IsValid = true
            };

            // Act
            var result = service!.FormatDataForDisplay(data);

            // Assert
            Assert.That(result, Does.Contain("⚠️"));
        }

        [Test]
        public void FormatDataForDisplay_WithNullData_ShouldReturnErrorMessage()
        {
            // Arrange
            var service = _dataProcessor as DataProcessorService;

            // Act
            var result = service!.FormatDataForDisplay(null!);

            // Assert
            Assert.That(result, Is.EqualTo("無效數據"));
        }

        [Test]
        public void DataProcessed_Event_ShouldBeSubscribable()
        {
            // Arrange
            var eventTriggered = false;
            DataProcessedEventArgs? eventArgs = null;
            
            // Act - Subscribe to the event
            _dataProcessor.DataProcessed += (sender, args) =>
            {
                eventTriggered = true;
                eventArgs = args;
            };

            // Assert - Verify event subscription works without errors
            Assert.That(_dataProcessor, Is.Not.Null);
            // Note: The actual event triggering happens in ProcessDataAsync method
        }

        // Property-based tests
        [FsCheck.NUnit.Property(Verbose = false, QuietOnSuccess = true, MaxTest = 100)]
        public void ValidateData_BloodPressureData_PropertyTest(float systolic, float diastolic, int heartRate)
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = systolic,
                DiastolicPressure = diastolic,
                HeartRate = heartRate,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert - The result should match the internal validation logic
            var expectedValid = data.ValidateData();
            Assert.That(result, Is.EqualTo(expectedValid));
        }

        [FsCheck.NUnit.Property(Verbose = false, QuietOnSuccess = true, MaxTest = 100)]
        public void ValidateData_TemperatureData_PropertyTest(float temperature)
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = temperature,
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert - The result should match the internal validation logic
            var expectedValid = data.ValidateData();
            Assert.That(result, Is.EqualTo(expectedValid));
        }

        // Generator for valid blood pressure values
        public static Arbitrary<(float systolic, float diastolic, int heartRate)> ValidBloodPressureValues()
        {
            return Arb.From(
                from systolic in Gen.Choose(80, 200).Select(x => (float)x)
                from diastolic in Gen.Choose(50, 120).Select(x => (float)x)
                from heartRate in Gen.Choose(40, 180)
                where systolic > diastolic
                select (systolic, diastolic, heartRate)
            );
        }

        // Generator for valid temperature values
        public static Arbitrary<float> ValidTemperatureValues()
        {
            return Arb.From(Gen.Choose(300, 420).Select(x => x / 10.0f)); // 30.0 to 42.0 Celsius
        }

        [FsCheck.NUnit.Property(Verbose = false, QuietOnSuccess = true, MaxTest = 100, Arbitrary = new[] { typeof(DataProcessorServiceTests) })]
        public void ValidateData_WithValidBloodPressureValues_ShouldReturnTrue((float systolic, float diastolic, int heartRate) values)
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = values.systolic,
                DiastolicPressure = values.diastolic,
                HeartRate = values.heartRate,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert
            Assert.That(result, Is.True);
        }

        [FsCheck.NUnit.Property(Verbose = false, QuietOnSuccess = true, MaxTest = 100, Arbitrary = new[] { typeof(DataProcessorServiceTests) })]
        public void ValidateData_WithValidTemperatureValues_ShouldReturnTrue(float temperature)
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = temperature,
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer
            };

            // Act
            var result = _dataProcessor.ValidateData(data);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}