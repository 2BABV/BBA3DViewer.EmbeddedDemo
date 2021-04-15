using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BBA3DViewer.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private Dictionary<string, string> _scripts = new Dictionary<string, string>();
		private Dictionary<string, FileInfo> _viewerObjects = new Dictionary<string, FileInfo>();

		public MainWindow()
		{
			InitializeComponent();
		}

		private async void webbrowser_Initialized(object sender, EventArgs e)
		{
			var dll = new FileInfo(Assembly.GetExecutingAssembly().Location);
			var dir = dll.Directory;

			var jsDir = new DirectoryInfo(Path.Combine(dir.FullName, "js"));

			foreach (var script in jsDir.EnumerateFiles("*.js"))
			{
				using (var sr = new StreamReader(script.OpenRead()))
				{
					// Make sure the files are marked as content and copy if newer!
					_scripts.Add(script.Name, await sr.ReadToEndAsync());
				}
			}

			var viewerObjectDir = new DirectoryInfo(Path.Combine(dir.FullName, "ViewerObjects"));
			foreach (var file in viewerObjectDir.EnumerateFiles("*.json"))
			{
					// Make sure the files are marked as content and copy if newer!
				_viewerObjects.Add(file.Name, file); // Add file instead of content as we wanna lazy-load this.
			}

			await webbrowser.EnsureCoreWebView2Async(); // This ensures that CoreWebView2 is available. We override the window.PostMessage method so we can pass the messages on to the .NET application.
			await webbrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_scripts["PostMessageCapturer.js"]);

			// This line opens the dev tool right away. useful for debugging.
			// webbrowser.CoreWebView2.OpenDevToolsWindow();

			// This event is called every time window.postMessage(...) is called on a website. The PostMessageCapturer makes sure this event is called.
			webbrowser.WebMessageReceived += Webbrowser_WebMessageReceived;
		}

		/// <summary>
		/// This is an event which is called every time window.postMessage(...) is called thanks to PostMessageCapturer.js.
		/// </summary>
		/// <param name="sender">Webbrowser</param>
		/// <param name="e">Event arguments</param>
		private void Webbrowser_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
		{
			try
			{
				// Get the contents as json
				var o = JsonConvert.DeserializeObject<PostMessageObject>(e.WebMessageAsJson);

				// Check if its not null before proceeding
				if (o != null && o.Type != null)
				{
					// If it's a message from the viewer, and it indicates that the viewer is ready, enable the buttons.
					switch (o.Type)
					{
						case "Viewer3D:ObjectLoaded":
							setButtons(true);
							break;
						case "Viewer3D:ViewerReady":
							setButtons(true);
							break;
						default:
							break;
					}
				}
			}
			catch
			{
				// Just ignore it for this demo
			}
		}

		/// <summary>
		/// Calls window.PostMessage which is listened to in the viewer.
		/// </summary>
		/// <param name="type">The type of message. This is a string which is mapped in the viewer e.g. "LoadViewer"</param>
		/// <param name="json">The product specification to generate an object with.</param>
		private async Task DoPostMessageAsync(string type, string json)
		{
			var script = _scripts["MessageSender.js"]
				.Replace("__type__", type)
				.Replace("__targetOrigin__", Settings1.Default.ViewerUrl)
				.Replace("__json__", json);

			setButtons(false);

			await webbrowser.ExecuteScriptAsync(script);
		}

		private async void btnStuurtransformator_Click(object sender, RoutedEventArgs e)
		{
			await RequestObjectAsync("Stuurtransformator");
		}

		private async void btnBlokpomp_Click(object sender, RoutedEventArgs e)
		{
			await RequestObjectAsync("Blokpomp");
		}

		/// <summary>
		/// Requests the viewer to render the product
		/// </summary>
		/// <param name="name">The name of the json file without extension. Make sure the json file is marked as Content and Copy if newer.</param>
		public async Task RequestObjectAsync(string name)
		{
			using (var sr = new StreamReader(_viewerObjects[name + ".json"].OpenRead()))
			{
				var json = await sr.ReadToEndAsync();
				await DoPostMessageAsync("LoadViewer", json);
			}
		}

		private void setButtons(bool active)
		{
			btnBlokpomp.IsEnabled = active;
			btnStuurtransformator.IsEnabled = active;
		}
	}
}
