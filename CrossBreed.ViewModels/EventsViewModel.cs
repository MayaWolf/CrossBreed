using System;
using System.Diagnostics;
using System.Globalization;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ServerMessages;
using ML.AppBase;
using ML.Collections;

namespace CrossBreed.ViewModels {
	public class EventsViewModel : BaseViewModel {
		public IObservableList<EventViewModel> Events { get; }
		private readonly ObservableList<EventViewModel> events = new ObservableList<EventViewModel>();

		public EventsViewModel(IEventManager eventManager) {
			Events = new UIThreadObservableList<EventViewModel>(events);
			eventManager.NewEvent += e => events.Add(new EventViewModel(e));
		}
	}

	public class EventViewModel : MessageViewModel {
		public override SyntaxTreeNode Formatted { get; }
		public Event Event { get; }

		public EventViewModel(Event e) {
			Event = e;
			var text = "[" + e.Time.ToString(e.Time.Date != DateTime.Now.Date ? "g" : "t", CultureInfo.CurrentUICulture) + "] ";
			switch(e) {
				case LoginEvent loginEvent:
					text += string.Format(Strings.Events_CharacterOnline, $"[user]{loginEvent.Character.Name}[/user]");
					break;
				case LogoutEvent logoutEvent:
					text += string.Format(Strings.Events_CharacterOffline, $"[user]{logoutEvent.Character.Name}[/user]");
					break;
				case BroadcastEvent broadcastEvent:
					text += string.Format(Strings.Events_Broadcast, $"[user]{broadcastEvent.Character.Name}[/user]", broadcastEvent.Message);
					break;
				case StatusEvent statusEvent:
					text += string.Format(string.IsNullOrWhiteSpace(statusEvent.StatusMessage) ? Strings.Events_Status : Strings.Events_Status_Message,
						$"[user]{statusEvent.Character.Name}[/user]", statusEvent.Status, statusEvent.StatusMessage);
					break;
				case ErrorEvent errorEvent:
					text += "[color=red]" + string.Format(Strings.Events_Error, errorEvent.Message) + "[/color]";
					break;
				case NoteEvent noteEvent:
					text += string.Format(Strings.Events_Note,
						$"[user]{noteEvent.Character.Name}[/user]", "https://www.f-list.net/view_note.php?note_id=" + noteEvent.Id, noteEvent.Title);
					break;
				case MentionEvent highlightEvent:
					text += string.Format(Strings.Events_Highlight, $"[session={highlightEvent.Channel.Name}]{highlightEvent.Channel.Id}[/session]");
					break;
				case ChannelJoinEvent joinEvent:
					text += string.Format(Strings.Events_ChannelJoined,
						$"[user]{joinEvent.Member.Character.Name}[/user]", $"[session={joinEvent.Channel.Name}]{joinEvent.Channel.Id}[/session]");
					break;
				case ChannelLeaveEvent leaveEvent:
					text += string.Format(Strings.Events_ChannelLeft,
						$"[user]{leaveEvent.Member.Character.Name}[/user]", $"[session={leaveEvent.Channel.Name}]{leaveEvent.Channel.Id}[/session]");
					break;
				case InviteEvent inviteEvent:
					text += string.Format(Strings.Events_Invite, inviteEvent.Sender.Name, $"[session={inviteEvent.Channel.Name}]{inviteEvent.Channel.Id}[/session]");
					break;
				case SysEvent sysEvent:
					text += string.Format(Strings.Events_Sys, sysEvent.Message);
					break;
				case RtbEvent rtbEvent:
					if(rtbEvent.Type == ServerRtb.Type.comment) {
						var targetId = rtbEvent.Payload.Value<string>("target_id");
						var url = "https://www.f-list.net/";
						var targetType = rtbEvent.Payload.Value<ServerRtb.CommentTargetType>("target_type");
						switch(targetType) {
							case ServerRtb.CommentTargetType.newspost:
								url += $"newspost/{targetId}/#Comment";
								break;
							case ServerRtb.CommentTargetType.bugreport:
								url += $"view_bugreport.php?id=/{targetId}/#";
								break;
							case ServerRtb.CommentTargetType.changelog:
								url += $"log.php?id=/{targetId}/#";
								break;
							case ServerRtb.CommentTargetType.feature:
								url += $"vote.php?id=/{targetId}/#";
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
						var str = rtbEvent.Payload.Value<int>("parent_id") == 0 ? Strings.Events_RtbCommentReply : Strings.Events_RtbComment;
						text += string.Format(str, $"[user]{rtbEvent.Character.Name}[/user]", Strings.ResourceManager.GetString("Events_RtbComment_" + targetType),
							$"[url={url}]{rtbEvent.Payload.Value<string>("target")}");
					} else {
						string link;
						var title = rtbEvent.Payload.Value<string>("title");
						var id = rtbEvent.Payload.Value<string>("id");
						switch(rtbEvent.Type) {
							case ServerRtb.Type.grouprequest:
								link = $"[url=https://www.f-list.net/panel/group_requests.php]{title}[/url]";
								break;
							case ServerRtb.Type.bugreport:
								link = $"[url=https://www.f-list.net/panel/view_ticket.php?id={id}]{title}[/url]";
								break;
							case ServerRtb.Type.helpdeskticket:
								link = $"[url=https://www.f-list.net/panel/view_ticket.php?id={id}]{title}[/url]";
								break;
							case ServerRtb.Type.helpdeskreply:
								link = $"https://www.f-list.net/panel/view_ticket.php?id={id}";
								break;
							case ServerRtb.Type.featurerequest:
								link = $"[url=https://www.f-list.net/panel/fid.php?id={id}]{title}[/url]";
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
						text += string.Format(Strings.ResourceManager.GetString("Events_Rtb_" + rtbEvent.Type), $"[user]{rtbEvent.Character.Name}[/user]", link);
					}
					break;
				default:
					return;
			}
			Formatted = BbCodeParser.Parse(text);
		}
	}
}