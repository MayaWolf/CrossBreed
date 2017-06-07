using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrossBreed.Chat;
using CrossBreed.Entities;

namespace CrossBreed.ViewModels {
	public class MessageSerializer {
		private readonly ICharacterManager characterManager;
		private const long dayTicks = 864000000000L;
		private const long maxIndexPosition = uint.MaxValue * 1024L;

		public MessageSerializer(ICharacterManager characterManager) {
			this.characterManager = characterManager;
		}

		public void Write(Stream stream, Message message) {
			var time = (message.Time.Ticks - Helpers.UnixEpoch.Ticks) / 10000000;
			stream.WriteByte((byte) (time >> 24));
			stream.WriteByte((byte) (time >> 16));
			stream.WriteByte((byte) (time >> 8));
			stream.WriteByte((byte) time);
			stream.WriteByte((byte) message.MessageType);

			var size = 5;

			var sender = message.Sender.Name;
			var text = message.Text;
			var bytes = new byte[Encoding.UTF8.GetMaxByteCount(Math.Max(sender.Length, text.Length))];
			var length = Encoding.UTF8.GetBytes(sender, 0, sender.Length, bytes, 0);
			if(length > byte.MaxValue) throw new ArgumentException("Character name too long"); 
			stream.WriteByte((byte) length);
			stream.Write(bytes, 0, length);
			size += length + 1;

			length = Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
			if(length > ushort.MaxValue) throw new ArgumentException("Message text too long");
			stream.WriteByte((byte) (length >> 8));
			stream.WriteByte((byte) length);
			stream.Write(bytes, 0, length);
			size += length + 2;

			stream.WriteByte((byte) (size >> 8));
			stream.WriteByte((byte) size);
		}

		public Message Read(Stream stream) {
			var message = DoRead(stream);
			stream.Seek(2, SeekOrigin.Current);
			return message;
		}

		private Message DoRead(Stream stream) {
			var time = stream.ReadByte() << 24 | stream.ReadByte() << 16 | stream.ReadByte() << 8 | stream.ReadByte();
			var date = new DateTime((long) time * 10000000 + Helpers.UnixEpoch.Ticks);
			var type = (Message.Type) stream.ReadByte();
			var length = stream.ReadByte();
			var bytes = new byte[length];
			stream.Read(bytes, 0, length);
			var sender = Encoding.UTF8.GetString(bytes, 0, length);

			length = stream.ReadByte() << 8 | stream.ReadByte();
			bytes = new byte[length];
			stream.Read(bytes, 0, length);
			var text = Encoding.UTF8.GetString(bytes, 0, length);

			return new Message(type, characterManager.GetCharacter(sender), date, text);
		}

		public IEnumerable<Message> Load(Stream stream) {
			while(true) {
				yield return DoRead(stream);
				var pos = stream.Position;
				if(pos > stream.Length - 3) break;
				stream.Position = pos + 2;
			}
		}

		public IReadOnlyList<Message> LoadReverse(Stream stream, int count) {
			if(stream.Position == 0) return new Message[0];
			var messages = new Message[count];
			while(count > 0) {
				count -= 1;
				stream.Seek(-2, SeekOrigin.Current);
				var size = stream.ReadByte() << 8 | stream.ReadByte();
				var pos = stream.Seek(-size - 2, SeekOrigin.Current);
				messages[count] = DoRead(stream);
				stream.Position = pos;
				if(pos == 0) break;
			}
			if(count == 0) return messages;
			return new ArraySegment<Message>(messages, count, messages.Length - count);
		}

		public void WriteIndex(Stream stream, DateTime date, long position) {
			var time = (date.Ticks - Helpers.UnixEpoch.Ticks) / dayTicks;
			if(time < ushort.MinValue || time > ushort.MaxValue) throw new ArgumentException("Date not in valid range");
			stream.WriteByte((byte) (time >> 8));
			stream.WriteByte((byte) time);

			if(position > maxIndexPosition) throw new ArgumentException("Invalid index position");
			stream.WriteByte((byte) (position >> 32));
			stream.WriteByte((byte) (position >> 24));
			stream.WriteByte((byte) (position >> 16));
			stream.WriteByte((byte) (position >> 8));
			stream.WriteByte((byte) position);
		}

		public Dictionary<DateTime, long> ReadIndex(Stream stream) {
			var size = (int) (stream.Length / 7);
			var dictionary = new Dictionary<DateTime, long>(size + 2);
			for(var i = 0; i < size; ++i) {
				var time = new DateTime((stream.ReadByte() << 8 | stream.ReadByte()) * dayTicks + Helpers.UnixEpoch.Ticks);
				var pos = stream.ReadByte() << 32 | stream.ReadByte() << 24 | stream.ReadByte() << 16 | stream.ReadByte() << 8 | stream.ReadByte();
				dictionary.Add(time, pos);
			}
			return dictionary;
		}

		public void RegenerateIndex(Stream file, Stream index) {
			var date = DateTime.MinValue;
			var pos = 0L;
			foreach(var message in Load(file)) {
				if(date != message.Time.Date) {
					date = message.Time.Date;
					WriteIndex(index, date, pos);
				}
				pos = file.Position;
			}
		}
	}
}