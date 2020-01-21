using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
	public static class HttpClientExtensions
	{
        public static string[][] Images = new string[][] {
            new string[] { "R2D2", "https://www.swarovski.com/is-bin/intershop.static/WFS/SCO-Media-Site/-/-/publicimages/CG/B2C/PROD/360/Swarovski-Star-Wars-R2-D2-5301533-W360.jpg" },
            new string[] { "Data", "https://s-media-cache-ak0.pinimg.com/originals/b8/dc/6d/b8dc6ddfe09223d4f04e4dd9e6f42e21.jpg" },
            new string[] { "Robocop", "https://i.ytimg.com/vi/zbCbwP6ibR4/maxresdefault.jpg" },
            new string[] { "Bender", "https://pbs.twimg.com/profile_images/76277472/bender_400x400.jpg" },
            new string[] { "C3PO", "https://i.ytimg.com/vi/jH_DT7ekFiA/maxresdefault.jpg" },
            new string[] { "Optimus Prime", "https://s3.amazonaws.com/tf.images/reduced-3zeroopt.jpg" },
            new string[] { "Terminator", "http://cdn-static.denofgeek.com/sites/denofgeek/files/styles/article_main_wide_image/public/3/05//terminator_1_0.jpg" },
            new string[] { "Wall-E", "http://www.robots-and-androids.com/images/wall-e.jpg" },
            new string[] { "Fembot", "http://static.srcdn.com/slir/w533-h300-q90-c533:300/wp-content/uploads/FemBots-Austin-Powers.jpeg" }
        };

        public static Task<HttpResponseMessage> SendAsJsonAsync<T>(this HttpClient client, HttpMethod method, string requestUri, T value)
		{
			var content = value.GetType().Name.Equals("JObject") ? 
				value.ToString() : 
				JsonConvert.SerializeObject(value, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });

			HttpRequestMessage request = new HttpRequestMessage(method, requestUri) { Content = new StringContent(content) };
			request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

			return client.SendAsync(request);
		}
	}
}
