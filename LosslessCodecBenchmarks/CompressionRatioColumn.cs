using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace LosslessCodecBenchmarks;

/// <summary>
/// Based on https://github.com/dotnet/BenchmarkDotNet/issues/180
/// </summary>
public class CompressionRatioColumn : IColumn
{
    public string Id => nameof(CompressionRatioColumn);
    public string ColumnName => "Compression Ratio";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Params;
    public int PriorityInCategory => 4;
    public bool IsNumeric => false;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "Compression Ratio";
    public override string ToString() => ColumnName;

    public string GetValue(Summary summary, BenchmarkCase benchmark)
    {
        if (benchmark.Descriptor.WorkloadMethod.Name != nameof(Benchmarks.Encode))
            return "N/A";

        var type = benchmark.Descriptor.Type;
        var instance = Activator.CreateInstance(type);

        type.GetProperty(nameof(Benchmarks.ReportCompressionRatio), BindingFlags.Public | BindingFlags.Instance)
            !.SetValue(instance, true);

        foreach (var parameter in benchmark.Parameters.Items)
        {
            type.GetProperty(parameter.Name, BindingFlags.Public | BindingFlags.Instance)
                !.SetValue(instance, parameter.Value);
        }

        benchmark.Descriptor.GlobalSetupMethod.Invoke(instance, Array.Empty<object>());

        var originalFileSize = (double)type.GetProperty(nameof(Benchmarks.OriginalFileSize), BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance)!;
        var encodedFileSize = (double)type.GetProperty(nameof(Benchmarks.EncodedFileSize), BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance)!;
        var compressionRatio = 100 * encodedFileSize / originalFileSize;

        var originalFileSizeKb = originalFileSize / 1024;
        var encodedFileSizeKb = encodedFileSize / 1024;

        return $"{originalFileSizeKb:0}Kb -> {encodedFileSizeKb:0}Kb ({compressionRatio:0.0}%)";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public bool IsAvailable(Summary summary) => true;
}
