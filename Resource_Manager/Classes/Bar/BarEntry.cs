
using Resource_Manager.Classes.Alz4;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Bar
{
	public class BarEntry : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public static BarEntry Load(BinaryReader binaryReader, uint version, string rootPath)
		{
			BarEntry barEntry = new();

			if (version > 3)
			{
				barEntry.Offset = binaryReader.ReadInt64();
			}
			else
			{
				barEntry.Offset = binaryReader.ReadInt32();
			}

			barEntry.FileSize = binaryReader.ReadInt32();
			barEntry.FileSize2 = binaryReader.ReadInt32();

			if (version >= 5)
			{
				barEntry.FileSize3 = binaryReader.ReadInt32();
			}

			barEntry.LastWriteTime = version < 6 ?
					new BarEntryLastWriteTime(binaryReader) :
					new BarEntryLastWriteTime(DateTime.MinValue);

			var length = binaryReader.ReadUInt32();
			barEntry.FileName = Encoding.Unicode.GetString(binaryReader.ReadBytes((int)length * 2));
			barEntry.FileNameWithRoot = Path.Combine(rootPath, barEntry.FileName);
			if (version > 3)
				barEntry.isCompressed = binaryReader.ReadUInt32();

			// MessageBox.Show(barEntry.FileName);
			return barEntry;
		}

		public static async Task<BarEntry> Create(string rootPath, FileInfo fileInfo, long offset, uint version)
		{
			BarEntry barEntry = new BarEntry();
			barEntry.FileNameWithRoot = Path.GetFileName(rootPath);
			rootPath = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString())
					? rootPath
					: rootPath + Path.DirectorySeparatorChar;

			barEntry.FileName = fileInfo.FullName.Replace(rootPath, string.Empty);
			barEntry.FileNameWithRoot = Path.Combine(barEntry.FileNameWithRoot, barEntry.FileName);
			barEntry.Offset = offset;

			if (version > 3)
			{
				barEntry.isCompressed = 0;
				barEntry.FileSize = (int)fileInfo.Length;
				if (Alz4Utils.IsAlz4File(fileInfo.FullName))
				{
					barEntry.isCompressed = 1;
					barEntry.FileSize = await Alz4Utils.ReadCompressedSizeAlz4Async(fileInfo.FullName);
				}
				barEntry.FileSize2 = (int)fileInfo.Length;
				barEntry.FileSize3 = barEntry.FileSize2;
			}
			else
			{
				barEntry.FileSize = (int)fileInfo.Length;
				barEntry.FileSize2 = barEntry.FileSize;
			}

			barEntry.LastWriteTime = version < 6 ?
					new BarEntryLastWriteTime(fileInfo.LastWriteTimeUtc) :
					new BarEntryLastWriteTime(DateTime.MinValue);

			return barEntry;
		}

		private uint hash { get; set; }
		[JsonPropertyName("compression")]
		public uint isCompressed { get; set; }

		private List<Tuple<string, DateTime>> changes { get; set; } = new List<Tuple<string, DateTime>>();

		[JsonIgnore]
		public List<Tuple<string, DateTime>> Changes
		{
			get
			{
				return changes;
			}
			set
			{
				changes = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged("History");
			}
		}

		private bool isLatestChange { get; set; } = false;
		[JsonIgnore]
		public bool IsLatestChange
		{
			get { return isLatestChange; }
			set
			{
				isLatestChange = value;
				NotifyPropertyChanged();
			}
		}

		[JsonIgnore]
		public string History
		{
			get
			{
				var history = new List<string>();
				history.Add(FileNameWithRoot);
				var changes = Changes.OrderByDescending(t => t.Item2).ToList();

				for (int i = 0; i < changes.Count - 1; i++)
				{
					if (changes[i].Item1 != changes[i + 1].Item1)
					{
						if (changes[i].Item1 == null)
						{
							history.Add($"Removed on {changes[i].Item2.ToString("F")}");
						}
						if (changes[i + 1].Item1 == null)
						{
							history.Add($"Added {changes[i].Item1} on {changes[i].Item2.ToString("F")}");
							if (i == 0)
							{
								IsLatestChange = true;
							}
						}
						if (changes[i].Item1 != null && changes[i + 1].Item1 != null)
						{
							history.Add($"Changed {changes[i + 1].Item1} -> {changes[i].Item1} on {changes[i].Item2.ToString("F")}");

							if (i == 0)
							{
								IsLatestChange = true;
							}
						}

					}
				}

				return string.Join(Environment.NewLine, history);
			}
		}

		[JsonIgnore]
		public string Extension
		{
			get
			{
				return Path.GetExtension(FileName).ToUpper() != "" ? Path.GetExtension(FileName).ToUpper() : "UNKNOWN";
			}
		}
		[JsonIgnore]
		private string FileName { get; set; }
		[JsonPropertyName("file")]
		public string FileNameWithRoot { get; set; }

		[JsonIgnore]
		public long Offset { get; set; }
		[JsonIgnore]
		public int FileSize { get; set; }
		[JsonPropertyName("size")]
		public int FileSize2 { get; set; }
		[JsonIgnore]
		public int FileSize3 { get; set; }
		[JsonIgnore]
		public BarEntryLastWriteTime LastWriteTime { get; set; }
		[JsonIgnore]
		public DateTime lastModifiedDate
		{
			get
			{
				return new DateTime(LastWriteTime.Year, LastWriteTime.Month, LastWriteTime.Day, LastWriteTime.Hour, LastWriteTime.Minute, LastWriteTime.Second, LastWriteTime.Msecond, DateTimeKind.Utc);
			}
		}
		[JsonPropertyName("hash")]
		public string FormattedHash
		{
			get
			{
				return Hash.ToString("X8");
			}
		}

		[JsonIgnore]
		public uint Hash
		{
			get
			{
				return hash;
			}
			set
			{
				hash = value;
				NotifyPropertyChanged();
			}
		}

		[JsonIgnore]
		public string FileFormat
		{
			get
			{
				return (isCompressed != 0 ? "Compressed " : "") + Path.GetExtension(FileName).ToUpper();
			}
		}
		[JsonIgnore]
		public string FileNameWithoutPath
		{
			get
			{
				return Path.GetFileName(FileName);
			}
		}

		public byte[] ToByteArray(uint version)
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			if (version > 3)
			{
				bw.Write(Offset);
			}
			else
			{
				bw.Write(Convert.ToInt32(Offset));
			}
			bw.Write(FileSize);
			bw.Write(FileSize2);

			if (version >= 5)
			{
				bw.Write(FileSize3);
			}
			if (version < 6)
			{
				bw.Write(LastWriteTime.ToByteArray());
			}
			bw.Write(FileName.Length);
			bw.Write(Encoding.Unicode.GetBytes(FileName));
			if (version > 3)
			{
				bw.Write(isCompressed);
			}
			return ms.ToArray();
		}
	}
}
