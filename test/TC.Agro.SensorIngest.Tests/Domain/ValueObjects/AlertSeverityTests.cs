using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Tests.Domain.ValueObjects
{
    public class AlertSeverityTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Critical")]
        [InlineData("Warning")]
        [InlineData("Info")]
        public void Create_WithValidSeverity_ShouldSucceed(string severityValue)
        {
            var result = AlertSeverity.Create(severityValue);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(severityValue);
        }

        [Theory]
        [InlineData("critical", "Critical")]
        [InlineData("WARNING", "Warning")]
        [InlineData("info", "Info")]
        public void Create_WithDifferentCasing_ShouldNormalizeValue(string severityValue, string expectedValue)
        {
            var result = AlertSeverity.Create(severityValue);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(expectedValue);
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullValue_ShouldReturnRequiredError(string? severityValue)
        {
            var result = AlertSeverity.Create(severityValue!);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertSeverity.Required");
        }

        [Theory]
        [InlineData("InvalidSeverity")]
        [InlineData("Error")]
        [InlineData("High")]
        public void Create_WithInvalidSeverity_ShouldReturnInvalidValueError(string severityValue)
        {
            var result = AlertSeverity.Create(severityValue);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertSeverity.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            var result = AlertSeverity.FromDb("Critical");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Critical");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? severityValue)
        {
            var result = AlertSeverity.FromDb(severityValue!);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "AlertSeverity.Required");
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            var severity = AlertSeverity.Create("Critical").Value;

            string result = severity;

            result.ShouldBe("Critical");
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            var severity = AlertSeverity.Create("Info").Value;

            severity.ToString().ShouldBe("Info");
        }

        #endregion
    }
}
