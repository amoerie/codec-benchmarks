using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libjxl;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using IImageEncoder = SixLabors.ImageSharp.Formats.IImageEncoder;

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
    private List<IDisposable>? _disposables;

    [Params("MagicScaler", "ImageSharp", Priority = 0)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "This is a Benchmark parameter")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "This is a Benchmark parameter")]
    public string? Library { get; set; }
    

    [Params("BMP", "PNG", "WEBP", "JXL", Priority = 1)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "This is a Benchmark parameter")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "This is a Benchmark parameter")]
    public string? Format { get; set; }

    [Params("MR", /*"CT"*/"CR", Priority = 2)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "This is a Benchmark parameter")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "This is a Benchmark parameter")]
    public string? File { get; set; }

    [Params("BestSpeed", "BestCompression", Priority = 3)]
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

    public byte[]? EncoderInput => _encoderInput;
    public byte[]? DecoderOutput => _decoderOutput;

    /// <summary>
    /// Global setup is run once for every combination of parameters
    /// We (ab)use this to setup an _encode and _decode func so we only have 2 benchmark methods
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _disposables = new List<IDisposable>();
        switch (Library)
        {
            case "MagicScaler":
            {
                switch (Format)
                {
                    case "BMP":
                    {
                        void ModifyEncoderSettings(ProcessImageSettings settings)
                        {
                            settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);
                        }

                        void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                        _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                        break;
                    }
                    case "PNG":
                    {
                        void ModifyEncoderSettings(ProcessImageSettings settings)
                        {
                            settings.TrySetEncoderFormat(ImageMimeTypes.Png);
                        }

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
                    case "JXL":
                    {
                        JxlEncodeSpeed encodeSpeed;
                        switch (CompressionLevel)
                        {
                            case "BestSpeed":
                                encodeSpeed = JxlEncodeSpeed.Cheetah;
                                break;
                            case "BestCompression":
                                encodeSpeed = JxlEncodeSpeed.Wombat;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        void ModifyEncoderSettings(ProcessImageSettings settings)
                        {
                            settings.TrySetEncoderFormat(ImageMimeTypes.Jxl);
                            settings.EncoderOptions = new JxlLosslessEncoderOptions
                            {
                                EncodeSpeed = encodeSpeed,
                                DecodeSpeed = JxlDecodeSpeed.Fastest
                            };
                        }

                        void ModifyDecoderSettings(ProcessImageSettings settings) => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                        _encode = EncodeWithMagicScaler(ModifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(ModifyEncoderSettings, ModifyDecoderSettings);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            }
            case "ImageSharp":
            {
                var rawEncoder = new BmpEncoder();
                switch (Format)
                {
                    case "BMP":
                    {
                        var encoder = new BmpEncoder();
                        _encode = EncodeWithImageSharp(encoder);
                        _decode = DecodeWithImageSharp(encoder, rawEncoder);
                        break;
                    }
                    case "PNG":
                    {
                        PngCompressionLevel compressionLevel;
                        switch (CompressionLevel)
                        {
                            case "BestSpeed":
                                compressionLevel = PngCompressionLevel.BestSpeed;
                                break;
                            case "BestCompression":
                                compressionLevel = PngCompressionLevel.BestCompression;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        var encoder = new PngEncoder
                        {
                            CompressionLevel = compressionLevel
                        };
                        _encode = EncodeWithImageSharp(encoder);
                        _decode = DecodeWithImageSharp(encoder, rawEncoder);
                        break;
                    }
                    case "WEBP":
                    {
                        WebpEncodingMethod webpEncodingMethod;
                        switch (CompressionLevel)
                        {
                            case "BestSpeed":
                                webpEncodingMethod = WebpEncodingMethod.Fastest;
                                break;
                            case "BestCompression":
                                webpEncodingMethod = WebpEncodingMethod.BestQuality;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        var encoder = new WebpEncoder
                        {
                            FileFormat = WebpFileFormatType.Lossless,
                            Method = webpEncodingMethod
                        };
                        _encode = EncodeWithImageSharp(encoder);
                        _decode = DecodeWithImageSharp(encoder, rawEncoder);
                        break;
                    }
                    case "JXL":
                    {
                        // Not supported
                        _encode = () => { };
                        _decode = () => { };
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            }
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (_disposables == null)
        {
            return;
        }
        
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
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
        // Prepare raw file in BMP format 
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
        var encoderSettings = GetDefaultEncoderSettings();
        modifyEncoderSettings(encoderSettings);

        // Run the encoder once so that we know what the encoded length will be
        using var oneTimeOutputStream = new MemoryStream();
        MagicImageProcessor.ProcessImage(_encoderInput, oneTimeOutputStream, encoderSettings);
        _encoderOutput = new byte[oneTimeOutputStream.Length];
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

    private Action EncodeWithImageSharp(IImageEncoder encoder)
    {
        // Prepare raw file in BMP format 
        var file = new FileInfo($"./TestData/{File}.png");
        using var fileMs = new MemoryStream();
        using var fileAsImage = Image.Load(file.FullName);
        fileAsImage.SaveAsBmp(fileMs);
        _encoderInput = fileMs.ToArray();

        if (ReportCompressionRatio)
        {
            OriginalFileSize = _encoderInput.Length;
        }
        
        // Run the encoder once so that we know what the encoded length will be
        using var oneTimeOutputStream = new MemoryStream();
        var image = Image.Load(_encoderInput);
        image.Save(oneTimeOutputStream, encoder);
        _encoderOutput = new byte[oneTimeOutputStream.Length];

        if (ReportCompressionRatio)
        {
            EncodedFileSize = oneTimeOutputStream.Length;
        }

        _disposables?.Add(image);

        return () =>
        {
            using var encoderOutputStream = new MemoryStream(_encoderOutput, true);
            image.Save(encoderOutputStream, encoder);
        };
    }

    private Action DecodeWithImageSharp(IImageEncoder encoder, IImageEncoder rawEncoder)
    {
        // Run the encoder once so we can use its output as the input for the decoding process
        using var decoderInputMs = new MemoryStream();
        using var image = Image.Load(_encoderInput);
        image.Save(decoderInputMs, encoder);
        _decoderInput = decoderInputMs.ToArray();
        _decoderOutput = new byte[_encoderInput!.Length];

        return () =>
        {
            using var decoderOutputStream = new MemoryStream(_decoderOutput, true);
            using var decodedImage = Image.Load(_decoderInput);
            decodedImage.Save(decoderOutputStream, rawEncoder);
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
