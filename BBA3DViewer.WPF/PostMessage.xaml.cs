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
	/// This view is an example of how to implement the 3D Viewer using the PostMessage browser api.
	/// This is a bit tricky because we need to hook into the PostMessage api so we can receive data sent through this api.
	/// Hooking into the PostMessage api is achieved by running the PostMessageCapturer.js (in the js folder) file before the page is loaded.
	/// This script will override the default window.PostMessage(...) function.
	/// Our implementation does the following:
	/// 1. Call the original/base implementation
	/// 2. If WebView2 is detected, post it to the browser too. This will trigger <see cref="Webbrowser_WebMessageReceived"/>.
	/// The viewer uses the PostMessage api to send some information (e.g. ViewerReady will indicate that it is ready to receive a product object)
	/// After the ViewerReady message we can respond with the PostMessage api. To do this we created a simple javascript script called MessageSender.js (in the js folder).
	/// You have to inject this script in order to run it. Before injecting it you will need to replace 3 variables.
	/// 1. __type__:			this is an argument the viewer will watch for. In our case it is LoadViewer (this indicates that you are sending a product object to render)
	/// 2. __targetOrigin__:	this is an argument which is specifically for the PostMessage api. It makes sure you know to which origin you are sending the message to.
	/// 3. __json__:			this is the product object as json. You have to replace replace this with your productobject (as json).
	/// 
	/// This method of calling the viewer is created to make it possible to interact with the viewer from an iframe or opened window.
	/// As you can see, it is possible to implement it in a wpf app, however the <see cref="Traditional"/> implementation is easier to implement and understand.
	/// </summary>
	public partial class PostMessage : Window
	{
		private Dictionary<string, string> _scripts = new Dictionary<string, string>();
		private Dictionary<string, FileInfo> _viewerObjects = new Dictionary<string, FileInfo>();

		public PostMessage()
		{
			InitializeComponent();
		}

		private async void webbrowser_Initialized(object sender, EventArgs e)
		{
			var dll = new FileInfo(Assembly.GetExecutingAssembly().Location);
			var dir = dll.Directory;

			var jsDir = new DirectoryInfo(Path.Combine(dir.FullName, "js"));

			// Load the javascript files in memory so we can access it very easily
			foreach (var script in jsDir.EnumerateFiles("*.js"))
			{
				using (var sr = new StreamReader(script.OpenRead()))
				{
					// Make sure the files are marked as content and copy if newer!
					_scripts.Add(script.Name, await sr.ReadToEndAsync());
				}
			}

			// Load the json files in memory so we can access it very easily
			var viewerObjectDir = new DirectoryInfo(Path.Combine(dir.FullName, "ViewerObjects"));
			foreach (var file in viewerObjectDir.EnumerateFiles("*.json"))
			{
					// Make sure the files are marked as content and copy if newer!
				_viewerObjects.Add(file.Name, file); // Add file instead of content as we wanna lazy-load this.
			}

			await webbrowser.EnsureCoreWebView2Async(); // This ensures that CoreWebView2 is available. We override the window.PostMessage method so we can pass the messages on to the .NET application.
			await webbrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_scripts["PostMessageCapturer.js"]); // Make sure the window.postMessage api is using our implementation.

			// This line opens the dev tool right away. useful for debugging.
			// webbrowser.CoreWebView2.OpenDevToolsWindow();

			// This event is called every time window.postMessage(...) is called on a website. The PostMessageCapturer makes sure this event is called.
			webbrowser.WebMessageReceived += Webbrowser_WebMessageReceived;

			// Navigate to the viewer
			webbrowser.CoreWebView2.Navigate(Settings1.Default.ViewerUrl + "/Viewer3D");
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
