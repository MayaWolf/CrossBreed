using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CrossBreed.Windows.Updater {
	public class UpdateChecker {

		public const string BaseUpdatePath = "https://dl.dropboxusercontent.com/sh/3kvq15r6h7hv26c/";
		public async Task<LatestVersionData> CheckForUpdate(int currentVersion) {
			using(var client = new WebClient { CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache) }) {
				var stream = await client.OpenReadTaskAsync(BaseUpdatePath + "AACFEXgvr1xI_aNjPsstNM0Pa/Windows/latest.json");
				if(stream == null) return null;
				var latest = new DataContractJsonSerializer(typeof(LatestVersionData)).ReadObject(stream) as LatestVersionData;
				if(latest?.Url == null || latest.Version <= currentVersion) return null;
				latest.ChangelogText = await client.DownloadStringTaskAsync(BaseUpdatePath + latest.Changelog);
				latest.Patcher = BaseUpdatePath + latest.Patcher;
				latest.Url = BaseUpdatePath + latest.Url;
				return latest;
			}
		}
	}
}
