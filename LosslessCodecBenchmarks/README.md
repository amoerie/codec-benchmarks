**Lossless image codecs**

In my quest for a good lossless image codec + fast implementation I have written benchmarks to measure
- The encoding speed
- The decoding speed
- The compression ratio of the encoded file compared to BMP (which is as good as raw)

**Before taking any conclusions, please take note of the following**

- I am not an imaging expert, nor am I intimately familiar with MagicScaler or ImageSharp. Please open an issue if you spot something in the benchmark code
- The MagicScaler WebP encoder is not lossless yet because the encoder does not accept parameters yet. See https://github.com/saucecontrol/PhotoSauce/discussions/88
- MagicScaler PNG does not allow parameterization for best compression or best speed (AFAIK)
- ImageSharp does not support JXL (yet)
- MagicScaler uses WIC (Windows Imaging Component) under the hood, which only works on Windows

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1766 (21H2)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.301
  [Host]     : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT
  DefaultJob : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT


```
| Method |     Library | Format | File | CompressionLevel |         Compression Ratio |                 Mean |              Error |             StdDev |     Gen 0 |     Gen 1 |     Gen 2 |    Allocated |
|------- |------------ |------- |----- |----------------- |-------------------------- |---------------------:|-------------------:|-------------------:|----------:|----------:|----------:|-------------:|
| **Encode** |  **ImageSharp** |    **BMP** |   **CR** |  **BestCompression** | **2601Kb -&gt; 2601Kb (100,0%)** |       **526,815.257 ns** |     **12,423.9180 ns** |     **36,044.0388 ns** |         **-** |         **-** |         **-** |        **250 B** |
| Decode |  ImageSharp |    BMP |   CR |  BestCompression |                       N/A |     1,050,487.939 ns |     20,754.1519 ns |     20,383.3522 ns |         - |         - |         - |      2,122 B |
| **Encode** |  **ImageSharp** |    **BMP** |   **CR** |        **BestSpeed** | **2601Kb -&gt; 2601Kb (100,0%)** |       **466,543.282 ns** |      **6,174.8791 ns** |      **4,820.9394 ns** |         **-** |         **-** |         **-** |        **249 B** |
| Decode |  ImageSharp |    BMP |   CR |        BestSpeed |                       N/A |     1,074,101.968 ns |     15,434.4758 ns |     12,888.4863 ns |         - |         - |         - |      2,122 B |
| **Encode** |  **ImageSharp** |    **BMP** |   **MR** |  **BestCompression** |   **192Kb -&gt; 192Kb (100,0%)** |        **32,678.588 ns** |        **636.4062 ns** |        **564.1575 ns** |    **0.0610** |         **-** |         **-** |        **248 B** |
| Decode |  ImageSharp |    BMP |   MR |  BestCompression |                       N/A |        73,155.232 ns |      1,325.1730 ns |      1,239.5676 ns |    0.6104 |         - |         - |      2,122 B |
| **Encode** |  **ImageSharp** |    **BMP** |   **MR** |        **BestSpeed** |   **192Kb -&gt; 192Kb (100,0%)** |        **32,441.482 ns** |        **567.7723 ns** |        **531.0946 ns** |    **0.0610** |         **-** |         **-** |        **248 B** |
| Decode |  ImageSharp |    BMP |   MR |        BestSpeed |                       N/A |        70,853.088 ns |      1,117.6023 ns |      1,330.4261 ns |    0.6104 |         - |         - |      2,120 B |
| **Encode** |  **ImageSharp** |    **JXL** |   **CR** |  **BestCompression** |         **0Kb -&gt; 0Kb (NaN%)** |             **1.429 ns** |          **0.0514 ns** |          **0.0481 ns** |         **-** |         **-** |         **-** |            **-** |
| Decode |  ImageSharp |    JXL |   CR |  BestCompression |                       N/A |             1.191 ns |          0.0553 ns |          0.0738 ns |         - |         - |         - |            - |
| **Encode** |  **ImageSharp** |    **JXL** |   **CR** |        **BestSpeed** |         **0Kb -&gt; 0Kb (NaN%)** |             **1.134 ns** |          **0.0323 ns** |          **0.0302 ns** |         **-** |         **-** |         **-** |            **-** |
| Decode |  ImageSharp |    JXL |   CR |        BestSpeed |                       N/A |             1.413 ns |          0.0498 ns |          0.0441 ns |         - |         - |         - |            - |
| **Encode** |  **ImageSharp** |    **JXL** |   **MR** |  **BestCompression** |         **0Kb -&gt; 0Kb (NaN%)** |             **1.440 ns** |          **0.0467 ns** |          **0.0414 ns** |         **-** |         **-** |         **-** |            **-** |
| Decode |  ImageSharp |    JXL |   MR |  BestCompression |                       N/A |             1.164 ns |          0.0458 ns |          0.0406 ns |         - |         - |         - |            - |
| **Encode** |  **ImageSharp** |    **JXL** |   **MR** |        **BestSpeed** |         **0Kb -&gt; 0Kb (NaN%)** |             **1.142 ns** |          **0.0404 ns** |          **0.0358 ns** |         **-** |         **-** |         **-** |            **-** |
| Decode |  ImageSharp |    JXL |   MR |        BestSpeed |                       N/A |             1.487 ns |          0.0624 ns |          0.0789 ns |         - |         - |         - |            - |
| **Encode** |  **ImageSharp** |    **PNG** |   **CR** |  **BestCompression** |   **2601Kb -&gt; 552Kb (21,2%)** | **1,547,612,114.286 ns** | **15,749,081.5828 ns** | **13,961,150.6074 ns** |         **-** |         **-** |         **-** |  **2,698,776 B** |
| Decode |  ImageSharp |    PNG |   CR |  BestCompression |                       N/A |     9,803,696.771 ns |    173,232.2368 ns |    162,041.5418 ns |         - |         - |         - |      4,774 B |
| **Encode** |  **ImageSharp** |    **PNG** |   **CR** |        **BestSpeed** |   **2601Kb -&gt; 695Kb (26,7%)** |    **49,401,650.303 ns** |    **984,971.8245 ns** |    **921,343.2558 ns** |   **90.9091** |   **90.9091** |   **90.9091** |  **2,860,381 B** |
| Decode |  ImageSharp |    PNG |   CR |        BestSpeed |                       N/A |    13,590,941.771 ns |     85,968.3180 ns |     80,414.8180 ns |         - |         - |         - |      4,932 B |
| **Encode** |  **ImageSharp** |    **PNG** |   **MR** |  **BestCompression** |     **192Kb -&gt; 22Kb (11,6%)** |    **24,650,037.370 ns** |    **486,746.8181 ns** |    **632,908.3523 ns** |         **-** |         **-** |         **-** |     **93,648 B** |
| Decode |  ImageSharp |    PNG |   MR |  BestCompression |                       N/A |       894,678.065 ns |     15,552.9607 ns |     12,987.4266 ns |    0.9766 |         - |         - |      3,954 B |
| **Encode** |  **ImageSharp** |    **PNG** |   **MR** |        **BestSpeed** |     **192Kb -&gt; 27Kb (13,8%)** |     **2,104,191.432 ns** |     **34,977.3782 ns** |     **32,717.8613 ns** |   **31.2500** |         **-** |         **-** |     **99,036 B** |
| Decode |  ImageSharp |    PNG |   MR |        BestSpeed |                       N/A |       925,181.323 ns |     10,170.1027 ns |      7,940.1472 ns |    0.9766 |         - |         - |      3,953 B |
| **Encode** |  **ImageSharp** |   **WEBP** |   **CR** |  **BestCompression** |   **2601Kb -&gt; 327Kb (12,6%)** |   **628,443,176.923 ns** | **10,341,481.9742 ns** |  **8,635,605.8165 ns** | **9000.0000** | **4000.0000** | **1000.0000** | **80,602,624 B** |
| Decode |  ImageSharp |   WEBP |   CR |  BestCompression |                       N/A |    24,916,778.750 ns |    273,210.8911 ns |    255,561.6371 ns |   31.2500 |         - |         - |    293,570 B |
| **Encode** |  **ImageSharp** |   **WEBP** |   **CR** |        **BestSpeed** |   **2601Kb -&gt; 384Kb (14,8%)** |   **399,346,862.069 ns** |  **7,738,309.3222 ns** | **11,342,713.4699 ns** | **4000.0000** | **1000.0000** |         **-** | **57,934,272 B** |
| Decode |  ImageSharp |   WEBP |   CR |        BestSpeed |                       N/A |    29,726,558.125 ns |    388,715.9227 ns |    363,605.1153 ns |   31.2500 |         - |         - |    253,050 B |
| **Encode** |  **ImageSharp** |   **WEBP** |   **MR** |  **BestCompression** |      **192Kb -&gt; 12Kb (6,3%)** |    **54,561,684.286 ns** |    **790,268.0297 ns** |    **700,552.0242 ns** | **4100.0000** | **1800.0000** |  **600.0000** | **25,070,073 B** |
| Decode |  ImageSharp |   WEBP |   MR |  BestCompression |                       N/A |     1,010,478.064 ns |     19,991.0908 ns |     28,024.7173 ns |   66.4063 |         - |         - |    210,753 B |
| **Encode** |  **ImageSharp** |   **WEBP** |   **MR** |        **BestSpeed** |      **192Kb -&gt; 14Kb (7,1%)** |    **12,477,008.021 ns** |    **177,431.0939 ns** |    **165,969.1553 ns** |  **671.8750** |  **625.0000** |  **468.7500** |  **3,454,910 B** |
| Decode |  ImageSharp |   WEBP |   MR |        BestSpeed |                       N/A |     1,196,780.090 ns |     23,573.4262 ns |     36,700.9846 ns |   27.3438 |         - |         - |     87,539 B |
| **Encode** | **MagicScaler** |    **BMP** |   **CR** |  **BestCompression** |   **870Kb -&gt; 870Kb (100,0%)** |     **5,887,446.354 ns** |    **112,799.3697 ns** |    **105,512.6004 ns** |         **-** |         **-** |         **-** |      **1,078 B** |
| Decode | MagicScaler |    BMP |   CR |  BestCompression |                       N/A |     5,829,875.931 ns |     62,893.7371 ns |     52,519.1190 ns |         - |         - |         - |      1,078 B |
| **Encode** | **MagicScaler** |    **BMP** |   **CR** |        **BestSpeed** |   **870Kb -&gt; 870Kb (100,0%)** |     **5,916,002.051 ns** |    **111,965.4394 ns** |    **174,316.7000 ns** |         **-** |         **-** |         **-** |      **1,078 B** |
| Decode | MagicScaler |    BMP |   CR |        BestSpeed |                       N/A |     5,826,777.644 ns |     49,677.1521 ns |     41,482.6719 ns |         - |         - |         - |      1,078 B |
| **Encode** | **MagicScaler** |    **BMP** |   **MR** |  **BestCompression** |     **65Kb -&gt; 65Kb (100,0%)** |       **435,888.823 ns** |      **8,258.7421 ns** |     **10,444.6644 ns** |         **-** |         **-** |         **-** |      **1,072 B** |
| Decode | MagicScaler |    BMP |   MR |  BestCompression |                       N/A |       433,653.469 ns |      6,655.3451 ns |      5,557.5146 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |    **BMP** |   **MR** |        **BestSpeed** |     **65Kb -&gt; 65Kb (100,0%)** |       **469,087.678 ns** |      **9,352.7001 ns** |     **25,602.8657 ns** |         **-** |         **-** |         **-** |      **1,072 B** |
| Decode | MagicScaler |    BMP |   MR |        BestSpeed |                       N/A |       436,325.285 ns |      2,775.3637 ns |      2,317.5544 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |    **JXL** |   **CR** |  **BestCompression** |    **870Kb -&gt; 528Kb (60,7%)** |     **9,968,420.536 ns** |    **189,494.8030 ns** |    **167,982.2071 ns** |         **-** |         **-** |         **-** |        **972 B** |
| Decode | MagicScaler |    JXL |   CR |  BestCompression |                       N/A |     2,327,517.663 ns |     45,705.0123 ns |     57,802.2064 ns |         - |         - |         - |      1,075 B |
| **Encode** | **MagicScaler** |    **JXL** |   **CR** |        **BestSpeed** |    **870Kb -&gt; 559Kb (64,2%)** |    **10,284,979.282 ns** |    **204,675.0919 ns** |    **286,925.8934 ns** |         **-** |         **-** |         **-** |        **971 B** |
| Decode | MagicScaler |    JXL |   CR |        BestSpeed |                       N/A |     2,286,670.254 ns |     44,149.9193 ns |     50,843.1127 ns |         - |         - |         - |      1,075 B |
| **Encode** | **MagicScaler** |    **JXL** |   **MR** |  **BestCompression** |      **65Kb -&gt; 17Kb (26,8%)** |       **687,281.077 ns** |      **4,566.8908 ns** |      **3,813.5606 ns** |         **-** |         **-** |         **-** |        **961 B** |
| Decode | MagicScaler |    JXL |   MR |  BestCompression |                       N/A |       255,615.956 ns |      4,958.0530 ns |      4,140.1988 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |    **JXL** |   **MR** |        **BestSpeed** |      **65Kb -&gt; 19Kb (29,1%)** |       **687,409.361 ns** |      **5,802.5030 ns** |      **5,143.7678 ns** |         **-** |         **-** |         **-** |        **961 B** |
| Decode | MagicScaler |    JXL |   MR |        BestSpeed |                       N/A |       252,769.181 ns |      2,985.5550 ns |      2,330.9250 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |    **PNG** |   **CR** |  **BestCompression** |    **870Kb -&gt; 421Kb (48,4%)** |    **29,914,077.746 ns** |    **914,391.2158 ns** |  **2,681,751.1488 ns** |         **-** |         **-** |         **-** |        **992 B** |
| Decode | MagicScaler |    PNG |   CR |  BestCompression |                       N/A |     4,730,181.633 ns |     95,980.0371 ns |    278,455.4912 ns |         - |         - |         - |      1,078 B |
| **Encode** | **MagicScaler** |    **PNG** |   **CR** |        **BestSpeed** |    **870Kb -&gt; 421Kb (48,4%)** |    **30,204,859.539 ns** |    **718,756.4345 ns** |  **2,062,246.9461 ns** |         **-** |         **-** |         **-** |      **1,005 B** |
| Decode | MagicScaler |    PNG |   CR |        BestSpeed |                       N/A |     4,946,633.353 ns |     98,464.8449 ns |    259,395.7860 ns |         - |         - |         - |      1,075 B |
| **Encode** | **MagicScaler** |    **PNG** |   **MR** |  **BestCompression** |      **65Kb -&gt; 15Kb (23,5%)** |     **1,350,815.169 ns** |     **19,404.8582 ns** |     **25,231.7969 ns** |         **-** |         **-** |         **-** |        **961 B** |
| Decode | MagicScaler |    PNG |   MR |  BestCompression |                       N/A |       284,931.642 ns |      5,487.1838 ns |      5,132.7152 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |    **PNG** |   **MR** |        **BestSpeed** |      **65Kb -&gt; 15Kb (23,5%)** |     **1,341,820.742 ns** |     **25,148.4637 ns** |     **56,248.1428 ns** |         **-** |         **-** |         **-** |        **961 B** |
| Decode | MagicScaler |    PNG |   MR |        BestSpeed |                       N/A |       339,063.449 ns |     10,525.5639 ns |     31,034.8627 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |   **WEBP** |   **CR** |  **BestCompression** |    **870Kb -&gt; 108Kb (12,4%)** |    **11,287,760.698 ns** |    **225,468.1982 ns** |    **450,284.3544 ns** |         **-** |         **-** |         **-** |        **976 B** |
| Decode | MagicScaler |   WEBP |   CR |  BestCompression |                       N/A |     2,781,834.817 ns |     83,699.9154 ns |    245,477.3629 ns |         - |         - |         - |      1,075 B |
| **Encode** | **MagicScaler** |   **WEBP** |   **CR** |        **BestSpeed** |    **870Kb -&gt; 108Kb (12,4%)** |    **11,673,005.125 ns** |    **344,425.2028 ns** |  **1,015,545.4816 ns** |         **-** |         **-** |         **-** |        **967 B** |
| Decode | MagicScaler |   WEBP |   CR |        BestSpeed |                       N/A |     2,588,052.851 ns |    100,555.0162 ns |    294,910.4559 ns |         - |         - |         - |      1,074 B |
| **Encode** | **MagicScaler** |   **WEBP** |   **MR** |  **BestCompression** |       **65Kb -&gt; 9Kb (14,2%)** |       **795,172.766 ns** |     **24,518.2954 ns** |     **72,292.7470 ns** |         **-** |         **-** |         **-** |        **960 B** |
| Decode | MagicScaler |   WEBP |   MR |  BestCompression |                       N/A |       302,462.302 ns |     11,655.0103 ns |     34,365.0608 ns |         - |         - |         - |      1,072 B |
| **Encode** | **MagicScaler** |   **WEBP** |   **MR** |        **BestSpeed** |       **65Kb -&gt; 9Kb (14,2%)** |       **801,147.710 ns** |     **26,560.1657 ns** |     **77,477.2499 ns** |         **-** |         **-** |         **-** |        **961 B** |
| Decode | MagicScaler |   WEBP |   MR |        BestSpeed |                       N/A |       288,695.457 ns |      9,841.1266 ns |     28,707.0281 ns |         - |         - |         - |      1,072 B |
