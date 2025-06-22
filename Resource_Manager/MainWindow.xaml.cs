using Archive_Unpacker.Classes.BarViewModel;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Sample;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using NAudio.Wave;
using Newtonsoft.Json;
using Resource_Manager.Classes.Alz4;
using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.Commands;
using Resource_Manager.Classes.DDT;
using Resource_Manager.Classes.L33TZip;
using Resource_Manager.Classes.Sort;
using Resource_Manager.Classes.XMB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
public class RecentFile
{
	public string Title { get; set; }
	public string FileName { get; set; }
	public ICommand OnClickCommand { get; set; }
}

namespace Resource_Manager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region Variables
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ObservableCollection<RecentFile> recentFiles { get; set; } = new ObservableCollection<RecentFile>();

		private string fileContent;
		public string FileContent
		{
			get
			{
				return fileContent;
			}
			set
			{
				fileContent = value;
				NotifyPropertyChanged();
			}
		}

		public BarViewModel file { get; set; }

		private long selectedSize;
		public long SelectedSize
		{
			get
			{
				return selectedSize;
			}
			set
			{
				selectedSize = value;
				NotifyPropertyChanged();
			}
		}

		private double _zoomValue = 1.0;

		public int ZoomValue
		{
			get
			{
				return (int)(_zoomValue * 100);
			}
		}

		CancellationTokenSource CancelTokenSource;
		CancellationToken Token;

		private GridViewColumnHeader listViewSortCol = null;
		private SortAdorner listViewSortAdorner = null;
		#endregion
		int Version = 50;
		public MainWindow()
		{

			InitializeComponent();
			SearchPanel.Install(XMLViewer);
			DataContext = this;
			tempColumn = (files.View as GridView).Columns[3];

			files.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);

			for (int i = 0; i < Math.Min(10, Settings.Default.RecentFiles.Count); i++)
				recentFiles.Add(new RecentFile() { FileName = Settings.Default.RecentFiles[i], Title = Path.GetFileName(Settings.Default.RecentFiles[i]), OnClickCommand = new RelayCommand<string>(openFile) });

			if (File.Exists(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt")))
			{
				int counter = Convert.ToInt32(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt")));
				if (counter < Version)
				{
					File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt"), Version.ToString());
					ReleaseNotes window = new();
					window.ShowDialog();
				}
			}
			else
			{
				File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt"), Version.ToString());
				ReleaseNotes window = new ReleaseNotes();
				window.ShowDialog();
			}
		}

		FoldingManager foldingManager;
		private WaveOutEvent outputDevice;
		private Mp3FileReader mp3File;
		private WaveFileReader waveFile;

		private async void files_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//files.IsEnabled = false;
			e.Handled = true;
			try
			{
				var entry = files.SelectedItem as BarEntry;
				if (entry == null)
				{
					ImageViewer.Visibility = Visibility.Collapsed;
					XMLViewer.Visibility = Visibility.Collapsed;
					//files.IsEnabled = true;
					return;
				}
				var entries = files.SelectedItems.Cast<BarEntry>().ToList();
				SelectedSize = entries.Sum(x => (long)x.FileSize2);
				await file.readFile(entry);
				if (outputDevice != null)
				{
					outputDevice.Dispose();
					outputDevice = null;
				}
				if (mp3File != null)
				{
					mp3File.Dispose();
					mp3File = null;
				}
				if (waveFile != null)
				{
					waveFile.Dispose();
					waveFile = null;
				}
				point = new Point();
				validPoint = false;
				ImagePreview.RenderTransform = new TranslateTransform();

				if (file.Preview != null)
				{
					XMLViewer.Text = file.Preview.Text;
					if (foldingManager != null)
					{
						FoldingManager.Uninstall(foldingManager);
						foldingManager = null;
					}
					foldingManager = FoldingManager.Install(XMLViewer.TextArea);
					if (entry.Extension == ".XMB" || entry.Extension == ".XML" || entry.Extension == ".SHP" || entry.Extension == ".LGT" || entry.Extension == ".TXT" || entry.Extension == ".CFG" || entry.Extension == ".XAML" || entry.Extension == ".PY")
					{
						var foldingStrategy = new XmlFoldingStrategy();
						foldingStrategy.UpdateFoldings(foldingManager, XMLViewer.Document);
					}
					else
					if (entry.Extension == ".XS")
					{
						var foldingStrategy = new BraceFoldingStrategy();
						foldingStrategy.UpdateFoldings(foldingManager, XMLViewer.Document);
					}
				}

				if (entry.Extension == ".WAV")
				{

					using (outputDevice = new WaveOutEvent())
					using (waveFile = new WaveFileReader(file.Audio))
					{
						outputDevice.Init(waveFile);
						outputDevice.Play();
						while (outputDevice.PlaybackState == PlaybackState.Playing)
						{
							await Task.Delay(500);
						}
					}
					ImageViewer.Visibility = Visibility.Collapsed;
					XMLViewer.Visibility = Visibility.Collapsed;
				}
				if (entry.Extension == ".MP3")
				{
					//var w = new WaveFormat(new BinaryReader(file.audio));
					//MessageBox.Show(w.Encoding.ToString());
					using (outputDevice = new WaveOutEvent())
					using (mp3File = new Mp3FileReader(file.Audio))
					{
						outputDevice.Init(mp3File);
						outputDevice.Play();
						while (outputDevice.PlaybackState == PlaybackState.Playing)
						{
							await Task.Delay(500);
						}
					}
					ImageViewer.Visibility = Visibility.Collapsed;
					XMLViewer.Visibility = Visibility.Collapsed;
				}
				else
				if (entry.Extension == ".DDT")
				{
					ImagePreview.Source = file.PreviewDDT.Bitmap;
					XMLViewer.Visibility = Visibility.Collapsed;
					ImageViewer.Visibility = Visibility.Visible;
				}
				else
				if (entry.Extension == ".TGA" || entry.Extension == ".BMP" || entry.Extension == ".PNG" || entry.Extension == ".CUR" || entry.Extension == ".JPG")
				{
					ImagePreview.Source = file.PreviewImage;
					XMLViewer.Visibility = Visibility.Collapsed;
					ImageViewer.Visibility = Visibility.Visible;
				}
				else
				if (entry.Extension == ".XMB" || entry.Extension == ".XML" || entry.Extension == ".SHP" || entry.Extension == ".LGT" || entry.Extension == ".XS" || entry.Extension == ".TXT" || entry.Extension == ".CFG" || entry.Extension == ".XAML" || entry.Extension == ".PY")
				{
					ImageViewer.Visibility = Visibility.Collapsed;
					XMLViewer.Visibility = Visibility.Visible;
				}
				else
				{
					ImageViewer.Visibility = Visibility.Collapsed;
					XMLViewer.Visibility = Visibility.Collapsed;

				}

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			//files.IsEnabled = true;
		}

		private async void openFile(string path = null)
		{
			mainMenu.IsEnabled = false;
			SpinnerFile.Visibility = Visibility.Visible;
			tbFile.Text = "Opening";
			var filePath = path;
			if (string.IsNullOrEmpty(path))
			{

				OpenFileDialog openFileDialog = new OpenFileDialog();
				openFileDialog.InitialDirectory = Settings.Default.lastOpenedPath;
				openFileDialog.Filter = "Age of Empires 3 .BAR files (*.bar)|*.bar";
				if (openFileDialog.ShowDialog() == true)
				{
					filePath = openFileDialog.FileName;
					Settings.Default.lastOpenedPath = Path.GetDirectoryName(filePath);
					Settings.Default.Save();
				}
				else
				{
					SpinnerFile.Visibility = Visibility.Collapsed;
					tbFile.Text = "File";
					mainMenu.IsEnabled = true;
					return;
				}
			}
			try
			{
				file = null;
				NotifyPropertyChanged("recentFiles");
				NotifyPropertyChanged("file");
				// var stopwatch = new Stopwatch();
				//stopwatch.Start();
				file = await BarViewModel.Load(filePath);
				//stopwatch.Stop();
				if (Settings.Default.RecentFiles.Contains(filePath))
				{
					Settings.Default.RecentFiles.Remove(filePath);
					recentFiles.Remove(recentFiles.SingleOrDefault(x => x.FileName == filePath));
				}
				recentFiles.Insert(0, new RecentFile() { FileName = filePath, Title = Path.GetFileName(filePath), OnClickCommand = new RelayCommand<string>(openFile) });
				Settings.Default.RecentFiles.Insert(0, filePath);
				Settings.Default.Save();
				NotifyPropertyChanged("recentFiles");
				NotifyPropertyChanged("file");
				if (file.BarFile.barFileHeader.Version > 5)
				{
					gvclastModifiedDate.Width = 0;
					gvcFileName.Width = 440;

				}
				else
				{
					gvclastModifiedDate.Width = 190;
					gvcFileName.Width = 250;
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			SpinnerFile.Visibility = Visibility.Collapsed;
			tbFile.Text = "File";
			mainMenu.IsEnabled = true;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			openFile();
		}

		private void MenuItem_Click_1(object sender, RoutedEventArgs e)
		{
			Environment.Exit(0);
		}

		private void MenuItem_Click_3(object sender, RoutedEventArgs e)
		{
			About about = new About();
			about.ShowDialog();
		}

		private void MenuItem_Click_4(object sender, RoutedEventArgs e)
		{

			string targetURL = "https://github.com/VladTheJunior/Resource-Manager";
			var psi = new ProcessStartInfo
			{
				FileName = targetURL,
				UseShellExecute = true
			};
			Process.Start(psi);

		}

		private void files_Click(object sender, RoutedEventArgs e)
		{

			GridViewColumnHeader column = (sender as GridViewColumnHeader);
			string sortBy = column.Tag.ToString();
			if (listViewSortCol != null)
			{
				AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
				files.Items.SortDescriptions.Clear();
			}

			ListSortDirection newDir = ListSortDirection.Ascending;
			if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
				newDir = ListSortDirection.Descending;

			listViewSortCol = column;
			listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
			AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
			files.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
		}

		private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			int minWidth = 100;
			Thumb senderAsThumb = e.OriginalSource as Thumb;
			GridViewColumnHeader header
				= senderAsThumb.TemplatedParent as GridViewColumnHeader;
			if (header == null) return;
			if (header.Tag.ToString() == "isCompressed")
			{
				minWidth = 50;
			}
			if (header.Tag.ToString() == "FileNameWithRoot")
			{
				if (file == null || file.BarFile.barFileHeader.Version <= 5)
				{
					minWidth = 250;
				}
				else
				{
					minWidth = 440;

				}

			}
			if (header.Tag.ToString() == "FileSize2")
			{
				minWidth = 160;
			}
			if (header.Tag.ToString() == "Hash")
			{
				minWidth = 100;
			}
			if (header.Tag.ToString() == "lastModifiedDate")
			{

				if (file == null || file.BarFile.barFileHeader.Version <= 5)
				{
					minWidth = 190;
				}
				else
				{
					e.Handled = true;
					header.Column.Width = 0;

				}

			}
			if (header.Column.ActualWidth < minWidth)
			{
				e.Handled = true;
				header.Column.Width = minWidth;

			}
		}

		private async void extractMenuItem(object sender, RoutedEventArgs e)
		{
			if (file == null) return;
			mainMenu.IsEnabled = false;
			tbExtract.Text = "Extracting";
			SpinnerExtract.Visibility = Visibility.Visible;
			List<BarEntry> entries;
			if ((sender as MenuItem).Tag.ToString() == "Selected")
			{
				entries = files.SelectedItems.Cast<BarEntry>().ToList();
			}
			else if ((sender as MenuItem).Tag.ToString() == "List")
			{
				entries = new List<BarEntry>();
				OpenFileDialog openFileDialog = new OpenFileDialog();
				openFileDialog.Filter = "Text files (*.txt)|*.txt";
				if (openFileDialog.ShowDialog() == true)
				{
					List<string> icons = new List<string>(await File.ReadAllLinesAsync(openFileDialog.FileName));
					await Task.Run(() =>
					{
						entries = file.BarFile.BarFileEntries.Where(x =>
									icons.Any(y => x.FileNameWithRoot.ToLower().EndsWith(y.Replace('/', '\\').ToLower()))).ToList();
					});

				}
			}
			else
			{
				entries = file.SourceCollection.Cast<BarEntry>().ToList();
			}

			if (entries.Count != 0)
			{
				string RootPath;

				ExtractDialog ExtractDialog = new ExtractDialog(file.BarFilePath);
				if (ExtractDialog.ShowDialog() == true)
				{

					RootPath = ExtractDialog.Path;
					bPause.IsEnabled = true;
					bStop.IsEnabled = true;
					bRun.IsEnabled = false;
					bool decompress = ExtractDialog.AutoDecompress;

					file.ExtractingState = 0;
					CancelTokenSource = new CancellationTokenSource();
					Token = CancelTokenSource.Token;
					try
					{

						await Task.Run(async () =>
						{
							await file.saveFiles(entries, RootPath, decompress, Token, ExtractDialog.AutoDDTToPNGConversion, ExtractDialog.AutoDDTToTGAConversion, ExtractDialog.AutoXMBConversion, ExtractDialog.OneFolder, ExtractDialog.SavePNGasBMP, ExtractDialog.AutoJSONConversion, ExtractDialog.OverlayColor, ExtractDialog.CompressPNG, ExtractDialog.AutoPNGToWEBPConversion);
						});

					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					bPause.IsEnabled = false;
					bStop.IsEnabled = false;
					bRun.IsEnabled = false;
				}

			}
			tbExtract.Text = "Extract";
			SpinnerExtract.Visibility = Visibility.Collapsed;
			mainMenu.IsEnabled = true;
		}

		private void bPause_Click(object sender, RoutedEventArgs e)
		{
			bPause.IsEnabled = false;
			file.ExtractingState = 1;
			CancelTokenSource.Cancel();
			bRun.IsEnabled = true;
			bStop.IsEnabled = true;
		}

		private void bRun_Click(object sender, RoutedEventArgs e)
		{
			bRun.IsEnabled = false;
			CancelTokenSource = new CancellationTokenSource();
			Token = CancelTokenSource.Token;
			file.ExtractingState = 0;
			bStop.IsEnabled = true;
			bPause.IsEnabled = true;
		}

		private void bStop_Click(object sender, RoutedEventArgs e)
		{
			bStop.IsEnabled = false;
			bPause.IsEnabled = false;
			bRun.IsEnabled = false;
			file.ExtractingState = 2;
			CancelTokenSource.Cancel();
		}

		private void ImageViewer_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
			{
				if (ZoomValue < 400)
					_zoomValue += 0.1;
				else
					return;
			}
			else
			{
				if (ZoomValue > 10)
					_zoomValue -= 0.1;
				else
					return;
			}
			NotifyPropertyChanged("ZoomValue");
			ScaleTransform scale = new ScaleTransform(_zoomValue, _zoomValue);
			ImagePreview.LayoutTransform = scale;
			e.Handled = true;
		}

		private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_zoomValue = 1;
			ScaleTransform scale = new ScaleTransform(_zoomValue, _zoomValue);
			ImagePreview.LayoutTransform = scale;
			NotifyPropertyChanged("ZoomValue");
		}

		private void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
		{

			//await DDTFileUtils.DDT2PngAsync(@"D:\Development\Resource_Manager\Resource_Manager\bin\Release\netcoreapp3.1\Art\ui\alerts\alert_treatyend_bump.ddt");
		}

		private async void MenuItem_Click_7(object sender, RoutedEventArgs e)
		{
			mainMenu.IsEnabled = false;
			SpinnerFile.Visibility = Visibility.Visible;
			tbFile.Text = "Creating";
			CreateBarFileDialog createBarFileDialog = new CreateBarFileDialog();
			if (createBarFileDialog.ShowDialog() == true)
			{
				try
				{
					file = null;
					NotifyPropertyChanged("recentFiles");
					NotifyPropertyChanged("file");
					file = await BarViewModel.Create(createBarFileDialog.RootPath, createBarFileDialog.Version, createBarFileDialog.BarFileName);
					if (Settings.Default.RecentFiles.Contains(file.BarFilePath))
					{
						Settings.Default.RecentFiles.Remove(file.BarFilePath);
						recentFiles.Remove(recentFiles.SingleOrDefault(x => x.FileName == file.BarFilePath));
					}
					recentFiles.Insert(0, new RecentFile() { FileName = file.BarFilePath, Title = Path.GetFileName(file.BarFilePath), OnClickCommand = new RelayCommand<string>(openFile) });
					Settings.Default.RecentFiles.Insert(0, file.BarFilePath);
					Settings.Default.Save();
					NotifyPropertyChanged("recentFiles");
					NotifyPropertyChanged("file");

					if (file.BarFile.barFileHeader.Version > 5)
					{

						gvclastModifiedDate.Width = 0;

					}
					else
					{
						gvclastModifiedDate.Width = 190;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			SpinnerFile.Visibility = Visibility.Collapsed;
			tbFile.Text = "File";
			mainMenu.IsEnabled = true;
		}

		private void MenuItem_Checked(object sender, RoutedEventArgs e)
		{
			files.Items.GroupDescriptions.Clear();
			files.Items.GroupDescriptions.Add(new PropertyGroupDescription("Extension"));
		}

		private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
		{
			files.Items.GroupDescriptions.Clear();

		}

		private void MenuItem_Checked_1(object sender, RoutedEventArgs e)
		{
			gPreview.Visibility = Visibility.Visible;
			gsSplitter.Visibility = Visibility.Visible;
		}

		private void MenuItem_Unchecked_1(object sender, RoutedEventArgs e)
		{
			gPreview.Visibility = Visibility.Collapsed;
			gsSplitter.Visibility = Visibility.Collapsed;
		}

		private async void convertFiles(object sender, RoutedEventArgs e)
		{
			mainMenu.IsEnabled = false;
			SpinnerConvert.Visibility = Visibility.Visible;
			tbConvert.Text = "Converting";
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Settings.Default.lastConvertedPath;
			openFileDialog.Multiselect = true;

			string operationType = (sender as MenuItem).Tag.ToString();
			//await DDTFileUtils.DDT2PngAsync(@"D:\Development\Resource_Manager\Resource_Manager\bin\Release\netcoreapp3.1\Art\ui\alerts\alert_treatyend_bump.ddt");

			if (operationType == "totga")
			{
				openFileDialog.Filter = "Age of Empires 3 DDT files (*.ddt)|*.ddt";
				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							var ddt = new DDT();
							await ddt.Create(file);
							await ddt.SaveAsTGA(file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "tgatoddt")
			{
				openFileDialog.Filter = "TGA files (*.tga)|*.tga";
				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							var ddt = new DDT();
							await ddt.FromPicture(file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "topng")
			{
				openFileDialog.Filter = "Age of Empires 3 DDT files (*.ddt)|*.ddt";
				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							var ddt = new DDT();
							await ddt.Create(file);
							await ddt.SaveAsPNG(file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "toxml")
			{
				openFileDialog.Filter = "Age of Empires 3 XMB files (*.xmb)|*.xmb";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							var data = await File.ReadAllBytesAsync(file);

							if (Alz4Utils.IsAlz4File(data))
							{
								data = await Alz4Utils.ExtractAlz4BytesAsync(data);
							}
							else
							{
								if (L33TZipUtils.IsL33TZipFile(data))
									data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
							}

							using MemoryStream stream = new MemoryStream(data);
							XMBFile xmb = await XMBFile.LoadXMBFile(stream);

							var newName = Path.ChangeExtension(file, "");

							xmb.file.Save(newName);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "jsontoxml")
			{
				openFileDialog.Filter = "JSON files (*.json)|*.json";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							var data = await File.ReadAllBytesAsync(file);

							if (Alz4Utils.IsAlz4File(data))
							{
								data = await Alz4Utils.ExtractAlz4BytesAsync(data);
							}
							else
							{
								if (L33TZipUtils.IsL33TZipFile(data))
									data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
							}
							string json = await File.ReadAllTextAsync(file);
							var xml = JsonConvert.DeserializeXmlNode(json);

							var newName = Path.ChangeExtension(file, "");

							xml.Save(newName);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "xmbtojson")
			{
				openFileDialog.Filter = "Age of Empires 3 XMB files (*.xmb)|*.xmb";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							var data = await File.ReadAllBytesAsync(file);

							if (Alz4Utils.IsAlz4File(data))
							{
								data = await Alz4Utils.ExtractAlz4BytesAsync(data);
							}
							else
							{
								if (L33TZipUtils.IsL33TZipFile(data))
									data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
							}

							using MemoryStream stream = new MemoryStream(data);

							XMBFile xmb = await XMBFile.LoadXMBFile(stream);
							string json = JsonConvert.SerializeXmlNode(xmb.file);
							await File.WriteAllTextAsync(Path.ChangeExtension(file, "json"), json);

						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "xmltojson")
			{
				openFileDialog.Filter = "Age of Empires 3 XML files (*.*)|*.*";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							XmlDocument xml = new XmlDocument();
							xml.Load(file);
							string json = JsonConvert.SerializeXmlNode(xml);
							await File.WriteAllTextAsync(Path.ChangeExtension(file, "json"), json);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "jsontoxmblegacy")
			{
				openFileDialog.Filter = "JSON files (*.json)|*.json";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							string json = await File.ReadAllTextAsync(file);
							var xml = JsonConvert.DeserializeXmlNode(json);
							await XMBFile.CreateXMBFileL33T(xml, file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "jsontoxmbde")
			{
				openFileDialog.Filter = "JSON files (*.json)|*.json";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							string json = await File.ReadAllTextAsync(file);
							var xml = JsonConvert.DeserializeXmlNode(json);
							await XMBFile.CreateXMBFileALZ4(xml, file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "toxmbde")
			{
				openFileDialog.Filter = "Age of Empires 3 XML files (*.*)|*.*";

				if (openFileDialog.ShowDialog() == true)
				{
					Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
					Settings.Default.Save();
					foreach (var file in openFileDialog.FileNames)
					{
						try
						{
							await XMBFile.CreateXMBFileALZ4(file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			if (operationType == "toxmbcc")
			{
				openFileDialog.Filter = "Age of Empires 3 XML files (*.*)|*.*";
				if (openFileDialog.ShowDialog() == true)
				{
					foreach (var file in openFileDialog.FileNames)
					{
						Settings.Default.lastConvertedPath = Path.GetDirectoryName(openFileDialog.FileName);
						Settings.Default.Save();
						try
						{
							await XMBFile.CreateXMBFileL33T(file);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}

			tbConvert.Text = "Convert";
			SpinnerConvert.Visibility = Visibility.Collapsed;
			mainMenu.IsEnabled = true;
		}

		GridViewColumn tempColumn;
		private void MenuItem_Checked_2(object sender, RoutedEventArgs e)
		{

		}

		private void MenuItem_Unchecked_2(object sender, RoutedEventArgs e)
		{

		}

		private void MenuItem_Click_5(object sender, RoutedEventArgs e)
		{
			CompareWindow window = new CompareWindow();
			window.Show();
		}

		private void TextBlock_MouseDown_2(object sender, MouseButtonEventArgs e)
		{
		}

		Point point = new Point();
		bool validPoint = false;

		private void ImagePreview_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point newPos = e.GetPosition(this);

				if (validPoint)
				{
					ImagePreview.RenderTransform = new TranslateTransform(newPos.X - point.X, newPos.Y - point.Y);
				}

				if (!validPoint)
				{
					point = newPos;
					validPoint = true;

					if (ImagePreview.RenderTransform != null)
					{
						point.X -= ImagePreview.RenderTransform.Value.OffsetX;
						point.Y -= ImagePreview.RenderTransform.Value.OffsetY;
					}
				}
			}
			else
			{
				validPoint = false;
			}
		}

		private void StatusBar_MouseDown(object sender, MouseButtonEventArgs e)
		{

		}

		private void MenuItem_Click_2(object sender, RoutedEventArgs e)
		{
			EntryDetailsDialog window = new EntryDetailsDialog(files.SelectedItem as BarEntry, file.BarFilePath);
			window.ShowDialog();
		}

		private void bClose_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var args = Environment.GetCommandLineArgs();
			if (args.Length > 1 && Path.GetExtension(args[1]).ToUpper() == ".BAR")
			{
				openFile(args[1]);
			}

		}

		private void MenuItem_Click_6(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText((files.SelectedItem as BarEntry).FileNameWithRoot);
		}

		private async void MenuItem_Click_8(object sender, RoutedEventArgs e)
		{
			if (file == null) return;
			var options = new JsonSerializerOptions { WriteIndented = true };
			var json = JsonSerializer.Serialize(file.BarFile.BarFileEntries, options);

			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.FileName = Path.GetFileNameWithoutExtension(file.BarFilePath) + " - entries details.json";
			saveDialog.Filter = "JSON files (*.json)|*.json";
			if (saveDialog.ShowDialog() == true)
			{
				await File.WriteAllTextAsync(saveDialog.FileName, json);
			}
		}

		private void MenuItem_Click_9(object sender, RoutedEventArgs e)
		{
			string barDirectory = Path.Combine(AppContext.BaseDirectory, "Cached");
			if (Directory.Exists(barDirectory))
			{
				Directory.Delete(barDirectory, true);
			}

		}

		private void MenuItem_Click_10(object sender, RoutedEventArgs e)
		{
			string targetURL = "https://docs.google.com/forms/d/e/1FAIpQLSdWIaJ_Xlb-wdye5Rf1AtWSfcHhmaEN3nflYHcLEzrXZtnHmA/viewform?usp=sf_link";
			var psi = new ProcessStartInfo
			{
				FileName = targetURL,
				UseShellExecute = true
			};
			Process.Start(psi);
		}

		private void MenuItem_Click_11(object sender, RoutedEventArgs e)
		{
			ReleaseNotes window = new ReleaseNotes();
			window.ShowDialog();
		}
	}

	#region Value Converters


	public class VisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null && value is bool)
			{
				if ((bool)value == true)
				{
					return Visibility.Collapsed;
				}
				else
				{ return Visibility.Visible; }
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	public class ReverseVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null && value is bool)
			{
				if ((bool)value == false)
				{
					return Visibility.Collapsed;
				}
				else
				{ return Visibility.Visible; }
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	public class MathConverter : IValueConverter
	{
		public object Convert(object value, Type targetType,
					object parameter, CultureInfo culture)
		{
			return (double)value * 100;
		}

		public object ConvertBack(object value, Type targetType,
				object parameter, CultureInfo culture)
		{
			return null;
		}
	}
	public class RunEnabledColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				if ((bool)value)
					return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF388934"));
				else
					return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF707070"));
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	public class PauseEnabledColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				if ((bool)value)
					return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF00539c"));
				else
					return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF707070"));
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	public class StopEnabledColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				if ((bool)value)
					return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA1260D"));
				else
					return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF707070"));
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	public class HighlightingDefinitionConverter : IValueConverter
	{
		private static readonly HighlightingDefinitionTypeConverter Converter = new HighlightingDefinitionTypeConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				return Converter.ConvertFrom(value);
			}
			else
				return Converter.ConvertFrom("XML");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Converter.ConvertToString(value);
		}
	}
	#endregion
}
