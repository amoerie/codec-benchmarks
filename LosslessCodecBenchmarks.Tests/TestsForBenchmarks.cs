using Xunit;

namespace LosslessCodecBenchmarks.Tests;

public class TestsForBenchmarks
{
    public record TestCase(string File, string Format, string CompressionLevel);

    private static readonly string[] Files = { "MR", "CT", "CR" };
    private static readonly string[] Formats = { "LZ4", "PNG", "WEBP", "JXL", "AVIF" };
    private static readonly string[] CompressionSpeeds = { "BestSpeed", "BestCompression" };

    public static readonly TheoryData<TestCase> TestCases = new [] { Files, Formats, CompressionSpeeds }
        .CartesianProduct()
        .Select(args => args.ToArray())
        .Select(args => new TestCase(args[0], args[1], args[2]))
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
        var benchmarks = new Benchmarks
        {
            File = testCase.File,
            Format = testCase.Format,
            CompressionLevel = testCase.CompressionLevel
        };
        
        // Act
        benchmarks.GlobalSetup();
        benchmarks.Decode();

        // Assert
        var encoderInput = benchmarks.EncoderInput;
        var decoderOutput = benchmarks.DecoderOutput;
        Assert.Equal(encoderInput.Length, decoderOutput.Length);
        for (int i = 0; i < encoderInput.Length; i++)
        {
            if (encoderInput[i] != decoderOutput[i])
                throw new Exception("Encoder input is not identical to decoder output");
        }
    }
}
