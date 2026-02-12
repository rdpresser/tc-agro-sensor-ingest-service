namespace TC.Agro.SensorIngest.Service.Telemetry
{
    internal class TelemetryExporterInfo
    {
        public string ExporterType { get; set; } = "None";
        public float? SamplingRatio { get; set; }
        public string? Endpoint { get; set; }
        public string? Protocol { get; set; }
    }
}
