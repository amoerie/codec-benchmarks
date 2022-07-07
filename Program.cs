using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using K4os.Compression.LZ4;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libjxl;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dobco.PACSONWEB3.Benchmarks.NetCore.Compression
{
    /// <summary>
    /// These benchmarks exist to compare compression algorithms
    /// </summary>
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class CompressionAlgorithmBenchmarks
    {
        class Config : ManualConfig
        {
            public Config()
            {
                AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()));
                AddJob(DefaultConfig.Instance.GetJobs().ToArray());
                AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
                AddExporter(DefaultConfig.Instance.GetExporters().ToArray());
                AddDiagnoser(DefaultConfig.Instance.GetDiagnosers().ToArray());
                AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
                AddValidator(DefaultConfig.Instance.GetValidators().ToArray());

                AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
                AddColumn(new CompressionRatioColumn());
            }
        }
        
        class CompressionRatioColumn : IColumn
        {
            public string GetValue(Summary summary, BenchmarkCase benchmark)
            {
                if (benchmark.Descriptor.WorkloadMethod.Name != nameof(Encode))
                    return "N/A";
                
                var type = benchmark.Descriptor.Type;
                var instance = Activator.CreateInstance(type);

                type.GetProperty(nameof(ReportCompressionRatio), BindingFlags.Public | BindingFlags.Instance)
                    !.SetValue(instance, true);

                foreach (var parameter in benchmark.Parameters.Items)
                {
                    type.GetProperty(parameter.Name, BindingFlags.Public | BindingFlags.Instance)
                        !.SetValue(instance, parameter.Value);
                }
                
                benchmark.Descriptor.GlobalSetupMethod.Invoke(instance, Array.Empty<object>());

                var originalFileSize = (double) type.GetProperty(nameof(OriginalFileSize), BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance)!;
                var encodedFileSize = (double) type.GetProperty(nameof(EncodedFileSize), BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance)!;
                var compressionRatio = 100 * encodedFileSize / originalFileSize;

                var originalFileSizeKb = originalFileSize / 1024;
                var encodedFileSizeKb = encodedFileSize / 1024;

                return $"{originalFileSizeKb:0}Kb -> {encodedFileSizeKb:0}Kb ({compressionRatio:0.0}%)";
            }   

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
            public bool IsAvailable(Summary summary) => true;
            
            public string Id => nameof(CompressionRatioColumn);
            public string ColumnName => "Compression Ratio";
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Params;
            public int PriorityInCategory => 3;
            public bool IsNumeric => false;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => "Compression Ratio";
            public override string ToString() => ColumnName;
        }
        
        private byte[] _encoderInput;

        private Action _encode;
        private Action _decode;
        private byte[] _encoderOutput;
        
        
        [Params("Echo", Priority = 0)]
        public string File { get; set; }
        
        [Params("LZ4", "PNG", "WEBP",/*"HEIF doesn't work yet",*/"JPEG-XL","AVIF", Priority = 1)]
        public string Format { get; set; }

        [Params("BestSpeed"/*, "BestCompression"*/, Priority = 2)]
        public string CompressionLevel { get; set; }
        
        public bool ReportCompressionRatio { get; set; }

        public double OriginalFileSize { get; set; }
        public double EncodedFileSize { get; set; }
        
        [GlobalSetup]
        public void GlobalSetup()
        {
            {
                var file = new FileInfo($"./TestData/{File}.png");
                var decodeToRawSettings = GetDefaultDecoderSettings();
                decodeToRawSettings.TrySetEncoderFormat(ImageMimeTypes.Bmp);

                using var sampleFileMs = new MemoryStream();
                MagicImageProcessor.ProcessImage(file.FullName, sampleFileMs, decodeToRawSettings);
                _encoderInput = sampleFileMs.ToArray();
            }

            LZ4Level lz4CompressionLevel;
            switch (CompressionLevel)
            {
                case "BestSpeed":
                    lz4CompressionLevel = LZ4Level.L00_FAST;
                    break;
                case "Low":
                    lz4CompressionLevel = LZ4Level.L03_HC;
                    break;
                case "High":
                    lz4CompressionLevel = LZ4Level.L10_OPT;
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
                        var maxLength = LZ4Codec.MaximumOutputSize(_encoderInput.Length);
                        if (ReportCompressionRatio)
                        {
                            // Run LZ4 once to report compression ratio
                            var encodedLength = LZ4Codec.Encode(_encoderInput, _encoderOutput, lz4CompressionLevel);

                            OriginalFileSize = _encoderInput.Length;
                            EncodedFileSize = encodedLength;
                        }
                        _encode = () => LZ4Codec.Encode(_encoderInput, _encoderOutput, lz4CompressionLevel);
                        
                        var decoderInput = new byte[maxLength];
                        var decoderOutput = new byte[_encoderInput.Length];
                        var length = LZ4Codec.Encode(_encoderInput, decoderInput, lz4CompressionLevel);
                        Array.Resize(ref decoderInput, length);
                        _decode = () => LZ4Codec.Decode(decoderInput, decoderOutput);
                    }
                    break;
                case "PNG":
                    {
                        Action<ProcessImageSettings> modifyEncoderSettings = settings =>
                        {
                            settings.TrySetEncoderFormat(ImageMimeTypes.Png);
                        };
                        Action<ProcessImageSettings> modifyDecoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);
                        
                        _encode = EncodeWithMagicScaler(modifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(modifyEncoderSettings, modifyDecoderSettings);
                        break;
                    }
                case "WEBP":
                    {
                        Action<ProcessImageSettings> modifyEncoderSettings = settings =>
                        {
                            settings.TrySetEncoderFormat(ImageMimeTypes.Webp);
                        };
                        Action<ProcessImageSettings> modifyDecoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);
                        
                        _encode = EncodeWithMagicScaler(modifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(modifyEncoderSettings, modifyDecoderSettings);
                        break;
                    }
                case "HEIF":
                    {
                        Action<ProcessImageSettings> modifyEncoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Heic);
                        Action<ProcessImageSettings> modifyDecoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);
                        
                        _encode = EncodeWithMagicScaler(modifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(modifyEncoderSettings, modifyDecoderSettings);
                        break;
                    }
                case "JPEG-XL":
                    {
                        Action<ProcessImageSettings> modifyEncoderSettings = settings =>
                        {
                            settings.TrySetEncoderFormat(ImageMimeTypes.Jxl);
                            settings.EncoderOptions = new JxlLosslessEncoderOptions
                            {
                                EncodeSpeed = JxlEncodeSpeed.Cheetah
                            };
                        };
                        Action<ProcessImageSettings> modifyDecoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);
                        
                        _encode = EncodeWithMagicScaler(modifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(modifyEncoderSettings, modifyDecoderSettings);
                        break;
                    }
                case "AVIF":
                    {
                        Action<ProcessImageSettings> modifyEncoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Avif);
                        Action<ProcessImageSettings> modifyDecoderSettings = settings => settings.TrySetEncoderFormat(ImageMimeTypes.Bmp);
                        
                        _encode = EncodeWithMagicScaler(modifyEncoderSettings);
                        _decode = DecodeWithMagicScaler(modifyEncoderSettings, modifyDecoderSettings);
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
                OriginalFileSize = _encoderInput.Length;
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
            using var decoderInputMs = new MemoryStream();
            var encoderSettings = GetDefaultEncoderSettings();
            modifyEncoderSettings(encoderSettings);
            MagicImageProcessor.ProcessImage(_encoderInput, decoderInputMs, encoderSettings);
            var decoderInput = decoderInputMs.ToArray();
            var decoderOutput = new byte[_encoderInput.Length];
            var decoderSettings = GetDefaultDecoderSettings();
            modifyDecoderSettings(decoderSettings);

            return () =>
            {
                using var decoderOutputStream = new MemoryStream(decoderOutput, true);
                MagicImageProcessor.ProcessImage(decoderInput, decoderOutputStream, decoderSettings);
            };
        }

        [Benchmark]
        public void Encode()
        {
            _encode();
        }

        [Benchmark]
        public void Decode()
        {
            _decode();
        }
    }
}