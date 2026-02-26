using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class PlaceholderView : Page
	{
		public PlaceholderView(string title)
		{
			InitializeComponent();
			TitleText.Text = title;
		}
	}
}