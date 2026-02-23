
using System.Windows;
using FleetManagement.Desktop.ViewModels;

namespace FleetManagement.Desktop
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainViewModel();
		}
	}
}