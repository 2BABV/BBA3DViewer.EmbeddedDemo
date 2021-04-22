using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BBA3DViewer.WPF
{
	/// <summary>
	/// Interaction logic for OptionPicker.xaml
	/// </summary>
	public partial class OptionPicker : Window
	{
		public OptionPicker()
		{
			InitializeComponent();

			// If the picker is closed, just close the whole application.
			this.Closing += (e, args) =>
			{
				Application.Current.Shutdown();
			};
		}

		private void btn_traditional_Click(object sender, RoutedEventArgs e)
		{
			var window = new Traditional();
			window.Show();
		}

		private void btn_postmessage_Click(object sender, RoutedEventArgs e)
		{
			var window = new PostMessage();
			window.Show();
		}
	}
}
