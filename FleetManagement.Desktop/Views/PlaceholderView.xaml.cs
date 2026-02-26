using System.Windows.Controls;

namespace FleetManagement.Desktop.Views
{
	public partial class PlaceholderView : Page
	{
		public PlaceholderView(string title)
		{
			InitializeComponent();
			InfoText.Text = $"Menü: {title}";
		}
	}
}
