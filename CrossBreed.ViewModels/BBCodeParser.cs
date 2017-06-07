using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeKicker.BBCode;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.Entities;

namespace CrossBreed.ViewModels {
	public class BbCodeParser {
		private static readonly BBTag[] knownTags = {
			CreateTag("b"),
			CreateTag("i"),
			CreateTag("u"),
			CreateTag("s"),
			CreateTag("sup"),
			CreateTag("sub"),
			CreateTag("url", true, new BBAttribute("href", "")),
			CreateTag("big"),
			CreateTag("small"),
			new BBTag("noparse") { StopProcessing = true },
			CreateTag("color", true, new BBAttribute("color", "")),
			CreateTag("user", false),
			CreateTag("icon", false),
			CreateTag("eicon", false),
			CreateTag("img", false, new BBAttribute("img", "")),
			CreateTag("session", true, new BBAttribute("session", "")),
			CreateTag("collapse", true, new BBAttribute("collapse", "")),
			CreateTag("indent"),
			CreateTag("heading"),
			CreateTag("left"),
			CreateTag("center"),
			CreateTag("right"),
			CreateTag("justify"),
			CreateTag("quote"),
			new BBTag("hr", BBTagClosingStyle.LeafElementWithoutContent)
		};
		private static BBCodeParser parser = CreateParser();

		static BbCodeParser() {
			UserSettings.Instance.General.DisallowedBbCodeChanged += () => parser = CreateParser();
		}

		private static BBCodeParser CreateParser() {
			var disallowed = UserSettings.Instance.General.DisallowedBbCode;
			return new BBCodeParser(knownTags.Where(x => !disallowed.Contains(x.Name)).ToList());
		}

		private static BBTag CreateTag(string name, bool allowChildren = true, params BBAttribute[] attributes) =>
			new BBTag(name, allowChildren, attributes) { GreedyAttributeProcessing = true };

		public static SyntaxTreeNode Parse(string message) {
			message = Regex.Replace(message, @"(?<!\[url\=)(?<!\[url\])(http|https|ftp)\://[a-zA-Z0-9\-]+\.[a-zA-Z0-9\-\.]+(:[0-9]+)?/[a-zA-Z0-9\-\._\?\,\'/\\\+&%\$#\=~]*",
				"[url=$0]$0[/url]");
			return parser.ParseSyntaxTree(message);
		}
	}
}