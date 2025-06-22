using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Resource_Manager.Classes.Bar
{
	public class BarFile
	{
		public async static Task<BarFile> Load(string filename)
		{

			BarFile barFile = new BarFile();
			if (!File.Exists(filename))
				throw new Exception("BAR file does not exist!");


			using var file = File.OpenRead(filename);
			//byte[] asd = await new K4os.Hash.xxHash.XXH32().AsHashAlgorithm().ComputeHashAsync(file);
			//file.Seek(0, SeekOrigin.Begin);
			var reader = new BinaryReader(file);
			barFile.barFileHeader = new BarFileHeader(reader);
			file.Seek(barFile.barFileHeader.FilesTableOffset, SeekOrigin.Begin);
			var rootNameLength = reader.ReadUInt32();
			barFile.RootPath = Encoding.Unicode.GetString(reader.ReadBytes((int)rootNameLength * 2));
			barFile.NumberOfRootFiles = reader.ReadUInt32();

			var barFileEntrys = new List<BarEntry>();
			for (uint i = 0; i < barFile.NumberOfRootFiles; i++)
			{
				barFileEntrys.Add(BarEntry.Load(reader, barFile.barFileHeader.Version, barFile.RootPath));
			}

			using var mem = new MemoryStream();
			var writer = new BinaryWriter(mem);
			foreach (var barEntry in barFileEntrys)
			{

				reader.BaseStream.Seek(barEntry.Offset, SeekOrigin.Begin);
				byte[] data = reader.ReadBytes(barEntry.FileSize2);

				await Task.Run(() =>
				{
					barEntry.Hash = K4os.Hash.xxHash.XXH32.DigestOf(data); //Crc32Algorithm.Compute(data);
				}
				);
				writer.Write(barEntry.Hash);
			}

			uint hash = K4os.Hash.xxHash.XXH32.DigestOf(mem.ToArray());



			string barDirectory = Path.Combine(AppContext.BaseDirectory, "Cached", Path.GetFileNameWithoutExtension(filename));

			string cacheName = Path.Combine(barDirectory, hash.ToString("X8") + ".rmcache");

			Directory.CreateDirectory(barDirectory);

			if (!File.Exists(cacheName))
			{
				var json = JsonSerializer.Serialize(barFileEntrys);
				await File.WriteAllTextAsync(cacheName, json);
			}

			FileInfo currentCacheInfo = new FileInfo(cacheName);

			foreach (var cache in Directory.EnumerateFiles(barDirectory, "*.rmcache"))//.Where(x => x != cacheName))
			{
				FileInfo cacheInfo = new FileInfo(cache);
				if (cacheInfo.CreationTime < currentCacheInfo.CreationTime)
				{
					string json = await File.ReadAllTextAsync(cache);
					var c = JsonSerializer.Deserialize<List<cachedEntry>>(json);
					await Task.Run(() =>
					{
						foreach (var barEntry in barFileEntrys)
						{
							barEntry.Changes.Add(new Tuple<string, DateTime>(barEntry.formattedHash, currentCacheInfo.CreationTime));
							var h = c.FirstOrDefault(x => barEntry.FileNameWithRoot.ToLower() == x.file.ToLower());

							if (h == null)
							{
								barEntry.Changes.Add(new Tuple<string, DateTime>(null, cacheInfo.CreationTime));
							}
							else
							{
								barEntry.Changes.Add(new Tuple<string, DateTime>(h.hash, cacheInfo.CreationTime));
							}
						}
					});

				}
			}

			barFile.BarFileEntrys = new ReadOnlyCollection<BarEntry>(barFileEntrys);
			return barFile;
		}

		public static async Task<BarFile> Create(string root, uint version, string filename)
		{
			BarFile barFile = new BarFile();

			if (!Directory.Exists(root))
				throw new Exception("Directory does not exist!");

			var fileInfos = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
									.Select(fileName => new FileInfo(fileName)).ToArray();


			//Directory.GetParent(root);
			//if (root.EndsWith(Path.DirectorySeparatorChar.ToString()))
			//    root = root[0..^1];


			using (var fileStream = File.Open(Path.Combine(Directory.GetParent(root).FullName, filename + ".bar"), FileMode.Create, FileAccess.Write, FileShare.None))
			{
				using (var writer = new BinaryWriter(fileStream))
				{
					//Write Bar Header
					var header = new BarFileHeader(fileInfos, version, filename + ".bar");
					writer.Write(header.ToByteArray());

					if (version > 3)
						writer.Write(0);
					//    writer.Write(0x7FF7);

					//Write Files
					var barEntrys = new List<BarEntry>();
					foreach (var file in fileInfos)
					{
						var filePath = file.FullName;
						var entry = await BarEntry.Create(root, file, (int)writer.BaseStream.Position, version);

						var data = await File.ReadAllBytesAsync(filePath);
						await Task.Run(() =>
						{
							entry.Hash = K4os.Hash.xxHash.XXH32.DigestOf(data);
						}
						);
						writer.Write(data);

						barEntrys.Add(entry);
					}

					barFile.barFileHeader = header;
					barFile.RootPath = Path.GetFileName(root) + Path.DirectorySeparatorChar;
					barFile.NumberOfRootFiles = (uint)barEntrys.Count;
					barFile.BarFileEntrys = new ReadOnlyCollection<BarEntry>(barEntrys);

					writer.Write(barFile.ToByteArray(version));
				}
			}

			return barFile;
		}
		public BarFileHeader barFileHeader { get; set; }

		public string RootPath { get; set; }

		public uint NumberOfRootFiles { get; set; }

		public IReadOnlyCollection<BarEntry> BarFileEntrys { get; set; }

		public byte[] ToByteArray(uint version)
		{
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms))
				{
					bw.Write(RootPath.Length);
					bw.Write(Encoding.Unicode.GetBytes(RootPath));
					bw.Write(NumberOfRootFiles);
					foreach (var barFileEntry in BarFileEntrys)
						bw.Write(barFileEntry.ToByteArray(version));
					return ms.ToArray();
				}
			}
		}
	}
}
