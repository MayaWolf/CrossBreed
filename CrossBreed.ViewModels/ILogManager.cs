using System;
using System.Collections.Generic;
using CrossBreed.Entities;

namespace CrossBreed.ViewModels {
	public interface ILogManager {
		string GetLogId(Channel channel);
		string GetLogId(Character character);
		IEnumerable<string> GetAllLogIds();
		IEnumerable<DateTime> GetDays(string logId, bool ads);
		IReadOnlyList<Message> LoadDay(string logId, bool ads, DateTime time);
		IReadOnlyList<Message> Load(string logId, bool ads, int count);
		IReadOnlyList<Message> Load(string logId, bool ads, int count, ref long position);
		IReadOnlyList<Message> LoadReverse(string logId, bool ads, int count);
		IReadOnlyList<Message> LoadReverse(string logId, bool ads, int count, ref long position);
	}
}