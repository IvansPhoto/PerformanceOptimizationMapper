namespace PerformanceOptimization.Mapper.TwoArrays;

public record Output
{
    public DateTime Date { get; init; }
    public double Value { get; init; }
    public string? State { get; init; }
    public string? Season { get; init; }
}

public record Input(Temperature[] Temperatures, string[] Places);

public record Temperature(DateTime Date, double Value);

public readonly record struct OutputSt
{
    public DateTime Date { get; init; }
    public double Value { get; init; }
    public string? State { get; init; }
    public string? Season { get; init; }
}

public readonly record struct InputSt(TemperatureSt[] Temperatures, string[] Places);

public readonly record struct TemperatureSt(DateTime Date, double Value);