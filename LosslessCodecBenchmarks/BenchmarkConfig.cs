using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace LosslessCodecBenchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()));
        AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
        AddExporter(DefaultConfig.Instance.GetExporters().ToArray());
        AddDiagnoser(DefaultConfig.Instance.GetDiagnosers().ToArray());
        AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
        AddValidator(DefaultConfig.Instance.GetValidators().ToArray());

        AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
        AddColumn(new CompressionRatioColumn());
    }
}
