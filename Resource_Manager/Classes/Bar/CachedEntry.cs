using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Bar
{
	public class CachedEntry
	{
		public int Compression { get; set; }
		public string File { get; set; }
		public int Size { get; set; }
		public string Hash { get; set; }
	}
}
