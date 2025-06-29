﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Resource_Manager.Classes.DDT
{
	public class DDTFileDepricated
	{
		public DDTFileDepricated(byte usage, byte alpha, byte format, byte mipmap_levels, ushort width, ushort height, byte[][] images)
		{
			Head = 0x33535452;
			Usage = (DDTFileTypeUsage)usage;
			Alpha = (DDTFileTypeAlpha)alpha;
			Format = (DDTFileTypeFormat)format;
			MipmapLevels = mipmap_levels;
			BaseWidth = width;
			BaseHeight = height;
			var mipmaps = new List<DDTImage>();
			var numImagesPerLevel = Usage.HasFlag(DDTFileTypeUsage.Cube) ? 6 : 1;
			int Offset = 24;
			for (var index = 0; index < MipmapLevels * numImagesPerLevel; ++index)
			{
				mipmaps.Add(new DDTImage(width, height, 16 + 8 * index, images[index].Length, images[index]));
				Offset += images[index].Length + 8;
			}
			Images = new ReadOnlyCollection<DDTImage>(mipmaps);
		}
		public DDTFileDepricated(byte[] data, bool AlphaPart)
		{
			using (var stream = new MemoryStream(data))
			{
				using (var binaryReader = new BinaryReader(stream))
				{
					Head = binaryReader.ReadUInt32();
					Usage = (DDTFileTypeUsage)binaryReader.ReadByte();
					Alpha = (DDTFileTypeAlpha)binaryReader.ReadByte();
					Format = (DDTFileTypeFormat)binaryReader.ReadByte();
					MipmapLevels = binaryReader.ReadByte();
					BaseWidth = (ushort)binaryReader.ReadInt32();
					BaseHeight = (ushort)binaryReader.ReadInt32();
					var images = new List<DDTImage>();
					var numImagesPerLevel = Usage.HasFlag(DDTFileTypeUsage.Cube) ? 6 : 1;
					for (var index = 0; index < MipmapLevels * numImagesPerLevel; ++index)
					{
						binaryReader.BaseStream.Position = 16 + 8 * index;
						var width = BaseWidth >> (index / numImagesPerLevel);
						if (width < 1)
							width = 1;
						var height = BaseHeight >> (index / numImagesPerLevel);
						if (height < 1)
							height = 1;
						var offset = binaryReader.ReadInt32();
						var length = binaryReader.ReadInt32();
						binaryReader.BaseStream.Position = offset;
						images.Add(new DDTImage(width, height, offset, length, binaryReader.ReadBytes(length)));
					}
					Images = new ReadOnlyCollection<DDTImage>(images);
				}
			}
			Bitmap = GetBitmap(AlphaPart);
		}

		public uint Head { get; }
		public DDTFileTypeUsage Usage { get; }
		public DDTFileTypeAlpha Alpha { get; }
		public DDTFileTypeFormat Format { get; }
		public byte MipmapLevels { get; }
		public ushort BaseWidth { get; }
		public ushort BaseHeight { get; }
		public IReadOnlyCollection<DDTImage> Images { get; }

		public string DDTInfo
		{
			get
			{
				return $"{BaseWidth.ToString()}x{BaseHeight.ToString()}, {Format.ToString().ToUpper()}" +
				$"({((byte)Usage).ToString()} {((byte)Alpha).ToString()} {((byte)Format).ToString()})";
			}
		}

		public BitmapSource Bitmap { get; }

		public byte[] ToByteArray()
		{
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms))
				{
					bw.Write(Head);
					bw.Write((byte)Usage);
					bw.Write((byte)Alpha);
					bw.Write((byte)Format);
					bw.Write(MipmapLevels);
					bw.Write((int)BaseWidth);
					bw.Write((int)BaseHeight);
					foreach (var image in Images)
					{
						bw.Write(image.Offset);
						bw.Write(image.Length);

					}
					foreach (var image in Images)
						bw.Write(image.RawData);
					return ms.ToArray();
				}
			}
		}

		private BitmapSource GetBitmap(bool AlphaPart)
		{
			var ddtImage = Images.FirstOrDefault();
			if (ddtImage == null)
				return null;

			byte[] data;
			switch (Format)
			{
				case DDTFileTypeFormat.Bgra:
					data = ddtImage.RawData;
					break;
				case DDTFileTypeFormat.Grey:
					var rawData = ddtImage.RawData;
					data = new byte[ddtImage.Width * ddtImage.Height * 4];
					for (var i = 0; i < ddtImage.Width * ddtImage.Height; i++)
					{
						data[i * 4] =
								data[i * 4 + 1] =
										data[i * 4 + 2] = rawData[i];
						data[i * 4 + 3] = 255;
					}
					break;
				case DDTFileTypeFormat.DXT1DE:
				case DDTFileTypeFormat.DXT1:
					data = DXTFileUtils.DecompressDXT1(ddtImage.RawData, ddtImage.Width, ddtImage.Height);
					break;
				case DDTFileTypeFormat.DXT3:
					data = DXTFileUtils.DecompressDXT3(ddtImage.RawData, ddtImage.Width, ddtImage.Height);
					break;
				case DDTFileTypeFormat.DXT5:
					if (Usage.HasFlag(DDTFileTypeUsage.Bump))
					{
						data = DXTFileUtils.DecompressDXT5Bump(ddtImage.RawData, ddtImage.Width, ddtImage.Height);
					}
					else
					{
						data = DXTFileUtils.DecompressDXT5(ddtImage.RawData, ddtImage.Width, ddtImage.Height);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(Format), Format, null);
			}

			if (AlphaPart)
			{
				byte[] alphaData = new byte[ddtImage.Width * ddtImage.Height * 4];
				if (Alpha == DDTFileTypeAlpha.None)
				{
					alphaData = Enumerable.Repeat((byte)255, alphaData.Length).ToArray();
				}
				else
				{
					for (var i = 0; i < ddtImage.Width * ddtImage.Height; i++)
					{
						alphaData[i * 4] =
								alphaData[i * 4 + 1] =
										alphaData[i * 4 + 2] = 0;
						alphaData[i * 4 + 3] = (byte)(255 - data[i * 4 + 3]);
					}
				}

				byte[] dataWithoutAlpha = data.ToArray();
				for (var i = 0; i < ddtImage.Width * ddtImage.Height; i++)
				{
					dataWithoutAlpha[i * 4 + 3] = 255;
				}

				var image = dataWithoutAlpha.Concat(alphaData);

				return BitmapSource.Create(
					ddtImage.Width,
					ddtImage.Height * 2,
					96,
					96,
					PixelFormats.Pbgra32,
					null,
					image.ToArray(),
					4 * ddtImage.Width
				);
			}
			else
			{
				return BitmapSource.Create(
					ddtImage.Width,
					ddtImage.Height,
					96,
					96,
					PixelFormats.Pbgra32,
					null,
					data,
					4 * ddtImage.Width
				);
			}

		}
	}

	public class DDTImage
	{
		public DDTImage(int width, int height, int offset, int length, byte[] rawData)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException(nameof(rawData));
			}
			if (rawData.Length != length)
			{
				throw new ArgumentOutOfRangeException(nameof(length), length, @"rawData.Length != length");
			}
			Width = width;
			Height = height;
			Offset = offset;
			RawData = rawData;
		}

		public int Width { get; }
		public int Height { get; }
		public int Offset { get; }
		public int Length => RawData?.Length ?? 0;
		public byte[] RawData { get; }
	}
}
