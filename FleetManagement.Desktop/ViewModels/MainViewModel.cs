using System.Windows.Input;
using FleetManagement.Desktop.Views;

namespace FleetManagement.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
	private object? _currentView;
	public object? CurrentView
	{
		get => _currentView;
		private set { _currentView = value; OnPropertyChanged(); }
	}

	public ICommand ShowDashboardCommand { get; }
	public ICommand ShowDriversCommand { get; }
	public ICommand ShowMovementsCommand { get; }

	public MainViewModel()
	{
		ShowDashboardCommand = new RelayCommand(_ => CurrentView = new DashboardView());
		ShowDriversCommand = new RelayCommand(_ => CurrentView = new DriverView());
		ShowMovementsCommand = new RelayCommand(_ => CurrentView = new VehicleMovementView());

		// Açılış
		CurrentView = new DashboardView();
	}
}