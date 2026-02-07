using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Tests.Domain.Aggregates
{
    public class SensorAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidData_ShouldSucceed()
        {
            var result = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0);

            result.IsSuccess.ShouldBeTrue();
            result.Value.SensorId.ShouldBe("SENSOR-001");
            result.Value.PlotName.ShouldBe("Plot Alpha");
            result.Value.Battery.ShouldBe(95.0);
            result.Value.Status.IsOnline.ShouldBeTrue();
            result.Value.IsActive.ShouldBeTrue();
            result.Value.UncommittedEvents.Count.ShouldBe(1);
        }

        #endregion

        #region Create - Invalid Cases

        [Fact]
        public void Create_WithEmptySensorId_ShouldFail()
        {
            var result = SensorAggregate.Create(
                sensorId: "",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorId.Required");
        }

        [Fact]
        public void Create_WithTooLongSensorId_ShouldFail()
        {
            var result = SensorAggregate.Create(
                sensorId: new string('A', 101),
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorId.TooLong");
        }

        [Fact]
        public void Create_WithEmptyPlotId_ShouldFail()
        {
            var result = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.Empty,
                plotName: "Plot Alpha",
                battery: 95.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "PlotId.Required");
        }

        [Fact]
        public void Create_WithEmptyPlotName_ShouldFail()
        {
            var result = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "",
                battery: 95.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "PlotName.Required");
        }

        [Fact]
        public void Create_WithInvalidBattery_ShouldFail()
        {
            var result = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 150.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Battery.OutOfRange");
        }

        #endregion

        #region UpdateLastReading

        [Fact]
        public void UpdateLastReading_ShouldUpdateValues()
        {
            var sensor = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0).Value;

            var readingTime = DateTimeOffset.UtcNow;
            sensor.UpdateLastReading(28.5, 65.0, 42.0, 90.0, readingTime);

            sensor.LastTemperature.ShouldBe(28.5);
            sensor.LastHumidity.ShouldBe(65.0);
            sensor.LastSoilMoisture.ShouldBe(42.0);
            sensor.Battery.ShouldBe(90.0);
            sensor.LastReadingAt.ShouldBe(readingTime);
        }

        #endregion

        #region UpdateStatus

        [Fact]
        public void UpdateStatus_WithValidStatus_ShouldChangeStatus()
        {
            var sensor = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0).Value;

            sensor.UpdateStatus("Warning");

            sensor.Status.IsWarning.ShouldBeTrue();
        }

        [Fact]
        public void UpdateStatus_WithDifferentStatus_ShouldAddDomainEvent()
        {
            var sensor = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0).Value;

            var initialEventCount = sensor.UncommittedEvents.Count;
            sensor.UpdateStatus("Offline");

            sensor.UncommittedEvents.Count.ShouldBe(initialEventCount + 1);
        }

        #endregion

        #region Deactivate

        [Fact]
        public void Deactivate_ShouldSetInactiveAndOffline()
        {
            var sensor = SensorAggregate.Create(
                sensorId: "SENSOR-001",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0).Value;

            sensor.Deactivate();

            sensor.IsActive.ShouldBeFalse();
            sensor.Status.IsOffline.ShouldBeTrue();
        }

        #endregion
    }
}
