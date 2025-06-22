using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

namespace Resource_Manager
{
	/// <summary>
	/// Interaction logic for ExportDDT.xaml
	/// </summary>
	public partial class ExtractDialog : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		public string Path { get; set; } = "";

		private Color overlayColor = Color.White;
		private bool autoDecompress = true;
		private bool oneFolder = false;
		private bool savePNGasBMP = false;
		private bool autoXMBConversion = false;
		private bool autoJSONConversion = false;
		private bool autoDDTToPNGConversion = false;
		private bool autoPNGToWEBPConversion = false;
		private bool autoDDTToTGAConversion = true;
		private bool compressPNG = false;

		public bool CompressPNG
		{
			get
			{
				return compressPNG;
			}
			set
			{
				compressPNG = value;
				NotifyPropertyChanged();
			}
		}
		public Color OverlayColor
		{
			get
			{
				return overlayColor;
			}
			set
			{
				overlayColor = value;
				NotifyPropertyChanged();
			}
		}
		public bool AutoXMBConversion
		{
			get
			{
				return autoXMBConversion;
			}
			set
			{
				autoXMBConversion = value;
				NotifyPropertyChanged();
			}
		}

		public bool AutoJSONConversion
		{
			get
			{
				return autoJSONConversion;
			}
			set
			{
				autoJSONConversion = value;
				NotifyPropertyChanged();
			}
		}

		public bool AutoDDTToPNGConversion
		{
			get
			{
				return autoDDTToPNGConversion;
			}
			set
			{
				autoDDTToPNGConversion = value;
				NotifyPropertyChanged();
			}
		}

		public bool AutoPNGToWEBPConversion
		{
			get
			{
				return autoPNGToWEBPConversion;
			}
			set
			{
				autoPNGToWEBPConversion = value;
				NotifyPropertyChanged();
			}
		}

		public bool AutoDDTToTGAConversion
		{
			get
			{
				return autoDDTToTGAConversion;
			}
			set
			{
				autoDDTToTGAConversion = value;
				NotifyPropertyChanged();
			}
		}

		public bool AutoDecompress
		{
			get
			{
				return autoDecompress;
			}
			set
			{
				autoDecompress = value;
				NotifyPropertyChanged();
			}
		}

		public bool SavePNGasBMP
		{
			get
			{
				return savePNGasBMP;
			}
			set
			{
				savePNGasBMP = value;
				NotifyPropertyChanged();
			}
		}

		public bool OneFolder
		{
			get
			{
				return oneFolder;
			}
			set
			{
				oneFolder = value;
				NotifyPropertyChanged();
			}
		}

		public ExtractDialog(string DefaultRootPath)
		{
			InitializeComponent();
			ExportPath.Text = Directory.Exists(Settings.Default.lastExportedPath) ? Settings.Default.lastExportedPath : DefaultRootPath;

			OverlayColor = Settings.Default.ExtractionOverlayColor;
			AutoDecompress = Settings.Default.ExtractionAutoDecompress;
			OneFolder = Settings.Default.ExtractionOneFolder;
			CompressPNG = Settings.Default.ExtractionCompressPNG;
			SavePNGasBMP = Settings.Default.ExtractionSavePNGasBMP;
			AutoXMBConversion = Settings.Default.ExtractionAutoXMBConversion;
			AutoJSONConversion = Settings.Default.ExtractionAutoJSONConversion;

			AutoPNGToWEBPConversion = Settings.Default.ExtractionAutoPNGToWEBPConversion;
			AutoDDTToPNGConversion = Settings.Default.ExtractionAutoDDTToPNGConversion;
			AutoDDTToTGAConversion = Settings.Default.ExtractionAutoDDTToTGAConversion;
			ColorPicker.SelectedColor = System.Windows.Media.Color.FromRgb(OverlayColor.R, OverlayColor.G, OverlayColor.B);
			DataContext = this;
		}

		private void NavigateButton_Click(object sender, RoutedEventArgs e)
		{
			using (FolderBrowserDialog dialog = new FolderBrowserDialog())
			{
				DialogResult result = dialog.ShowDialog();

				if (result == System.Windows.Forms.DialogResult.OK)
				{
					ExportPath.Text = dialog.SelectedPath;
					Settings.Default.lastExportedPath = dialog.SelectedPath;
					Settings.Default.Save();
				}
			}
		}

		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			if (Directory.Exists(ExportPath.Text))
			{
				Path = ExportPath.Text;
				Settings.Default.ExtractionAutoPNGToWEBPConversion = AutoPNGToWEBPConversion;
				Settings.Default.ExtractionCompressPNG = CompressPNG;
				Settings.Default.ExtractionOverlayColor = OverlayColor;
				Settings.Default.ExtractionAutoDecompress = AutoDecompress;
				Settings.Default.ExtractionOneFolder = OneFolder;
				Settings.Default.ExtractionSavePNGasBMP = SavePNGasBMP;
				Settings.Default.ExtractionAutoXMBConversion = AutoXMBConversion;
				Settings.Default.ExtractionAutoJSONConversion = AutoJSONConversion;
				Settings.Default.ExtractionAutoDDTToPNGConversion = AutoDDTToPNGConversion;
				Settings.Default.ExtractionAutoDDTToTGAConversion = AutoDDTToTGAConversion;
				Settings.Default.Save();
				DialogResult = true;
			}
			else
				System.Windows.Forms.MessageBox.Show("The path " + ExportPath.Text + "doesn't exist. Select valid path.");
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private void ColorPicker_ColorChanged(object sender, RoutedEventArgs e)
		{
			OverlayColor = System.Drawing.Color.FromArgb(
ColorPicker.SelectedColor.A, ColorPicker.SelectedColor.R, ColorPicker.SelectedColor.G, ColorPicker.SelectedColor.B);
		}
	}
}
