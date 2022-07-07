using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libjxl;

namespace LosslessCodecBenchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class Benchmarks
{
    private Action? _encode;
    private Action? _decode;
    private byte[]? _encoderInput;
    private byte[]? _encoderOutput;
    private byte[]? _decoderInput;
    private byte[]? _decoderOutput;

    [Params("MR", "CT", "CR" , Priority = 0)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "This is a Benchmark parameter")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "This is a Benchmark parameter")]
    public string? File { get; set; }

    [Params("LZ4", "PNG", "WEBP", /*"HEIF", [ Heif does not seem to be working? ]*/ "JXL", "AVIF", Priority = 1)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "This is a Benchmark parameter")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "This is a Benchmark parameter")]
    public string? Format { get; set; }

    [Params("BestSpeed", "BestCompression", Priority = 2)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "This is a Benchmark parameter")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "This is a Benchmark parameter")]
    public string? CompressionLevel { get; set; }

    /// <summary>
    /// To populate the "Compression Ratio" column, we will run the encode function exactly once.
    /// It is assumed that the properties <see cref="OriginalFileSize"/> and <see cref="EncodedFileSize"/> will be filled in after doing so
    /// This only happens when <see cref="ReportCompressionRatio"/> is set to true
    /// All of this is mostly done via reflection, based on https://github.com/dotnet/BenchmarkDotNet/issues/180
    /// </summary>
    /// <seealso cref="CompressionRatioColumn"/>
    public bool ReportCompressionRatio { get; set; }

    /// <summary>
    /// Will be populated with the raw decoded file size in BMP format
    /// </summary>
    public double OriginalFileSize { get; set; }
    
    /// <summary>
    /// Will be populated with the encoded file size, depending on the current <see cref="Format"/>
    /// </summary>
    public double EncodedFileSize { get; set; }

    public byte[] EncoderInput => _encoderInput!;
    public byte[] DecoderOutput => _decoderOutput!;

    /// <summary>
    /// Global setup is run once for every combination of parameters
    /// We (ab)use this to setup an _encode and _decode func so we only have 2 benchmark methods
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        {
            var file = new FileInfo($"./TestData/{File}.png");
            var decodeToRawSettings = GetDefaultDecoderSettings();
            decodeToRawSettings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

            using var fileMs = new MemoryStream();
            MagicImageProcessor.ProcessImage(file.FullName, fileMs, decodeToRawSettings);
            _encoderInput = fileMs.ToArray();
            
            if (ReportCompressionRatio)
            {
                OriginalFileSize = _encoderInput.Length;
            }
        }

        LZ4Level lz4CompressionLevel;
        switch (CompressionLevel)
        {
            case "BestSpeed":
                lz4CompressionLevel = LZ4Level.L00_FAST;
                break;
            case "BestCompression":
                lz4CompressionLevel = LZ4Level.L12_MAX;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _encoderOutput = new byte[_encoderInput.Length];
        switch (Format)
        {
            case "LZ4":
            {
                var encodedLength = LZ4Codec.Encode(_encoderInput, _encoderOutput, lz4CompressionLevel);
                Array.Resize(ref _encoderOutput, encodedLength);
                
                _decoderInput = new byte[encodedLength];
                _decoderOutput = new byte[_encoderInput.Length];
                Array.Copy(_encoderOutput, _decoderInput, encodedLength);
                
                _encode = () => LZ4Codec.Encode(_encoderInput, _encoderOutput, lz4CompressionLevel);
                _decode = () => LZ4Codec.Decode(_decoderInput, _decoderOutput);

                if (ReportCompressionRatio)
                {
                    EncodedFileSize = encodedLength;
                }
            }
                break;
            case "PNG":
            {
                void ModifyEncoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Png);
                void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                break;
            }
            case "WEBP":
            {
                void ModifyEncoderSettings(ProcessImageSettings settings)
                {
                    settings.TrySetEncoderFormat(ImageMimeTypes.Webp);
                    
                }

                void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                break;
            }
            case "HEIF":
            {
                void ModifyEncoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Heic);
                void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                break;
            }
            case "JXL":
            {
                void ModifyEncoderSettings(ProcessImageSettings settings)
                {
                    settings.TrySetEncoderFormat(ImageMimeTypes.Jxl);
                    settings.EncoderOptions = new JxlLosslessEncoderOptions { EncodeSpeed = JxlEncodeSpeed.Cheetah };
                }

                void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                break;
            }
            case "AVIF":
            {
                void ModifyEncoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Avif);
                void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private ProcessImageSettings GetDefaultEncoderSettings()
    {
        return new ProcessImageSettings { Sharpen = false, UnsharpMask = UnsharpMaskSettings.None };
    }

    private ProcessImageSettings GetDefaultDecoderSettings()
    {
        return new ProcessImageSettings { Sharpen = false, UnsharpMask = UnsharpMaskSettings.None };
    }

    private Action EncodeWithMagicScaler(Action<ProcessImageSettings> modifyEncoderSettings)
    {
        var encoderSettings = GetDefaultEncoderSettings();
        modifyEncoderSettings(encoderSettings);

        // Run the encoder once so that we know what the encoded length will be
        using var oneTimeOutputStream = new MemoryStream();
        MagicImageProcessor.ProcessImage(_encoderInput, oneTimeOutputStream, encoderSettings);
        Array.Resize(ref _encoderOutput, (int)oneTimeOutputStream.Length);

        if (ReportCompressionRatio)
        {
            EncodedFileSize = oneTimeOutputStream.Length;
        }

        return () =>
        {
            using var encoderOutputStream = new MemoryStream(_encoderOutput, true);
            MagicImageProcessor.ProcessImage(_encoderInput, encoderOutputStream, encoderSettings);
        };
    }

    private Action DecodeWithMagicScaler(
        Action<ProcessImageSettings> modifyEncoderSettings,
        Action<ProcessImageSettings> modifyDecoderSettings)
    {
        var encoderSettings = GetDefaultEncoderSettings();  
        var decoderSettings = GetDefaultDecoderSettings();
        modifyEncoderSettings(encoderSettings);
        modifyDecoderSettings(decoderSettings);
        
        // Run the encoder once so we can use its output as the input for the decoding process
        using var decoderInputMs = new MemoryStream();
        MagicImageProcessor.ProcessImage(_encoderInput!, decoderInputMs, encoderSettings);
        _decoderInput = decoderInputMs.ToArray();
        _decoderOutput = new byte[_encoderInput!.Length];

        return () =>
        {
            using var decoderOutputStream = new MemoryStream(_decoderOutput, true);
            MagicImageProcessor.ProcessImage(_decoderInput, decoderOutputStream, decoderSettings);
        };
    }

    [Benchmark]
    public void Encode()
    {
        _encode?.Invoke();
    }

    [Benchmark]
    public void Decode()
    {
        _decode?.Invoke();
    }
}
