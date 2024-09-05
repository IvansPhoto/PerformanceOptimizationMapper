﻿using System.Globalization;
using System.Text.Json;

namespace PerformanceOptimization.Mapper.TwoArrays;

public static class Original
{
    private static readonly DateTimeFormatInfo DateTimeFormatInfo = new()
    {
        ShortDatePattern = "MM/dd/yyyy HH:mm:ss",
        LongDatePattern = "MM/dd/yyyy HH:mm:ss",
    };
    
    // The "entry point"
    public static Output[] Map(string json)
    {
        var data = JsonSerializer.Deserialize<Input>(json)!;
        return data.Temperatures.Select(t => t.MapSingle(data.Places)).ToArray();
    }

    // The main mapper
    private static Output MapSingle(this Temperature src, string[] places)
    {
        var data = GetData(src, places);
        var state = data?.Length > 1 ? data[1] : null;
        var season = data?.Length > 2 ? data[2].ToLower() : null;

        var mapSingle = new Output
        {
            Date = src.Date,
            Value = double.Parse(src.Value.ToString("0.##0")),
            State = GetState(state),
            Season = GetSeason(season)
        };
        return mapSingle;

        string[]? GetData(Temperature temperature, string[] strings)
        {
            // After some time reading the code, you can notice that this is a foreach cycle inside .Select cycle
            // for searching an element and this is the general computation recourse waster.
            foreach (var str in strings)
            {
                // The method creates a lot of allocations with new strings, but only the fist one is in use.
                var segments = str.Split(';');
                
                // To pass tests, paste the DateTimeFormatInfo as second argument here in DateTime.TryParse().  
                if (DateTime.TryParse(segments[0], out var result))
                {
                    if (DateTime.Equals(temperature.Date, result))
                    {
                        return segments;
                    }
                }
            }

            return null;
        }

        string? GetState(string? s1)
        {
            return s1 switch
            {
                "WA" => "Washington",
                "OR" => "Oregon",
                "NE" => "New York",
                "AL" => "Alaska",
                "CO" => "Colorado",
                _ => null
            };
        }

        string? GetSeason(string? s2)
        {
            return s2 switch
            {
                "wi" => "Winter",
                "sp" => "Spring",
                "su" => "Summer",
                "fall" => "Autumn",
                _ => null
            };
        }
    }
}