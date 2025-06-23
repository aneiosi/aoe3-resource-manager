using MessageBox = System.Windows.MessageBox;

namespace Resource_Manager
{
	/// <summary>
	/// Interaction logic for CreateBarFileDialog.xaml
	/// </summary>
	public partial class CreateBarFileDialog : Window
	{
		public CreateBarFileDialog()
		{
			InitializeComponent();
		}

		public string RootPath { get; set; } = "";

		public string BarFileName { get; set; } = "";

		public uint Version
		{
			get
			{
				return (tbGameVersion.SelectedIndex == 0) ? (uint)2 : (uint)6;
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
			{
				if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					RootPath = folderBrowserDialog.SelectedPath;
					tbRootPath.Text = RootPath;
				}
			}
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(RootPath))
			{
				MessageBox.Show("Select Bar Root Folder!");
				return;
			}
			if (string.IsNullOrEmpty(tbBarName.Text))
			{
				MessageBox.Show("Enter Bar File Name!");
				return;
			}
			BarFileName = tbBarName.Text;

			DialogResult = true;

		}
	}
}
