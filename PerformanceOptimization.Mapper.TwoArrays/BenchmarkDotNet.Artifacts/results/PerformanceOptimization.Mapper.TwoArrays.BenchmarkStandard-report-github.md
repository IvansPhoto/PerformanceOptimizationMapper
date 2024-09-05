```

BenchmarkDotNet v0.13.11, Windows 11 (10.0.22631.4037/23H2/2023Update/SunValley3)
AMD Ryzen 7 6800H with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method                    | N     | Mean          | Error       | StdDev      | Median        | Ratio    | RatioSD | Gen0         | Gen1       | Gen2     | Allocated   | Alloc Ratio |
|-------------------------- |------ |--------------:|------------:|------------:|--------------:|---------:|--------:|-------------:|-----------:|---------:|------------:|------------:|
| MapOriginal               | 10000 | 13,117.831 ms | 120.0636 ms | 112.3075 ms | 13,117.128 ms | 1,241.72 |   58.01 | 1568000.0000 | 25000.0000 |        - | 12511.64 MB |    2,437.84 |
| MapOptimized              | 10000 |     12.582 ms |   0.5073 ms |   1.4957 ms |     13.328 ms |     1.00 |    0.00 |     875.0000 |   796.8750 | 484.3750 |     5.13 MB |        1.00 |
| MapOptimizedStruct        | 10000 |      5.494 ms |   0.1072 ms |   0.1002 ms |      5.515 ms |     0.52 |    0.02 |     570.3125 |   515.6250 | 390.6250 |     4.16 MB |        0.81 |
| MapOptimizedStructMarshal | 10000 |      5.175 ms |   0.0451 ms |   0.0400 ms |      5.183 ms |     0.49 |    0.02 |     632.8125 |   585.9375 | 468.7500 |     3.39 MB |        0.66 |
