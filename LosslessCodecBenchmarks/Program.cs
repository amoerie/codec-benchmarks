using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LosslessCodecBenchmarks;

using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libheif;
using PhotoSauce.NativeCodecs.Libjxl;

CodecManager.Configure(codecs => {
    codecs.UseLibheif();
    codecs.UseLibjxl();
    codecs.UseWicCodecs(WicCodecPolicy.Microsoft);
});

if (!DebugDetector.AreWeInDebugMode)
{
    // In Release mode, run the benchmarks normally
    BenchmarkRunner.Run<Benchmarks>();
}
else
{
    // In Debug mode, run all benchmarks exactly once for debugging purposes
    var benchmarks = new Benchmarks();
    var parameters = typeof(Benchmarks)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => new { Property = p, ParamsAttribute = p.GetCustomAttribute<ParamsAttribute>() })
        .Where(p => p.ParamsAttribute != null)
        .OrderBy(p => p.ParamsAttribute!.Priority)
        .Select(p => p.ParamsAttribute!.Values.Select(v => new { p.Property, Value = v }));
    var parameterCombinations = parameters.CartesianProduct();

    foreach (var parameterCombination in parameterCombinations)
    {
        var sb = new StringBuilder("Running benchmark case: ").Append(Environment.NewLine);
        foreach (var parameterValue in parameterCombination)
        {
            parameterValue.Property.SetValue(benchmarks, parameterValue.Value);
            sb.Append(parameterValue.Property.Name);
            sb.Append(" = ");
            sb.Append(parameterValue.Value);
            sb.Append(Environment.NewLine);
        }
        Console.Write(sb);
        benchmarks.GlobalSetup();
        Console.WriteLine("GlobalSetup finished successfully");
        benchmarks.Encode();
        Console.WriteLine("Encode finished successfully");
        benchmarks.Decode();
        Console.WriteLine("Decode finished successfully");
        
        Console.WriteLine("----------------------------------------------");
    }

    
}
