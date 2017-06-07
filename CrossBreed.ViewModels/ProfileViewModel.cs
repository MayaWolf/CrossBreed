using System;
using System.Collections.Generic;
using System.Linq;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.Entities.ApiResponses;

namespace CrossBreed.ViewModels {
	public class ProfileViewModel {
		private static readonly IComparer<Kink> customsFirstComparer = Comparer<Kink>.Create((x, y) => {
			var types = Comparer<bool>.Default.Compare(x is CustomKink, y is CustomKink);
			return types != 0 ? -types : Comparer<string>.Default.Compare(x.Name, y.Name);
		});

		public SyntaxTreeNode Description { get; }
		public int Views { get; }
		public string CustomTitle { get; }
		public DateTime CreatedAt { get; }
		public DateTime UpdatedAt { get; }
		public IReadOnlyDictionary<KinkChoiceEnum, IReadOnlyCollection<Kink>> Kinks { get; }
		public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Info { get; }
		public IReadOnlyCollection<Image> Images { get; }
		public IReadOnlyDictionary<int, InlineImage> InlineImages { get; }
		public string Subtitle { get; }

		internal ProfileViewModel(ProfileResponse response, MappingListResponse mappings) {
			Description = BbCodeParser.Parse(response.description);
			Views = response.views;
			CustomTitle = response.custom_title;
			CreatedAt = Helpers.UnixToDateTime(response.created_at);
			UpdatedAt = Helpers.UnixToDateTime(response.updated_at);
			var kinks = Enum.GetValues(typeof(KinkChoiceEnum)).Cast<KinkChoiceEnum>().ToDictionary(x => x, x => new LinkedList<Kink>());
			var responseKinks = response.kinks.ToDictionary(x => x.Key, x => x.Value);
			foreach(var kink in response.custom_kinks.Values) {
				var subKinks = new List<Kink>(kink.children.Count);
				foreach(var subKink in kink.children) {
					responseKinks.Remove(subKink);
					var map = mappings.kinks[subKink];
					subKinks.Add(new Kink(map.name, map.description));
				}
				kinks[kink.choice].AddLast(new CustomKink(kink.name, kink.description, subKinks));
			}
			foreach(var kink in responseKinks) {
				var map = mappings.kinks[kink.Key];
				kinks[kink.Value].AddLast(new Kink(map.name, map.description));
			}
			Kinks = kinks.ToDictionary(x => x.Key,
				x => (IReadOnlyCollection<Kink>) (response.customs_first ? x.Value.OrderBy(kink => kink, customsFirstComparer) : x.Value.OrderBy(kink => kink.Name)).ToList());

			var info = new Dictionary<int, Dictionary<string, string>>(mappings.infotag_groups.Count);
			foreach(var item in response.infotags) {
				var mapped = mappings.infotags[item.Key];
				if(!info.ContainsKey(mapped.group_id)) info.Add(mapped.group_id, new Dictionary<string, string>());
				info[mapped.group_id].Add(mapped.name, mapped.type == MappingListResponse.InfoTagType.text ? item.Value : mappings.listitems[int.Parse(item.Value)].value);
			}
			Info = info.ToDictionary(x => mappings.infotag_groups[x.Key], x => (IReadOnlyDictionary<string, string>) x.Value);
			Images = response.images.OrderBy(x => x.sort_order)
				.Select(x => new Image($"https://static.f-list.net/images/charimage/{x.image_id}.{x.extension}", x.description)).ToList();
			InlineImages = response.inlines.ToDictionary(x => x.Key, x => {
				var hash = x.Value.hash;
				return new InlineImage($"https://static.f-list.net/images/charinline/{hash.Substring(0, 2)}/{hash.Substring(2, 2)}/{hash}.{x.Value.extension}", x.Value.nsfw);
			});
		}

		public class Image {
			public string Url { get; }
			public string Description { get; }

			public Image(string url, string description) {
				Url = url;
				Description = description;
			}
		}

		public class Kink {
			public string Name { get; }
			public string Description { get; }

			public Kink(string name, string description) {
				Name = name;
				Description = description;
			}
		}

		public class CustomKink : Kink {
			public IReadOnlyCollection<Kink> Children { get; }

			public CustomKink(string name, string description, IReadOnlyCollection<Kink> children) : base(name, description) {
				Children = children;
			}
		}

		public class InlineImage {
			public string Url { get; }
			public bool IsNsfw { get; }

			public InlineImage(string url, bool isNsfw) {
				Url = url;
				IsNsfw = isNsfw;
			}
		}
	}
}