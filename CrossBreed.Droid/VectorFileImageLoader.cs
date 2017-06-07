using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Support.Graphics.Drawable;
using MvvmCross.Plugins.DownloadCache;
using MvvmCross.Plugins.DownloadCache.Droid;

namespace CrossBreed.Droid {
	class VectorFileImageLoader: IMvxLocalFileImageLoader<Bitmap> {
		private readonly IDictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();
		public async Task<MvxImage<Bitmap>> Load(string localPath, bool shouldCache, int width, int height) {
			if(cache.ContainsKey(localPath)) return new MvxAndroidImage(cache[localPath]);
			var baseValue = await new MvxAndroidLocalFileImageLoader().Load(localPath, shouldCache, width, height);
			if(baseValue.RawImage != null) return baseValue;
			if(!localPath.StartsWith("res:")) return null;
			try {
				var context = Application.Context;
				var id = context.Resources.GetIdentifier(localPath.Substring(4), "drawable", context.PackageName);
				var drawable = VectorDrawableCompat.Create(context.Resources, id, context.Theme);
				if(drawable == null) return null;
				var bitmap = Bitmap.CreateBitmap(drawable.IntrinsicWidth, drawable.IntrinsicHeight, Bitmap.Config.Argb8888);
				var canvas = new Canvas(bitmap);
				drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
				drawable.Draw(canvas);
				cache[localPath] = bitmap;
				return new MvxAndroidImage(bitmap);
			} catch {
				return null;
			}
		}
	}
}