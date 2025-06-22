using Resource_Manager.Classes.Bar;

namespace Resource_Manager.Classes.BarComparer
{
	public class BarComparerEntry
	{
		public string Type { get; set; } = "Unchanged";
		public BarEntry EntryOld { get; set; }
		public BarEntry EntryNew { get; set; }
	}
}
