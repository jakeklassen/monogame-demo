// global:: qualifiers because this file's own namespace is CherryBomb.Android, which
// would otherwise shadow the Android.* SDK namespaces during name resolution.
using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.OS;
using global::Android.Views;
using Microsoft.Xna.Framework;

namespace CherryBomb.Android
{
	// Android entry point for CherryBomb on Android TV (NVIDIA Shield, arm64).
	//
	// Controller-first: this declares both the standard LAUNCHER category and the
	// LEANBACK_LAUNCHER category (see AndroidManifest.xml) so the app appears on the
	// Shield's TV home screen. There is no touch UI; gameplay/menu input is gamepad
	// (handled in the shared game code) plus keyboard for desktop parity.
	//
	// The shared Game1 owns all graphics/screen setup; this Activity only hosts it:
	// it news up Game1, pulls the Android game View out of the game's services, sets
	// it as the content view, and starts the loop.
	[Activity(
		Label = "Cherry Bomb",
		MainLauncher = true,
		Icon = "@drawable/icon",
		AlwaysRetainTaskState = true,
		LaunchMode = LaunchMode.SingleInstance,
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		Theme = "@style/Theme.Splash",
		ConfigurationChanges = ConfigChanges.Orientation
			| ConfigChanges.Keyboard
			| ConfigChanges.KeyboardHidden
			| ConfigChanges.ScreenSize
			| ConfigChanges.ScreenLayout
			| ConfigChanges.UiMode
	)]
	// MainLauncher=true above emits the MAIN + LAUNCHER intent filter (app drawer on
	// phones/tablets). This second filter adds LEANBACK_LAUNCHER, which is what makes
	// the app appear on the Android TV / NVIDIA Shield home screen. Both are merged
	// into the final AndroidManifest.xml.
	[IntentFilter(
		new[] { Intent.ActionMain },
		Categories = new[] { "android.intent.category.LEANBACK_LAUNCHER" }
	)]
	public class MainActivity : AndroidGameActivity
	{
		private Game1 _game;
		private View _view;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			_game = new Game1();
			_view = _game.Services.GetService(typeof(View)) as View;

			SetContentView(_view);

			// Fullscreen / immersive: hide the system bars so the 128x128 logical
			// view (letterboxed by the BoxingViewportAdapter) fills the TV screen.
			GoFullscreenImmersive();

			_game.Run();
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			base.OnWindowFocusChanged(hasFocus);

			if (hasFocus)
			{
				GoFullscreenImmersive();
			}
		}

		private void GoFullscreenImmersive()
		{
			if (Window == null)
			{
				return;
			}

#pragma warning disable CA1422 // SystemUiVisibility is the broadly-compatible path (API 21+).
			Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
				SystemUiFlags.LayoutStable
				| SystemUiFlags.LayoutHideNavigation
				| SystemUiFlags.LayoutFullscreen
				| SystemUiFlags.HideNavigation
				| SystemUiFlags.Fullscreen
				| SystemUiFlags.ImmersiveSticky
			);
#pragma warning restore CA1422
		}
	}
}
