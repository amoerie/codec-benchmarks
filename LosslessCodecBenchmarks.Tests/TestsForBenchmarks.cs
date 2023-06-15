using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libjxl;
using PhotoSauce.NativeCodecs.Libwebp;
using Xunit;

namespace LosslessCodecBenchmarks.Tests;

public class TestsForBenchmarks
{
    public record TestCase(string Library, string File, string Format, string CompressionLevel);

    private static readonly string[] Libraries = { "MagicScaler", "ImageSharp" };
    private static readonly string[] Files = { "MR", "CT", "CR" };
    private static readonly string[] Formats = { "PNG", "WEBP", "JXL" };
    private static readonly string[] CompressionSpeeds = { "BestSpeed", "Balanced", "BestCompression" };

    public static readonly TheoryData<TestCase> TestCases = new [] { Libraries, Files, Formats, CompressionSpeeds }
        .CartesianProduct()
        .Select(args => args.ToArray())
        .Select(args => new TestCase(args[0], args[1], args[2], args[3]))
        .Aggregate(new TheoryData<TestCase>(), (theoryData, testCase) =>
        {
            theoryData.Add(testCase);
            return theoryData;
        });
        
    [Theory]
    [MemberData(nameof(TestCases))]
    public void EncodeDecodeShouldBeLossless(TestCase testCase)
    {
        // Arrange
        if (testCase.Library == "MagicScaler")
        {
            CodecManager.Configure(codecs =>
            {
                codecs.UseLibjxl();
                codecs.UseLibwebp();
                codecs.UseWicCodecs(WicCodecPolicy.Microsoft);
            });
        }

        var benchmarks = new Benchmarks
        {
            Library = testCase.Library,
            Format = testCase.Format,
            File = testCase.File,
            CompressionLevel = testCase.CompressionLevel
        };
        
        // Act
        benchmarks.GlobalSetup();
        //benchmarks.Decode();
        benchmarks.GlobalCleanup();

        // Assert
        var encoderInput = benchmarks.EncoderInput;
        var decoderOutput = benchmarks.DecoderOutput;
        if (encoderInput == null
            || encoderInput.Length == 0
            || decoderOutput == null
            || decoderOutput.Length == 0)
        {
            // not supported
            return;
        }
        
        Assert.Equal(encoderInput.Length, decoderOutput.Length);
        for (int i = 0; i < encoderInput.Length; i++)
        {
            if (encoderInput[i] != decoderOutput[i])
                throw new Exception("Encoder input is not identical to decoder output");
        }
    }
}
