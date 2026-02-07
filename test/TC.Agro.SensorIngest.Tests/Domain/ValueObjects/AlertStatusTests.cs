using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Tests.Domain.ValueObjects
{
    public class AlertStatusTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Pending")]
        [InlineData("Resolved")]
        public void Create_WithValidStatus_ShouldSucceed(string statusValue)
        {
            var result = AlertStatus.Create(statusValue);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(statusValue);
        }

        [Theory]
        [InlineData("pending", "Pending")]
        [InlineData("RESOLVED", "Resolved")]
        public void Create_WithDifferentCasing_ShouldNormalizeValue(string statusValue, string expectedValue)
        {
            var result = AlertStatus.Create(statusValue);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(expectedValue);
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullValue_ShouldReturnRequiredError(string? statusValue)
        {
            var result = AlertStatus.Create(statusValue!);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertStatus.Required");
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("Active")]
        [InlineData("Closed")]
        public void Create_WithInvalidStatus_ShouldReturnInvalidValueError(string statusValue)
        {
            var result = AlertStatus.Create(statusValue);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertStatus.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            var result = AlertStatus.FromDb("Pending");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Pending");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? statusValue)
        {
            var result = AlertStatus.FromDb(statusValue!);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertStatus.Required");
        }

        #endregion

        #region Factory Methods

        [Fact]
        public void CreatePending_ShouldReturnPendingStatus()
        {
            var status = AlertStatus.CreatePending();

            status.Value.ShouldBe(AlertStatus.Pending);
            status.IsPending.ShouldBeTrue();
            status.IsResolved.ShouldBeFalse();
        }

        [Fact]
        public void CreateResolved_ShouldReturnResolvedStatus()
        {
            var status = AlertStatus.CreateResolved();

            status.Value.ShouldBe(AlertStatus.Resolved);
            status.IsResolved.ShouldBeTrue();
            status.IsPending.ShouldBeFalse();
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            var status = AlertStatus.Create("Pending").Value;

            string result = status;

            result.ShouldBe("Pending");
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            var status = AlertStatus.Create("Resolved").Value;

            status.ToString().ShouldBe("Resolved");
        }

        #endregion
    }
}
