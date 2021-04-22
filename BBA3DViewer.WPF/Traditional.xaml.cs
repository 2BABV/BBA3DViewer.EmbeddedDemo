using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BBA3DViewer.WPF
{
	/// <summary>
	/// This view is an example of how to implement the 3D Viewer the "traditional" way.
	/// How this works is 
	/// 1. We POST our product to the api. This will return a key
	/// 2. We use this key to load the viewer in the browser. This may be an embedded browser, but will also work in other browsers.
	/// 
	/// Personally I would recomend using this method over the <see cref="PostMessage"/> method because this implementation is simpler to understand and maintain.
	/// </summary>
	public partial class Traditional : Window
	{
		private Dictionary<string, string> _scripts = new Dictionary<string, string>();
		private Dictionary<string, FileInfo> _viewerObjects = new Dictionary<string, FileInfo>();

		// HttpClient MUST be static. Otherwise you might exhaust your tcp sockets
		private static readonly HttpClient client = new HttpClient();

		public Traditional()
		{
			InitializeComponent();
		}

		private async void webbrowser_Initialized(object sender, EventArgs e)
		{
			var dll = new FileInfo(Assembly.GetExecutingAssembly().Location);
			var dir = dll.Directory;

			// Load the json files in memory so we can access it very easily
			var viewerObjectDir = new DirectoryInfo(Path.Combine(dir.FullName, "ViewerObjects"));
			foreach (var file in viewerObjectDir.EnumerateFiles("*.json"))
			{
				// Make sure the files are marked as content and copy if newer!
				_viewerObjects.Add(file.Name, file); // Add file instead of content as we wanna lazy-load this.
			}

			await webbrowser.EnsureCoreWebView2Async(); // This ensures that CoreWebView2 is available. We need this later to navigate.

			// Enable the buttons.
			setButtons(true);
		}

		/// <summary>
		/// Sends the json to the viewer api and opens the viewer with the key the api returned.
		/// </summary>
		/// <param name="type">The type of message. This is a string which is mapped in the viewer e.g. "LoadViewer"</param>
		/// <param name="json">The product specification to generate an object with.</param>
		private async Task DoPostMessageAsync(string json)
		{
			// This block of code wraps our json in a json property called "Product".
			// The result will be something like this: {"Product": (our json)}
			var jsonObj = (JToken)JsonConvert.DeserializeObject(json);
			Newtonsoft.Json.Linq.JObject newJson = new JObject();
			newJson.Add("Product", jsonObj);

			// Set our original json to the new json.
			json = newJson.ToString(Formatting.None);

			setButtons(false);
			string contentStr = null;
			try
			{
				using (var msg = new HttpRequestMessage(HttpMethod.Post, Settings1.Default.ViewerUrl + "/Api/PostProduct"))
				{
					msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
					var response = await client.SendAsync(msg);
					contentStr = await response.Content.ReadAsStringAsync();
					var responseObj = JsonConvert.DeserializeObject<ViewerApiResponse>(contentStr);
					response.EnsureSuccessStatusCode();

					webbrowser.CoreWebView2.Navigate(Settings1.Default.ViewerUrl + "/Viewer3D/" + responseObj.Key);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Something went wrong when sending a request to the viewer api\n" + ex + "\n Response: " + contentStr, "Error");
			}

			setButtons(true);
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

				await DoPostMessageAsync(json);
			}
		}

		private void setButtons(bool active)
		{
			btnBlokpomp.IsEnabled = active;
			btnStuurtransformator.IsEnabled = active;
		}

		private class ViewerApiResponse
		{
			public string Key { get; set; }
		}
	}
}
