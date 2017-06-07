using System.Runtime.Serialization;

namespace CrossBreed.Windows {
	[DataContract]
	public class LatestVersionData {
		[DataMember(Name = "version")]
		public int Version { get; set; }

		[DataMember(Name = "versionName")]
		public string VersionName { get; set; }

		[DataMember(Name = "url")]
		public string Url { get; set; }

		[DataMember(Name = "patcher")]
		public string Patcher { get; set; }

		[DataMember(Name = "patcherVersion")]
		public int PatcherVersion { get; set; }

		[DataMember(Name = "changelog")]
		public string Changelog { get; set; }

		public string ChangelogText { get; set; }

		[DataMember(Name = "fileHash")]
		public string FileHash { get; set; }
	}
}