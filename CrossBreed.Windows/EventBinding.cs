using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace CrossBreed.Windows {
	public class EventBindingExtension : MarkupExtension {
		public string Command { get; set; }

		public EventBindingExtension() {}

		public EventBindingExtension(string command) {
			Command = command;
		}

		public override object ProvideValue(IServiceProvider serviceProvider) {
			var targetProvider = (IProvideValueTarget) serviceProvider.GetService(typeof(IProvideValueTarget));

			if(string.IsNullOrWhiteSpace(Command)) throw new ArgumentException(nameof(Command));

			var handlerType = ((EventInfo) targetProvider.TargetProperty).EventHandlerType;
			var handlerInfo = handlerType.GetMethod("Invoke");
			var method = new DynamicMethod("", handlerInfo.ReturnType, handlerInfo.GetParameters().Select(x => x.ParameterType).ToArray());

			var gen = method.GetILGenerator();
			gen.Emit(OpCodes.Ldarg, 0);
			gen.Emit(OpCodes.Ldarg, 1);
			gen.Emit(OpCodes.Ldstr, Command);
			gen.Emit(OpCodes.Call, handlerMethod);
			gen.Emit(OpCodes.Ret);

			return method.CreateDelegate(handlerType);
		}

		private static readonly MethodInfo handlerMethod = typeof(EventBindingExtension).GetMethod("HandleEvent", new[] { typeof(object), typeof(EventArgs), typeof(string) });

		public static void HandleEvent(object sender, EventArgs args, string cmdName) {
			var context = ((FrameworkElement) sender).DataContext;
			var cmdProp = context.GetType().GetProperty(cmdName);
			var cmd = (ICommand) cmdProp.GetValue(context);
			cmd.Execute(null);
		}
	}
}