```
BenchmarkDotNet v0.13.11, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
AMD Ryzen 7 6800H with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```
| Method                    | N     | Mean          | Error       | StdDev      | Ratio  | RatioSD | Gen0        | Gen1       | Gen2     |  Allocated | Alloc Ratio |
|-------------------------- |------ |--------------:|------------:|------------:|-------:|--------:|------------:|-----------:|---------:|-----------:|------------:|
| MapOriginal               | 10000 | 10,042.696ms | 146.6087ms | 137.1378ms | 924.52 |   44.07 | 971000.0000 | 24000.0000 |        - |  7752.77MB |    1,510.74 |
| MapOptimized              | 10000 |     12.070ms |   0.3817ms |   1.1196ms |   1.00 |    0.00 |    875.0000 |   765.6250 | 484.3750 |     5.13MB |        1.00 |
| MapOptimizedStruct        | 10000 |      5.560ms |   0.1109ms |   0.1554ms |   0.52 |    0.03 |    460.9375 |   429.6875 | 281.2500 |     4.16MB |        0.81 |
| MapOptimizedStructMarshal | 10000 |      4.910ms |   0.0547ms |   0.0485ms |   0.45 |    0.02 |    625.0000 |   593.7500 | 460.9375 |     3.39MB |        0.66 |
