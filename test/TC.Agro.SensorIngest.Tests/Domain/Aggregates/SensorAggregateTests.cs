using TC.Agro.SensorIngest.Domain.Aggregates;

namespace TC.Agro.SensorIngest.Tests.Domain.Aggregates
{
    public class SensorAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidData_ShouldSucceed()
        {
            var sensorid = Guid.Parse("5783a03e-be56-4d2c-8fad-7e8166f067ca");

            var result = SensorAggregate.Create(
                sensorId: sensorid,
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0);

            result.IsSuccess.ShouldBeTrue();
            result.Value.SensorId.ShouldBe(sensorid);
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
                sensorId: Guid.Empty,
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                battery: 95.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorId.Required");
        }

        [Fact]
        public void Create_WithTooLongPlotName_ShouldFail()
        {
            var plotName = new string('A', 201);

            var result = SensorAggregate.Create(
                sensorId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                plotName: plotName,
                battery: 95.0);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "PlotName.TooLong");
        }

        [Fact]
        public void Create_WithEmptyPlotId_ShouldFail()
        {
            var result = SensorAggregate.Create(
                sensorId: Guid.NewGuid(),
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
                sensorId: Guid.NewGuid(),
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
                sensorId: Guid.NewGuid(),
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
                sensorId: Guid.NewGuid(),
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
                sensorId: Guid.NewGuid(),
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
                sensorId: Guid.NewGuid(),
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
                sensorId: Guid.NewGuid(),
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
