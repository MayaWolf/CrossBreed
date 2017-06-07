using System.Collections.Generic;
using System.Reflection;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using ML.AppBase.Droid;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Platform;
using MvvmCross.Plugins.DownloadCache;

namespace CrossBreed.Droid {
	public class Setup : MLSetup<ViewModels.App> {
		public Setup(Context applicationContext) : base(applicationContext) {}


		protected override IEnumerable<Assembly> AndroidViewAssemblies =>
			new List<Assembly>(base.AndroidViewAssemblies) { typeof(MvvmCross.Droid.Support.V7.RecyclerView.MvxRecyclerView).Assembly };

		protected override void InitializeLastChance() {
			base.InitializeLastChance();
			MvvmCross.Plugins.File.PluginLoader.Instance.EnsureLoaded();
			MvvmCross.Plugins.DownloadCache.PluginLoader.Instance.EnsureLoaded();
			Acr.Settings.Settings.InitRoaming(ApplicationContext.PackageName);
			Mvx.GetSingleton<IMvxTargetBindingFactoryRegistry>().RegisterCustomBindingFactory<TextView>("BBCode", view => new BBCodeBinding(view));
			Mvx.ConstructAndRegisterSingleton<IMvxLocalFileImageLoader<Bitmap>, VectorFileImageLoader>();
		}
	}
}