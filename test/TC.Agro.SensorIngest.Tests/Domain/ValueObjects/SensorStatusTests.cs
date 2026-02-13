using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Tests.Domain.ValueObjects
{
    public class SensorStatusTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Online")]
        [InlineData("Warning")]
        [InlineData("Offline")]
        public void Create_WithValidStatus_ShouldSucceed(string statusValue)
        {
            var result = SensorStatus.Create(statusValue);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(statusValue);
        }

        [Theory]
        [InlineData("online", "Online")]
        [InlineData("WARNING", "Warning")]
        [InlineData("offline", "Offline")]
        public void Create_WithDifferentCasing_ShouldNormalizeValue(string statusValue, string expectedValue)
        {
            var result = SensorStatus.Create(statusValue);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(expectedValue);
        }

        [Fact]
        public void Create_WithWhitespace_ShouldTrim()
        {
            var result = SensorStatus.Create("  Online  ");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Online");
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullValue_ShouldReturnRequiredError(string? statusValue)
        {
            var result = SensorStatus.Create(statusValue!);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorStatus.Required");
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("Active")]
        [InlineData("Inactive")]
        public void Create_WithInvalidStatus_ShouldReturnInvalidValueError(string statusValue)
        {
            var result = SensorStatus.Create(statusValue);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorStatus.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            var result = SensorStatus.FromDb("Online");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Online");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? statusValue)
        {
            var result = SensorStatus.FromDb(statusValue!);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorStatus.Required");
        }

        #endregion

        #region Factory Methods

        [Fact]
        public void CreateOnline_ShouldReturnOnlineStatus()
        {
            var status = SensorStatus.CreateOnline();

            status.Value.ShouldBe(SensorStatus.Online);
            status.IsOnline.ShouldBeTrue();
            status.IsWarning.ShouldBeFalse();
            status.IsOffline.ShouldBeFalse();
        }

        [Fact]
        public void CreateWarning_ShouldReturnWarningStatus()
        {
            var status = SensorStatus.CreateWarning();

            status.Value.ShouldBe(SensorStatus.Warning);
            status.IsWarning.ShouldBeTrue();
        }

        [Fact]
        public void CreateOffline_ShouldReturnOfflineStatus()
        {
            var status = SensorStatus.CreateOffline();

            status.Value.ShouldBe(SensorStatus.Offline);
            status.IsOffline.ShouldBeTrue();
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            var status = SensorStatus.Create("Online").Value;

            string result = status;

            result.ShouldBe("Online");
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            var status = SensorStatus.Create("Warning").Value;

            status.ToString().ShouldBe("Warning");
        }

        #endregion
    }
}
