using Newtonsoft.Json;

namespace BBA3DViewer.WPF
{
	public class PostMessageObject
	{
		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("data")]
		public object Data { get; set; }
	}
}