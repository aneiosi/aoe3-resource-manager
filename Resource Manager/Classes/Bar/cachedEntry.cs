using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Bar
{
	public class cachedEntry
	{
		public int compression { get; set; }
		public string file { get; set; }
		public int size { get; set; }
		public string hash { get; set; }
	}
}
