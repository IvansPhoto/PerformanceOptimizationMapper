## About
This project is a real case from my work about the consequences of negligent writing a simple mapper. Because the code from the project cannot be shared, I created a very similar mapper for weather observations.
## How it has begun
In one of my projects about refactoring, several endpoints run for more than a few seconds. We investigated the root cases for all slow endpoints and this story is about one of these investigations and fixes.
The endpoint runs about 30 seconds in a development environment (~16 seconds on my PC). After reading the call chain and short debugging, I quickly found out that the mapper took all the time except for ~100ms .
The full code is available on [GitHub](https://github.com/IvansPhoto/PerformanceOptimizationMapper)## Rules
The purpose of the mapper is to merge data from two arrays of the Input record: Temperature[] Temperatures and string[] Places into a third array of the Output record.
The resulting array should be the same size as the Temperatures array and should contain values and dates from the Temperatures array. Data for states and seasons fields should be received from the Places array. Each date in the Temperatures array is unique.
The Places array can be less than, equal to, or greater than the Temperatures array.
The Places array contains strings that can have one, two, or three segments, with the ‘;’ delimiter. The first segment is a date in the format: month/day/year and the constant time stamp 00:00:00, the date is unique per array; this segment is always present. The second segment is two letters of a state and optional. The third segment is an abbreviation of a state and it and the delimiter are also optional. State and season abbreviations should be transformed into the full names of states and seasons, mappers should be case-insensitive.
Arrays should be matched by the date. If the Places array does not have a record with the corresponding date, or a record does not have the second and/or the third segment the states and/or seasons fields should be null.
## Results
The benchmark results are placed in the result folder.
This is a table from the basic benchmark.
BenchmarkDotNet v0.13.11, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
AMD Ryzen 7 6800H with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.204
[Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

| Method                    | N     | Mean          | Error       | StdDev      | Ratio  | RatioSD | Gen0        | Gen1       | Gen2     |  Allocated | Alloc Ratio |
|-------------------------- |------ |--------------:|------------:|------------:|-------:|--------:|------------:|-----------:|---------:|-----------:|------------:|
| MapOriginal               | 10000 | 10,042.696ms  | 146.6087ms  | 137.1378ms  | 924.52 |   44.07 | 971000.0000 | 24000.0000 |        - |  7752.77MB |    1,510.74 |
| MapOptimized              | 10000 |     12.070ms  |   0.3817ms  |   1.1196ms  |   1.00 |    0.00 |    875.0000 |   765.6250 | 484.3750 |     5.13MB |        1.00 |
| MapOptimizedStruct        | 10000 |      5.560ms  |   0.1109ms  |   0.1554ms  |   0.52 |    0.03 |    460.9375 |   429.6875 | 281.2500 |     4.16MB |        0.81 |
| MapOptimizedStructMarshal | 10000 |      4.910ms  |   0.0547ms  |   0.0485ms  |   0.45 |    0.02 |    625.0000 |   593.7500 | 460.9375 |     3.39MB |        0.66 |

### Why is it so slow?
The entry point is a small method called **Map**.
The main logic is placed in the **MapSingle** method where we can find the general computation recourse waster.
```csharp
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

        string[]? GetData(Temperature temperature, DateTimeFormatInfo, string[] strings)
        {
            // After some time reading the code, you can notice that this is a foreach cycle inside .Select cycle
            // for searching an element and this is the general computation recourse waster.
            foreach (var str in strings)
            {
                // The method creates a lot of allocations with new strings, but only the first one is in use.
                var segments = str.Split(';');
                
                // To pass tests, paste the DateTimeFormatInfo as the second argument here in DateTime.TryParse().  
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
```

### How to improve
We can significantly improve performance just by replacing the liner search in array/list with a dictionary, and more specifically, FrozenDictionary. This is an immutable, search-optimized dictionary that was introduced in .NET 8.

But should we limit ourselves to these changes because more places are far from good code?

For example, using the **.Split()** method creates new strings that should be analyzed and consumed by GC. It can be replaced by **.AsSpan()** with zero allocation on the heap.
These changes will not dramatically improve the performance of the specific method by improving the overall performance of the service by reducing pressure on GC.
After these improvements, the method execution time will be drastically decreased.
```csharp
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
            // It eliminates all heap allocations because span is a ref struct.
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
```

### Better results but higher complexity.
We can make the mapper even faster without diving into **unsafe** code and making the mapper _readonly_.
We can replace classes with structs for data-storage entities and if be precise we will use **readonly record struct** instead of **records**.
```csharp
// Before
public record Output
{
    public DateTime Date { get; init; }
    public double Value { get; init; }
    public string? State { get; init; }
    public string? Season { get; init; }
}

public record Input(Temperature[] Temperatures, string[] Places);

public record Temperature(DateTime Date, double Value);

// After
public readonly record struct OutputSt
{
    public DateTime Date { get; init; }
    public double Value { get; init; }
    public string? State { get; init; }
    public string? Season { get; init; }
}

public readonly record struct InputSt(TemperatureSt[] Temperatures, string[] Places);

public readonly record struct TemperatureSt(DateTime Date, double Value);
```
Using **struct** is not as easy as using classes because of its nature. We must remember that when we pass a struct as an argument or return it from a method, all the data is copied.
For small structs like primitives, this is more than normal, but for large entities, it will lead to performance degradation.
But we can eliminate this problem simply by using ref / it / out to avoid data copying.
Another feature of structs is the structure of array and array-based collections.
An array of classes stores only links to an instance of that class in a heap, but an array of structs stores the whole struct itself.
I think an array of structs gives less memory fragmentation (but has better chances of being allocated in LOH), which will decrease the time spent getting an array element.
These small changes improve the method execution time twice.
```csharp
public static class OptimizedStruct
{
    private static readonly DateTimeFormatInfo DateTimeFormatInfo = new()
    {
        ShortDatePattern = "MM/dd/yyyy",
        LongDatePattern = "MM/dd/yyyy"
    };
    
    public static OutputSt[] Map(string json)
    {
        var data = JsonSerializer.Deserialize<InputSt>(json);
        var places = data.Places.ToFrozenDictionary(GetDate, GetSeasonState);

        return data.Temperatures.Select(t =>
        {
            places.TryGetValue(DateOnly.FromDateTime(t.Date), out var result);
            return new OutputSt
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

            var season = str.AsSpan(23, str.Length - 23);
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
```
### Another small boost.
We can slightly improve the performance by using a special method **GetValueRefOrNullRef** from the static class **CollectionsMarshal** to find an element and get a reference to it.
But FrozenDictionary has to be replaced with a usual Dictionary.
This manipulation gives us around a 6% boost over to the previous method.
```csharp
public static class OptimizedStructMarshal
{
    private static readonly DateTimeFormatInfo DateTimeFormatInfo = new()
    {
        ShortDatePattern = "MM/dd/yyyy",
        LongDatePattern = "MM/dd/yyyy"
    };
    
    public static OutputSt[] Map(string json)
    {
        var data = JsonSerializer.Deserialize<InputSt>(json);
        var places = data.Places.ToDictionary(GetDate, GetSeasonState);

        return data.Temperatures.Select(t =>
        {
            // New way to find the element.
            ref var result = ref CollectionsMarshal.GetValueRefOrNullRef(places, DateOnly.FromDateTime(t.Date));
            return new OutputSt
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

            var season = str.AsSpan(23, str.Length - 23);
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

```
