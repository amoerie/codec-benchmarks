using System.Diagnostics;

namespace LosslessCodecBenchmarks;

public static class DebugDetector
{
    public static bool AreWeInDebugMode;

    static DebugDetector()
    {
        YesWeAre();
    }

    /**
     * This method will be stripped out by the compiler when this project is built in Release mode
     */
    [Conditional("DEBUG")]
    private static void YesWeAre()
    {
        AreWeInDebugMode = true;
    }
}
