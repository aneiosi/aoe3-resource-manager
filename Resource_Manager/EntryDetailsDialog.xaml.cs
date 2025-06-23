using Resource_Manager.Classes.Alz4;
using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.DDT;
using Resource_Manager.Classes.L33TZip;

namespace Resource_Manager
{
	/// <summary>
	/// Interaction logic for EntryDetailsDialog.xaml
	/// </summary>
	public partial class EntryDetailsDialog : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public BarEntry entry { get; set; }

		private string headerText;
		public string HeaderText
		{
			get
			{
				return headerText;
			}
			set
			{
				headerText = value;
				NotifyPropertyChanged();
			}
		}

		private int header;
		public int Header
		{
			get
			{
				return header;
			}
			set
			{
				header = value;
				NotifyPropertyChanged();
			}
		}

		private DDT previewDDT;
		public DDT PreviewDDT
		{
			get { return previewDDT; }
			set
			{
				previewDDT = value;
				NotifyPropertyChanged();
			}
		}

		private string ddtUsage;
		public string DDTUsage
		{
			get
			{
				return ddtUsage;
			}
			set
			{
				ddtUsage = value;
				NotifyPropertyChanged();
			}
		}

		private string ddtAlpha;
		public string DDTAlpha
		{
			get
			{
				return ddtAlpha;
			}
			set
			{
				ddtAlpha = value;
				NotifyPropertyChanged();
			}
		}

		private string ddtFormat;
		public string DDTFormat
		{
			get
			{
				return ddtFormat;
			}
			set
			{
				ddtFormat = value;
				NotifyPropertyChanged();
			}
		}

		static IEnumerable<Enum> GetFlags(Enum input)
		{
			foreach (Enum value in Enum.GetValues(input.GetType()))
				if (input.HasFlag(value))
					yield return value;
		}

		public async void GetCustomValues(string filename)
		{
			if (!File.Exists(filename))
				throw new Exception("BAR file does not exist!");
			using var file = File.OpenRead(filename);
			var reader = new BinaryReader(file);

			reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
			byte[] data = reader.ReadBytes(entry.FileSize2);
			reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
			HeaderText = new string(reader.ReadChars(4));
			reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
			Header = reader.ReadInt32();
			if (entry.Extension == ".DDT")
			{
				if (Alz4Utils.IsAlz4File(data))
				{
					data = await Alz4Utils.ExtractAlz4BytesAsync(data);
				}
				else
				{
					if (L33TZipUtils.IsL33TZipFile(data))
						data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
				}

				var ddt = new DDT();
				await ddt.Create(data);
				PreviewDDT = ddt;
				var flagList = new List<string>();
				if (PreviewDDT.Usage.HasFlag(DDTFileTypeUsage.AlphaTest))
				{
					flagList.Add(DDTFileTypeUsage.AlphaTest.ToString());
				}
				if (PreviewDDT.Usage.HasFlag(DDTFileTypeUsage.LowDetail))
				{
					flagList.Add(DDTFileTypeUsage.LowDetail.ToString());
				}
				if (PreviewDDT.Usage.HasFlag(DDTFileTypeUsage.Bump))
				{
					flagList.Add(DDTFileTypeUsage.Bump.ToString());
				}
				if (PreviewDDT.Usage.HasFlag(DDTFileTypeUsage.Cube))
				{
					flagList.Add(DDTFileTypeUsage.Cube.ToString());
				}
				if (flagList.Count > 0)
					DDTUsage = ((byte)PreviewDDT.Usage).ToString() + " (" + string.Join('+', flagList) + ")";
				else
					DDTUsage = ((byte)PreviewDDT.Usage).ToString();
				DDTAlpha = ((byte)PreviewDDT.Alpha).ToString() + " (" + PreviewDDT.Alpha.ToString() + ")";
				DDTFormat = ((byte)PreviewDDT.Format).ToString() + " (" + PreviewDDT.Format.ToString() + ")";
				gpDDT.Visibility = Visibility.Visible;
			}

		}
		public EntryDetailsDialog(BarEntry e, string BarPath)
		{
			entry = e;

			InitializeComponent();
			GetCustomValues(BarPath);
			DataContext = this;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
