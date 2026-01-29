namespace TC.Agro.SensorIngest.Domain.Aggregates
{
    public static class SensorReadingDomainErrors
    {
        public static readonly DomainError SensorIdRequired = new("SensorReading.SensorIdRequired", "Sensor ID is required.");
        public static readonly DomainError PlotIdRequired = new("SensorReading.PlotIdRequired", "Plot ID is required.");
        public static readonly DomainError TimeRequired = new("SensorReading.TimeRequired", "Timestamp is required.");
        public static readonly DomainError MetricsRequired = new("SensorReading.MetricsRequired", "At least one metric is required.");
        public static readonly DomainError InvalidTemperature = new("SensorReading.InvalidTemperature", "Temperature is out of valid range.");
        public static readonly DomainError InvalidHumidity = new("SensorReading.InvalidHumidity", "Humidity must be between 0 and 100.");
        public static readonly DomainError InvalidSoilMoisture = new("SensorReading.InvalidSoilMoisture", "Soil moisture must be between 0 and 100.");
        public static readonly DomainError InvalidRainfall = new("SensorReading.InvalidRainfall", "Rainfall cannot be negative.");
        public static readonly DomainError SensorNotFound = new("SensorReading.SensorNotFound", "Sensor not found.");
        public static readonly DomainError PlotNotFound = new("SensorReading.PlotNotFound", "Plot not found.");
    }
}
