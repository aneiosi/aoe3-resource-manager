namespace Resource_Manager
{
	/// <summary>
	/// Interaction logic for ReleaseNotes.xaml
	/// </summary>
	public partial class ReleaseNotes : Window
	{
		public ReleaseNotes()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			string targetURL = "https://github.com/AOE3-Modding-Council/Resource-Manager/releases";
			var psi = new ProcessStartInfo
			{
				FileName = targetURL,
				UseShellExecute = true
			};
			Process.Start(psi);
		}
	}
}
