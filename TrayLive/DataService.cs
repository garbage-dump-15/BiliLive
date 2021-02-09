using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TrayLive
{
	public delegate void DataCallback(uint data);

	public class DataService
	{
		private readonly Uri _target;

		private readonly string _param;

		public DataService(uint uid, string param = "online")
		{
			_target = new Uri("https://live.bilibili.com/" + uid);
			_param = param;
		}

		private uint ReadValue(string raw)
		{
			return
				uint.Parse(Regex.Match(raw, "\"" + _param + "\":(\\d+)").Groups[1].Value);
		}

		private void ReadHttp(out Task<HttpResponseMessage> context)
		{
			var handler = new HttpClientHandler
			{
				AllowAutoRedirect = true,
				UseCookies = true,
				CookieContainer = new CookieContainer(),
				AutomaticDecompression = DecompressionMethods.GZip,
				ClientCertificateOptions = ClientCertificateOption.Automatic
			};

			var httpClient = new HttpClient(handler);
			httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
				"text/html,application/xhtml+xml,application/xml");
			httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
			httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
				"Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
			context = httpClient.GetAsync(_target);
		}

		public void ReadLiveAsync(DataCallback callback)
		{
			ReadHttp(out var task);
			task.GetAwaiter().OnCompleted(delegate
			{
				var awaiter = task.Result.Content
					.ReadAsStringAsync()
					.GetAwaiter();
				awaiter.OnCompleted(delegate { callback(ReadValue(awaiter.GetResult())); });
			});
		}

		public uint ReadLive()
		{
			ReadHttp(out var task);
			task.Wait();
			var str = task.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			return ReadValue(str);
		}
	}
}