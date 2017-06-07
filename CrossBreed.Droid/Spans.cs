using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Java.Lang.Reflect;

namespace CrossBreed.Droid {
	public class OutlineSpan: CharacterStyle, IUpdateAppearance {
		public override void UpdateDrawState(TextPaint tp) {
			tp.SetShadowLayer(3, 2, 2, Color.Black);
		}
	}

	public class ColoredUnderlineSpan : CharacterStyle, IUpdateAppearance {
		private readonly int color;
		private static readonly Method method = Java.Lang.Class.FromType(typeof(TextPaint)).GetMethod("setUnderlineText", Java.Lang.Integer.Type, Java.Lang.Float.Type);

		public ColoredUnderlineSpan(int color) {
			this.color = color;
		}

		public override void UpdateDrawState(TextPaint tp) {
			method.Invoke(tp, color, 1f);
		}
	}
}