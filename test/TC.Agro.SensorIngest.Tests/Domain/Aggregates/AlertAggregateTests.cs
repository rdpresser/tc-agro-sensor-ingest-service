using TC.Agro.SensorIngest.Domain.Aggregates;

namespace TC.Agro.SensorIngest.Tests.Domain.Aggregates
{
    public class AlertAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidData_ShouldSucceed()
        {
            var result = AlertAggregate.Create(
                severity: "Warning",
                title: "High Temperature",
                message: "Temperature exceeded 40C threshold",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid());

            result.IsSuccess.ShouldBeTrue();
            result.Value.Severity.Value.ShouldBe("Warning");
            result.Value.Title.ShouldBe("High Temperature");
            result.Value.Status.IsPending.ShouldBeTrue();
            result.Value.IsActive.ShouldBeTrue();
            result.Value.ResolvedAt.ShouldBeNull();
            result.Value.UncommittedEvents.Count.ShouldBe(1);
        }

        #endregion

        #region Create - Invalid Cases

        [Fact]
        public void Create_WithInvalidSeverity_ShouldFail()
        {
            var result = AlertAggregate.Create(
                severity: "InvalidSeverity",
                title: "Test",
                message: "Test message",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertSeverity.InvalidValue");
        }

        [Fact]
        public void Create_WithEmptyTitle_ShouldFail()
        {
            var result = AlertAggregate.Create(
                severity: "Warning",
                title: "",
                message: "Test message",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Title.Required");
        }

        [Fact]
        public void Create_WithEmptyMessage_ShouldFail()
        {
            var result = AlertAggregate.Create(
                severity: "Warning",
                title: "Test",
                message: "",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Message.Required");
        }

        [Fact]
        public void Create_WithEmptyPlotId_ShouldFail()
        {
            var result = AlertAggregate.Create(
                severity: "Warning",
                title: "Test",
                message: "Test message",
                plotId: Guid.Empty,
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "PlotId.Required");
        }

        [Fact]
        public void Create_WithEmptySensorId_ShouldFail()
        {
            var result = AlertAggregate.Create(
                severity: "Warning",
                title: "Test",
                message: "Test message",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.Empty);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorId.Required");
        }

        #endregion

        #region Resolve

        [Fact]
        public void Resolve_PendingAlert_ShouldSucceed()
        {
            var alert = AlertAggregate.Create(
                severity: "Warning",
                title: "Test",
                message: "Test message",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid()).Value;

            var result = alert.Resolve();

            result.IsSuccess.ShouldBeTrue();
            alert.Status.IsResolved.ShouldBeTrue();
            alert.ResolvedAt.ShouldNotBeNull();
        }

        [Fact]
        public void Resolve_AlreadyResolvedAlert_ShouldFail()
        {
            var alert = AlertAggregate.Create(
                severity: "Warning",
                title: "Test",
                message: "Test message",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid()).Value;

            alert.Resolve();
            var result = alert.Resolve();

            result.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public void Resolve_ShouldAddDomainEvent()
        {
            var alert = AlertAggregate.Create(
                severity: "Critical",
                title: "Test",
                message: "Test message",
                plotId: Guid.NewGuid(),
                plotName: "Plot Alpha",
                sensorId: Guid.NewGuid()).Value;

            var initialEventCount = alert.UncommittedEvents.Count;
            alert.Resolve();

            alert.UncommittedEvents.Count.ShouldBe(initialEventCount + 1);
        }

        #endregion
    }
}
