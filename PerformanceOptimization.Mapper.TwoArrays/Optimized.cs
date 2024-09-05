using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;

namespace PerformanceOptimization.Mapper.TwoArrays;

public static class Optimized
{
    private static readonly DateTimeFormatInfo DateTimeFormatInfo = new()
    {
        ShortDatePattern = "MM/dd/yyyy",
        LongDatePattern = "MM/dd/yyyy"
    };

    public static Output[] Map(string json)
    {
        var data = JsonSerializer.Deserialize<Input>(json)!;
        
        // The biggest performance changes are here - List has been replaced with FrozenDictionary. 
        var places = data.Places.ToFrozenDictionary(GetDate, GetSeasonState);

        return data.Temperatures.Select(t =>
        {
            places.TryGetValue(DateOnly.FromDateTime(t.Date), out var result);
            return new Output
            {
                Date = t.Date,
                Value = double.Round(t.Value, 3),
                Season = result.Season,
                State = result.State
            };
        }).ToArray();
    }

    private static DateOnly GetDate(string s)
    {
        return DateOnly.Parse(s.AsSpan(0, 10), DateTimeFormatInfo);
    }

    private static (string? Season, string? State) GetSeasonState(string s)
    {
        return (GetSeason(s), GetState(s));

        string? GetState(string str)
        {
            if (str.Length < 21)
                return null;

            // The contract is solid - we can use span with hardcoded values to use a part of the string.
            // It eliminates all heap allocations because Span is a ref struct.
            // It will not dramatically improve the performance of the specific method by improving the overall performance of the service.
            var state = str.AsSpan(20, 2);
            return state switch
            {
                { } when state.Equals("WA", StringComparison.OrdinalIgnoreCase) => "Washington",
                { } when state.Equals("OR", StringComparison.OrdinalIgnoreCase) => "Oregon",
                { } when state.Equals("CO", StringComparison.OrdinalIgnoreCase) => "Colorado",
                { } when state.Equals("AL", StringComparison.OrdinalIgnoreCase) => "Alaska",
                { } when state.Equals("NE", StringComparison.OrdinalIgnoreCase) => "New York",
                _ => null
            };
        }

        string? GetSeason(string str)
        {
            if (str.Length < 24)
                return null;

            var season = str.AsSpan()[23..];
            return season switch
            {
                { } when season.Equals("wi", StringComparison.OrdinalIgnoreCase) => "Winter",
                { } when season.Equals("sp", StringComparison.OrdinalIgnoreCase) => "Spring",
                { } when season.Equals("su", StringComparison.OrdinalIgnoreCase) => "Summer",
                { } when season.Equals("fall", StringComparison.OrdinalIgnoreCase) => "Autumn",
                _ => null
            };
        }
    }
}