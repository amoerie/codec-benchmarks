﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace JxlSharp
{
	/// <summary>
	/// A simple static class for working with JXL files.
	/// </summary>
	public static class JXL
	{
		/// <summary>
		/// Returns the number of bytes per pixel for the built-in PixelFormat type
		/// </summary>
		/// <param name="pixelFormat">The GDI+ pixel format</param>
		/// <returns>The number of bytes per pixel for that pixel format</returns>
		internal static int GetBytesPerPixel(PixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case PixelFormat.Format16bppArgb1555:
				case PixelFormat.Format16bppGrayScale:
				case PixelFormat.Format16bppRgb555:
				case PixelFormat.Format16bppRgb565:
					return 2;
				case PixelFormat.Format64bppArgb:
				case PixelFormat.Format64bppPArgb:
					return 8;
				case PixelFormat.Format48bppRgb:
					return 6;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
				case PixelFormat.Format32bppRgb:
					return 4;
				case PixelFormat.Format24bppRgb:
					return 3;
				case PixelFormat.Format8bppIndexed:
					return 1;
				case PixelFormat.Format1bppIndexed:
				case PixelFormat.Format4bppIndexed:
					throw new NotSupportedException();
				default:
					throw new NotSupportedException();
			}
		}

		//[ThreadStatic]
		//static JxlDecoder _threadJxlDecoder;
		//static JxlDecoder threadJxlDecoder
		//{
		//	get
		//	{
		//		if (_threadJxlDecoder == null)
		//		{
		//			_threadJxlDecoder = new JxlDecoder();
		//		}
		//		return _threadJxlDecoder;
		//	}
		//}

		/// <summary>
		/// Loads a JXL file and returns a Bitmap.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>Returns a bitmap, or returns null on failure.</returns>
		public static Bitmap LoadImage(string fileName)
		{
			return LoadImage(File.ReadAllBytes(fileName));
		}

		/// <summary>
		/// Returns the basic info for a JXL file
		/// </summary>
		/// <param name="data">The bytes of the JXL file (can be partial data)</param>
		/// <param name="canTranscodeToJpeg">Set to true if the image contains JPEG reconstruction data</param>
		/// <returns>A JxlBasicInfo object describing the image</returns>
		public static JxlBasicInfo GetBasicInfo(byte[] data, out bool canTranscodeToJpeg)
		{
			using (var jxlDecoder = new JxlDecoder())
			{
				JxlBasicInfo basicInfo = null;
				canTranscodeToJpeg = false;
				jxlDecoder.SetInput(data);
				jxlDecoder.SubscribeEvents(JxlDecoderStatus.BasicInfo | JxlDecoderStatus.JpegReconstruction | JxlDecoderStatus.Frame);

				while (true)
				{
					var status = jxlDecoder.ProcessInput();
					if (status == JxlDecoderStatus.BasicInfo)
					{
						status = jxlDecoder.GetBasicInfo(out basicInfo);
						if (status != JxlDecoderStatus.Success)
						{
							return null;
						}
					}
					else if (status == JxlDecoderStatus.JpegReconstruction)
					{
						canTranscodeToJpeg = true;
					}
					else if (status == JxlDecoderStatus.Frame)
					{
						return basicInfo;
					}
					else if (status == JxlDecoderStatus.Success)
					{
						return basicInfo;
					}
					else if (status >= JxlDecoderStatus.Error && status < JxlDecoderStatus.BasicInfo)
					{
						return null;
					}

					else if (status < JxlDecoderStatus.BasicInfo)
					{
						return basicInfo;
					}
				}
			}
		}

		private static void BgrSwap(int width, int height, int bytesPerPixel, IntPtr scan0, int stride)
		{
			unsafe
			{
				if (bytesPerPixel == 3)
				{
					for (int y = 0; y < height; y++)
					{
						byte* p = (byte*)scan0 + stride * y;
						for (int x = 0; x < width; x++)
						{
							byte r = p[2];
							byte b = p[0];
							p[0] = r;
							p[2] = b;
							p += 3;
						}
					}
				}
				else if (bytesPerPixel == 4)
				{
					for (int y = 0; y < height; y++)
					{
						byte* p = (byte*)scan0 + stride * y;
						for (int x = 0; x < width; x++)
						{
							byte r = p[2];
							byte b = p[0];
							p[0] = r;
							p[2] = b;
							p += 4;
						}
					}
				}
			}
		}

		private static void BgrSwap(BitmapData bitmapData)
		{
			int bytesPerPixel = 4;
			switch (bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
				case PixelFormat.Format32bppRgb:
					bytesPerPixel = 4;
					break;
				case PixelFormat.Format24bppRgb:
					bytesPerPixel = 3;
					break;
				default:
					return;
			}
			BgrSwap(bitmapData.Width, bitmapData.Height, bytesPerPixel, bitmapData.Scan0, bitmapData.Stride);
		}
		
		private static void BgrSwap(Bitmap bitmap)
		{
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
			try
			{
				BgrSwap(bitmapData);
			}
			finally
			{
				bitmap.UnlockBits(bitmapData);
			}
		}
		
		private static void SetGrayscalePalette(Bitmap bitmap)
		{
			var palette = bitmap.Palette;
			for (int i = 0; i < 256; i++)
			{
				palette.Entries[i] = Color.FromArgb(i, i, i);
			}
			bitmap.Palette = palette;
		}
		
		/// <summary>
		/// Loads a JXL image into a BitmapData object (SRGB or grayscale only, Image dimensions must match the file)
		/// </summary>
		/// <param name="data">The byte data for the JXL file</param>
		/// <param name="bitmapData">A BitmapData object (from Bitmap.LockBits)</param>
		/// <returns></returns>
		public static bool LoadImageIntoBitmap(byte[] data, BitmapData bitmapData)
		{
			return LoadImageIntoMemory(data, bitmapData.Width, bitmapData.Height, GetBytesPerPixel(bitmapData.PixelFormat), bitmapData.Scan0, bitmapData.Stride, true);
		}

		/// <summary>
		/// Loads a JXL image into a Bitmap object (SRGB or grayscale only, Image dimensions must match the file)
		/// </summary>
		/// <param name="data">The byte data for the JXL file</param>
		/// <param name="bitmap">A Bitmap object</param>
		/// <returns></returns>
		public static bool LoadImageIntoBitmap(byte[] data, Bitmap bitmap)
		{
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
			if (bitmapData.Stride < 0)
			{
				throw new NotSupportedException("Stride can not be negative");
			}
			try
			{
				bool okay = LoadImageIntoBitmap(data, bitmapData);
				if (okay)
				{
					if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						SetGrayscalePalette(bitmap);
					}
				}
				return okay;
			}
			finally
			{
				bitmap.UnlockBits(bitmapData);
			}
		}

		/// <summary>
		/// Loads a JXL file into a locked image buffer  (SRGB or grayscale only, Image dimensions and alpha channel must match the file)
		/// </summary>
		/// <param name="data">The byte data for the JXL file</param>
		/// <param name="width">Width of the buffer (must match the file)</param>
		/// <param name="height">Height of the buffer (must match the file)</param>
		/// <param name="bytesPerPixel">Bytes per pixel (1 = grayscale, 3 = RGB, 4 = RGBA)</param>
		/// <param name="scan0">Pointer to a locked scanline buffer</param>
		/// <param name="stride">Distance between scanlines in the buffer (must be positive)</param>
		/// <param name="doBgrSwap">If true, swaps the red and blue channel.  Required for GDI/GDI+ bitmaps which use BGR byte order.</param>
		/// <returns>True if the image was successfully loaded, otherwise false</returns>
		public static bool LoadImageIntoMemory(byte[] data, int width, int height, int bytesPerPixel, IntPtr scan0, int stride, bool doBgrSwap)
		{
			if (stride < 0) throw new NotSupportedException("Stride can not be negative");
			if (bytesPerPixel < 0 || bytesPerPixel > 4) throw new NotSupportedException("bytesPerPixel must be between 1 and 4");

			JxlBasicInfo basicInfo;
			using (var jxlDecoder = new JxlDecoder())
			{
				jxlDecoder.SetInput(data);
				jxlDecoder.SubscribeEvents(JxlDecoderStatus.BasicInfo | JxlDecoderStatus.Frame | JxlDecoderStatus.FullImage);
				while (true)
				{
					var status = jxlDecoder.ProcessInput();
					if (status == JxlDecoderStatus.BasicInfo)
					{
						status = jxlDecoder.GetBasicInfo(out basicInfo);
						if (status == JxlDecoderStatus.Success)
						{
							if (width != basicInfo.Width || height != basicInfo.Height)
							{
								return false;
							}
						}
						else
						{
							return false;
						}

					}
					else if (status == JxlDecoderStatus.Frame)
					{
						//PixelFormat bitmapPixelFormat = PixelFormat.Format32bppArgb;
						JxlPixelFormat pixelFormat = new JxlPixelFormat();
						pixelFormat.DataType = JxlDataType.UInt8;
						pixelFormat.Endianness = JxlEndianness.NativeEndian;
						pixelFormat.NumChannels = bytesPerPixel;
						
						pixelFormat.Align = stride;
						status = jxlDecoder.SetImageOutBuffer(pixelFormat, scan0, stride * height);
						if (status != JxlDecoderStatus.Success)
						{
							return false;
						}
						status = jxlDecoder.ProcessInput();
						if (status > JxlDecoderStatus.Success && status < JxlDecoderStatus.BasicInfo)
						{
							return false;
						}
						if (doBgrSwap && bytesPerPixel >= 3)
						{
							BgrSwap(width, height, bytesPerPixel, scan0, stride);
						}
						return true;
					}
					else if (status == JxlDecoderStatus.FullImage)
					{
						0.GetHashCode();
					}
					else if (status == JxlDecoderStatus.Success)
					{
						return true;
					}
					else if (status > JxlDecoderStatus.Success && status < JxlDecoderStatus.BasicInfo)
					{
						return false;
					}
				}
			}
		}

		/// <summary>
		/// Suggests a pixel format based on the BasicInfo header
		/// </summary>
		/// <param name="basicInfo">A JxlBasicInfo object describing the image</param>
		/// <returns>Either PixelFormat.Format32bppArgb, PixelFormat.Format24bppRgb, or PixelFormat.Format8bppIndexed</returns>
		public static PixelFormat SuggestPixelFormat(JxlBasicInfo basicInfo)
		{
			bool isColor = basicInfo.NumColorChannels > 1;
			bool hasAlpha = basicInfo.AlphaBits > 0;
			PixelFormat bitmapPixelFormat = PixelFormat.Format32bppArgb;
			if (isColor)
			{
				if (hasAlpha)
				{
					bitmapPixelFormat = PixelFormat.Format32bppArgb;
				}
				else
				{
					bitmapPixelFormat = PixelFormat.Format24bppRgb;
				}
			}
			else
			{
				if (hasAlpha)
				{
					bitmapPixelFormat = PixelFormat.Format32bppArgb;
				}
				else
				{
					bitmapPixelFormat = PixelFormat.Format8bppIndexed;
				}
			}
			return bitmapPixelFormat;
		}
		private static Bitmap CreateBlankBitmap(JxlBasicInfo basicInfo)
		{
			PixelFormat bitmapPixelFormat = SuggestPixelFormat(basicInfo);
			Bitmap bitmap = new Bitmap(basicInfo.Width, basicInfo.Height, bitmapPixelFormat);
			return bitmap;
		}
		/// <summary>
		/// Loads a JXL image as a Bitmap
		/// </summary>
		/// <param name="data">The JXL bytes</param>
		/// <returns>Returns a bitmap on success, otherwise returns null</returns>
		public static Bitmap LoadImage(byte[] data)
		{
			Bitmap bitmap = null;
			JxlBasicInfo basicInfo = GetBasicInfo(data, out _);
			if (basicInfo == null)
			{
				return null;
			}
			bitmap = CreateBlankBitmap(basicInfo);
			if (!LoadImageIntoBitmap(data, bitmap))
			{
				if (bitmap != null)
				{
					bitmap.Dispose();
				}
				return null;
			}
			return bitmap;
		}

		/// <summary>
		/// Transcodes a JXL file back to a JPEG file, only possible if the image was originally a JPEG file.
		/// </summary>
		/// <param name="jxlBytes">File bytes for the JXL file</param>
		/// <returns>The resulting JPEG bytes on success, otherwise returns null</returns>
		public static byte[] TranscodeJxlToJpeg(byte[] jxlBytes)
		{
			byte[] buffer = new byte[0];
			int outputPosition = 0;
			//byte[] buffer = new byte[1024 * 1024];
			using (var jxlDecoder = new JxlDecoder())
			{
				jxlDecoder.SetInput(jxlBytes);
				JxlBasicInfo basicInfo = null;
				bool canTranscodeToJpeg = false;
				jxlDecoder.SubscribeEvents(JxlDecoderStatus.BasicInfo | JxlDecoderStatus.JpegReconstruction | JxlDecoderStatus.Frame | JxlDecoderStatus.FullImage);
				while (true)
				{
					var status = jxlDecoder.ProcessInput();
					if (status == JxlDecoderStatus.BasicInfo)
					{
						status = jxlDecoder.GetBasicInfo(out basicInfo);
					}
					else if (status == JxlDecoderStatus.JpegReconstruction)
					{
						canTranscodeToJpeg = true;
						buffer = new byte[1024 * 1024];
						jxlDecoder.SetJPEGBuffer(buffer, outputPosition);
					}
					else if (status == JxlDecoderStatus.JpegNeedMoreOutput)
					{
						outputPosition += buffer.Length - jxlDecoder.ReleaseJPEGBuffer();
						byte[] nextBuffer = new byte[buffer.Length * 4];
						if (outputPosition > 0)
						{
							Array.Copy(buffer, 0, nextBuffer, 0, outputPosition);
						}
						buffer = nextBuffer;
						jxlDecoder.SetJPEGBuffer(buffer, outputPosition);
					}
					else if (status == JxlDecoderStatus.Frame)
					{
						//if (!canTranscodeToJpeg)
						//{
						//	return null;
						//}
					}
					else if (status == JxlDecoderStatus.Success)
					{
						outputPosition += buffer.Length - jxlDecoder.ReleaseJPEGBuffer();
						byte[] jpegBytes;
						if (buffer.Length == outputPosition)
						{
							jpegBytes = buffer;
						}
						else
						{
							jpegBytes = new byte[outputPosition];
							Array.Copy(buffer, 0, jpegBytes, 0, outputPosition);
						}
						return jpegBytes;
					}
					else if (status == JxlDecoderStatus.NeedImageOutBuffer)
					{
						return null;
					}
					else if (status >= JxlDecoderStatus.Error && status < JxlDecoderStatus.BasicInfo)
					{
						return null;
					}
					else if (status < JxlDecoderStatus.BasicInfo)
					{
						return null;
					}
				}
			}
		}

		/// <summary>
		/// Transcodes a JPEG file to a JXL file
		/// </summary>
		/// <param name="jpegBytes">File bytes for the JPEG file</param>
		/// <returns>The resulting JXL bytes on success, otherwise returns null</returns>
		public static byte[] TranscodeJpegToJxl(byte[] jpegBytes)
		{
			MemoryStream ms = new MemoryStream();
			JxlEncoderStatus status;
			using (var encoder = new JxlEncoder(ms))
			{
				status = encoder.StoreJPEGMetadata(true);
				status = encoder.AddJPEGFrame(encoder.FrameSettings, jpegBytes);
				encoder.CloseFrames();
				encoder.CloseInput();
				status = encoder.ProcessOutput();
				if (status == JxlEncoderStatus.Success)
				{
					return ms.ToArray();
				}
				return null;
			}
		}

		private static void CreateBasicInfo(Bitmap bitmap, out JxlBasicInfo basicInfo, out JxlPixelFormat pixelFormat, out JxlColorEncoding colorEncoding)
		{
			basicInfo = new JxlBasicInfo();
			pixelFormat = new JxlPixelFormat();
			pixelFormat.DataType = JxlDataType.UInt8;
			pixelFormat.Endianness = JxlEndianness.NativeEndian;
			if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb)
			{
				basicInfo.AlphaBits = 8;
				basicInfo.NumColorChannels = 3;
				basicInfo.NumExtraChannels = 1;
				if (bitmap.PixelFormat == PixelFormat.Format32bppPArgb)
				{
					basicInfo.AlphaPremultiplied = true;
				}
				pixelFormat.NumChannels = 4;
			}
			else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
			{
				basicInfo.NumColorChannels = 1;
				pixelFormat.NumChannels = 1;
			}
			else
			{
				basicInfo.NumColorChannels = 3;
				pixelFormat.NumChannels = 3;
			}
			basicInfo.BitsPerSample = 8;
			basicInfo.IntrinsicWidth = bitmap.Width;
			basicInfo.IntrinsicHeight = bitmap.Height;
			basicInfo.Width = bitmap.Width;
			basicInfo.Height = bitmap.Height;
			colorEncoding = new JxlColorEncoding();
			bool isGray = basicInfo.NumColorChannels == 1;
			colorEncoding.SetToSRGB(isGray);

		}

		/// <summary>
		/// Returns an RGB/RGBA byte array with Blue and Red swapped
		/// </summary>
		/// <param name="bitmap">The bitmap to return a copy of</param>
		/// <param name="hasAlpha">True to include an alpha channel</param>
		/// <returns>The image converted to an array (with blue and red swapped)</returns>
		private static byte[] CopyBitmapAndBgrSwap(Bitmap bitmap, bool hasAlpha)
		{
			if (hasAlpha)
			{
				byte[] newBytes = new byte[bitmap.Width * bitmap.Height * 4];
				BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				try
				{
					unsafe
					{
						fixed (byte* pBytes = newBytes)
						{
							for (int y = 0; y < bitmap.Height; y++)
							{
								byte* src = (byte*)bitmapData.Scan0 + bitmapData.Stride * y;
								byte* dest = pBytes + bitmap.Width * 4 * y;
								for (int x = 0; x < bitmap.Width; x++)
								{
									dest[0] = src[2];
									dest[1] = src[1];
									dest[2] = src[0];
									dest[3] = src[3];
									dest += 4;
									src += 4;
								}
							}
						}
					}
				}
				finally
				{
					bitmap.UnlockBits(bitmapData);
				}
				return newBytes;
			}
			else
			{
				byte[] newBytes = new byte[bitmap.Width * bitmap.Height * 3];
				BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				try
				{
					unsafe
					{
						fixed (byte* pBytes = newBytes)
						{
							for (int y = 0; y < bitmap.Height; y++)
							{
								byte* src = (byte*)bitmapData.Scan0 + bitmapData.Stride * y;
								byte* dest = pBytes + bitmap.Width * 3 * y;
								for (int x = 0; x < bitmap.Width; x++)
								{
									dest[0] = src[2];
									dest[1] = src[1];
									dest[2] = src[0];
									dest += 3;
									src += 3;
								}
							}
						}
					}
				}
				finally
				{
					bitmap.UnlockBits(bitmapData);
				}
				return newBytes;
			}
		}


		///// <summary>
		///// Encodes a JXL file losslessly
		///// </summary>
		///// <param name="bitmap">The bitmap to save</param>
		///// <returns>The JXL file bytes</returns>
		//public static byte[] EncodeJxlLossless(Bitmap bitmap)
		//{
		//	JxlEncoderStatus status;
		//	MemoryStream ms = new MemoryStream();
		//	using (var encoder = new JxlEncoder(ms))
		//	{
		//		JxlBasicInfo basicInfo;
		//		JxlPixelFormat pixelFormat;
		//		JxlColorEncoding colorEncoding;
		//		CreateBasicInfo(bitmap, out basicInfo, out pixelFormat, out colorEncoding);
		//		bool hasAlpha = basicInfo.AlphaBits > 0;
		//		byte[] bitmapCopy = CopyBitmapAndBgrSwap(bitmap, hasAlpha);
		//		status = encoder.SetBasicInfo(basicInfo);
		//		status = encoder.SetColorEncoding(colorEncoding);
		//		status = encoder.FrameSettings.SetFrameLossless(true);
		//		status = encoder.AddImageFrame(encoder.FrameSettings, pixelFormat, bitmapCopy);
		//		encoder.CloseFrames();
		//		encoder.CloseInput();
		//		status = encoder.ProcessOutput();
		//		byte[] bytes;
		//		if (status == JxlEncoderStatus.Success)
		//		{
		//			bytes = ms.ToArray();
		//		}
		//		return null;
		//	}
		//}

		/// <summary>
		/// Encodes a JXL file using the settings provided
		/// </summary>
		/// <param name="bitmap">The bitmap to save</param>
		/// <param name="lossyMode">Whether to save lossless, lossy, photo, or drawing</param>
		/// <param name="frameDistance">Sets the distance level for lossy compression<br/>
		/// target max butteraugli distance, lower = higher quality. <br/>
		/// Range: 0 .. 15.<br/>
		/// 0.0 = mathematically lossless (however, use lossless mode instead to use true lossless, 
		/// as setting distance to 0 alone is not the only requirement).<br/>
		/// 1.0 = visually lossless. <br/>
		/// Recommended range: 0.5 .. 3.0. <br/>
		/// Default value: 1.0.</param>
		/// <param name="settings">The settings to save the image with</param>
		/// <returns>The JXL file, or null on failure</returns>
		public static void EncodeJxl(Bitmap bitmap, JxlLossyMode lossyMode, float frameDistance, IDictionary<JxlEncoderFrameSettingId, long> settings, Stream outputStream)
		{
			JxlEncoderStatus status;
			using (var encoder = new JxlEncoder(outputStream))
			{
				JxlBasicInfo basicInfo;
				JxlPixelFormat pixelFormat;
				JxlColorEncoding colorEncoding;
				CreateBasicInfo(bitmap, out basicInfo, out pixelFormat, out colorEncoding);
				bool hasAlpha = basicInfo.AlphaBits > 0;
				byte[] bitmapCopy = CopyBitmapAndBgrSwap(bitmap, hasAlpha);
				if (lossyMode != JxlLossyMode.Lossless)
				{
					basicInfo.UsesOriginalProfile = false;
				}
				status = encoder.SetBasicInfo(basicInfo);
				status = encoder.SetColorEncoding(colorEncoding);
				foreach (var pair in settings)
				{
					status = encoder.FrameSettings.SetOption(pair.Key, pair.Value);
				}
				if (lossyMode == JxlLossyMode.Lossless)
				{
					status = encoder.FrameSettings.SetFrameLossless(true);
					status = encoder.FrameSettings.SetFrameDistance(0);
					status = encoder.FrameSettings.SetOption(JxlEncoderFrameSettingId.Modular, 1); 
				}
				else
				{
					status = encoder.FrameSettings.SetFrameDistance(frameDistance);
					status = encoder.FrameSettings.SetFrameLossless(false);
					if (lossyMode == JxlLossyMode.Photo)
					{
						status = encoder.FrameSettings.SetOption(JxlEncoderFrameSettingId.Modular, 0);
					}
					else if (lossyMode == JxlLossyMode.Drawing)
					{
						status = encoder.FrameSettings.SetOption(JxlEncoderFrameSettingId.Modular, 1);
					}
				}
				status = encoder.AddImageFrame(encoder.FrameSettings, pixelFormat, bitmapCopy);
				encoder.CloseFrames();
				encoder.CloseInput();
				status = encoder.ProcessOutput();

				if (status != JxlEncoderStatus.Success)
				{
					throw new InvalidOperationException("JXL encode failed: " + status);
				}
			}
		}
	}

	/// <summary>
	/// Lossless/Lossy Mode for JXL.EncodeJxl
	/// </summary>
	public enum JxlLossyMode
	{
		/// <summary>
		/// Lossless mode
		/// </summary>
		Lossless = 0,
		/// <summary>
		/// Automatic selection
		/// </summary>
		Default = 1,
		/// <summary>
		/// VarDCT mode (like JPEG)
		/// </summary>
		Photo = 2,
		/// <summary>
		/// Modular Mode for drawn images, not for things that have previously been saved as JPEG.
		/// </summary>
		Drawing = 3,
	}
}