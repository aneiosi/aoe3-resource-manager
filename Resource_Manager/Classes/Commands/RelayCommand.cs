using System;
using System.Windows.Input;

namespace Resource_Manager.Classes.Commands
{
	public class RelayCommand<T>(Action<string> openFile) : ICommand
	{
		private Action<string> openFile = openFile;

		public void Execute(object parameter)
		{
			openFile(parameter.ToString());
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;
	}
}
