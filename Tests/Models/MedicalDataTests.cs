using System;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Tests.Models
{
    /// <summary>
    /// 醫療數據模型測試
    /// </summary>
    [TestFixture]
    public class MedicalDataTests : TestBase
    {
        [Test]
        public void BloodPressureData_IsHypertensive_ShouldReturnTrueForHighValues()
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = 150,
                DiastolicPressure = 95,
                HeartRate = 80,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor,
                IsValid = true
            };

            // Act & Assert
            Assert.That(data.IsHypertensive, Is.True);
        }

        [Test]
        public void BloodPressureData_IsHypertensive_ShouldReturnFalseForNormalValues()
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = 120,
                DiastolicPressure = 80,
                HeartRate = 70,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor,
                IsValid = true
            };

            // Act & Assert
            Assert.That(data.IsHypertensive, Is.False);
        }

        [Test]
        public void TemperatureData_IsFever_ShouldReturnTrueForHighTemperature()
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = 38.5f,
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer,
                IsValid = true
            };

            // Act & Assert
            Assert.That(data.IsFever, Is.True);
        }

        [Test]
        public void TemperatureData_IsFever_ShouldReturnFalseForNormalTemperature()
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = 36.5f,
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer,
                IsValid = true
            };

            // Act & Assert
            Assert.That(data.IsFever, Is.False);
        }

        [FsCheck.NUnit.Property(Verbose = false, QuietOnSuccess = true)]
        public void BloodPressureData_IsHypertensive_PropertyTest(float systolic, float diastolic)
        {
            // Arrange
            var data = new BloodPressureData
            {
                SystolicPressure = systolic,
                DiastolicPressure = diastolic,
                HeartRate = 70,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.BloodPressureMonitor,
                IsValid = true
            };

            // Act & Assert
            var expectedHypertensive = systolic > 140 || diastolic > 90;
            Assert.That(data.IsHypertensive, Is.EqualTo(expectedHypertensive));
        }

        [FsCheck.NUnit.Property(Verbose = false, QuietOnSuccess = true)]
        public void TemperatureData_IsFever_PropertyTest(float temperature)
        {
            // Arrange
            var data = new TemperatureData
            {
                Temperature = temperature,
                Unit = TemperatureUnit.Celsius,
                Timestamp = DateTime.Now,
                DeviceId = "test-device",
                DeviceType = DeviceType.Thermometer,
                IsValid = true
            };

            // Act & Assert
            var expectedFever = temperature > 37.5f;
            Assert.That(data.IsFever, Is.EqualTo(expectedFever));
        }
    }
}