using Archive_Unpacker.Classes.BarViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Resource_Manager.Classes.BarComparer
{
	public class BarComparer
	{
		public IReadOnlyCollection<BarComparerEntry> CompareEntries { get; set; }
		private CollectionViewSource CompareEntriesCollection;
		public ICollectionView CompareSourceCollection
		{
			get
			{
				return CompareEntriesCollection.View;
			}
		}

		public static async Task<BarComparer> Compare(BarViewModel bar1, BarViewModel bar2)
		{
			BarComparer BarComparer = new();
			var barEntrys = new List<BarComparerEntry>();

			await Task.Run(() =>
			{
				var Added = bar2.
					BarFile.
					BarFileEntries.
					Where(item => !bar1.
						BarFile.
						BarFileEntries.
						Any(item2 => item2.FileNameWithRoot.ToLower() == item.FileNameWithRoot.ToLower())
					).
					ToList();

				var Removed = bar1.
					BarFile.
					BarFileEntries.
					Where(item => !bar2.
						BarFile.
						BarFileEntries.
						Any(item2 => item2.FileNameWithRoot.ToLower() == item.FileNameWithRoot.ToLower())
					).
					ToList();

				var ChangedOld = bar1.
					BarFile.
					BarFileEntries.
					Where(item => bar2.
						BarFile.
						BarFileEntries.
						Any(item2 => item2.FileNameWithRoot.ToLower() == item.FileNameWithRoot.ToLower() &&
							item2.Hash != item.Hash)
					).
					OrderBy(x => x.FileNameWithRoot).
					ToList();

				var ChangedNew = bar2.
					BarFile.
					BarFileEntries.
					Where(item => bar1.
						BarFile.
						BarFileEntries.
						Any(item2 => item2.FileNameWithRoot.ToLower() == item.FileNameWithRoot.ToLower() &&
							item2.Hash != item.Hash)
					).
					OrderBy(x => x.FileNameWithRoot).
					ToList();

				var SameOld = bar1.
					BarFile.
					BarFileEntries.
					Where(item => bar2.
						BarFile.
						BarFileEntries.
						Any(item2 => item2.FileNameWithRoot.ToLower() == item.FileNameWithRoot.ToLower() &&
							item2.Hash == item.Hash)
					).
					OrderBy(x => x.FileNameWithRoot).
					ToList();

				var SameNew = bar2.
					BarFile.
					BarFileEntries.
					Where(item => bar1.
						BarFile.
						BarFileEntries.
						Any(item2 => item2.FileNameWithRoot.ToLower() == item.FileNameWithRoot.ToLower() &&
							item2.Hash == item.Hash)
					).
					OrderBy(x => x.FileNameWithRoot).
					ToList();

				for (int i = 0; i < ChangedOld.Count; i++)
				{
					barEntrys.Add(new BarComparerEntry()
					{
						Type = "Changed",
						EntryNew = ChangedNew[i],
						EntryOld = ChangedOld[i]
					});
				}

				Removed.ForEach(c => barEntrys.Add(new BarComparerEntry()
				{
					Type = "Removed",
					EntryOld = c,
					EntryNew = null
				}));
				Added.ForEach(c => barEntrys.Add(new BarComparerEntry()
				{
					Type = "Added",
					EntryNew = c,
					EntryOld = null
				}));

				for (int i = 0; i < SameOld.Count; i++)
				{
					barEntrys.Add(new BarComparerEntry()
					{
						Type = "Unchanged",
						EntryNew = SameNew[i],
						EntryOld = SameOld[i]
					});
				}
			});

			BarComparer.CompareEntries = new ReadOnlyCollection<BarComparerEntry>(barEntrys);
			BarComparer.CompareEntriesCollection = new CollectionViewSource
			{
				Source = BarComparer.CompareEntries
			};
			return BarComparer;
		}
	}
}
