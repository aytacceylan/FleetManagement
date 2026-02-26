using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class PlaceholderPage : Page
	{
		public PlaceholderPage(string title)
		{
			InitializeComponent();
			TitleText.Text = title;
		}
	}
}