using System;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CherryBomb.Systems
{
	// The whole space-drift render pipeline in one pass. Ported from
	// space-drift/render.ts (renderFrame + drawWorld + drawStars). This is the
	// smooth-movement crux:
	//
	//   1. camX = shipRenderX - GameWidth/2  (shipRender = interpolated ship pos)
	//   2. flooredCam = floor(cam); frac = cam - flooredCam
	//   3. blit = -round(frac * Scale)  (whole SCREEN pixels)
	//   4. world content (particles) → worldRT (257x193) at floor(worldPos) -
	//      flooredCam  (WHOLE low-res pixels — never sub-pixel inside the RT)
	//   5. blit worldRT to the backbuffer at (blit) with scale = Scale
	//   6. ship drawn in SCREEN space, pinned to WINDOW/2, only its rotation changes
	//   7. stars: screen-space parallax BEHIND the world, sub-pixel at full res
	public sealed class WorldRenderingSystem : IDisposable
	{
		private const float DegToRad = MathF.PI / 180f;

		// Star wrap spans (screen pixels): a screen-plus-2 tile, ×Scale.
		private const int StarWrapW = (Constants.GameWidth + 2) * Constants.Scale;
		private const int StarWrapH = (Constants.GameHeight + 2) * Constants.Scale;

		// Ship "standard" frame in shmup.png (tile col 2, row 0).
		private static readonly Rectangle ShipFrame = new(16, 0, 8, 8);
		private static readonly Vector2 ShipOrigin = new(4f, 4f);

		private readonly World _world;
		private readonly Entity _ship;
		private readonly GraphicsDevice _device;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _shmup;
		private readonly RenderTarget2D _worldRT;

		// Native 1024×768 scene. PASS C bilinear-upscales it to fill the (DPI-sized)
		// backbuffer, so the window is a comfortable physical size on a high-DPI
		// display without the game logic caring about the window size.
		private readonly RenderTarget2D _sceneRT;
		private readonly Texture2D _pixel;

		private readonly QueryDescription _particleQuery = new QueryDescription().WithAll<
			Particle,
			Transform
		>();
		private readonly QueryDescription _starQuery = new QueryDescription().WithAll<
			Star,
			Transform,
			Pulse
		>();

		public WorldRenderingSystem(
			World world,
			Entity ship,
			GraphicsDevice device,
			SpriteBatch spriteBatch,
			Texture2D shmup
		)
		{
			_world = world;
			_ship = ship;
			_device = device;
			_spriteBatch = spriteBatch;
			_shmup = shmup;

			_worldRT = new RenderTarget2D(device, Constants.CanvasWidth, Constants.CanvasHeight);
			_sceneRT = new RenderTarget2D(device, Constants.WindowWidth, Constants.WindowHeight);
			_pixel = new Texture2D(device, 1, 1);
			_pixel.SetData([Color.White]);
		}

		// JS Math.round (half toward +inf), so the blit matches the source exactly
		// (C# Math.Round is banker's rounding).
		private static int RoundHalfUp(float v) => (int)MathF.Floor(v + 0.5f);

		// wrap(value, range) into [0, range) — matches math.ts wrap.
		private static int Wrap(int value, int range)
		{
			int r = value % range;
			return r < 0 ? r + range : r;
		}

		private static Color FlameColor(float t)
		{
			if (t > 0.75f)
				return Palette.Yellow;
			if (t > 0.5f)
				return Palette.Orange;
			if (t > 0.25f)
				return Palette.Red;
			return Palette.DarkPurple;
		}

		private static Color SmokeColor(float t)
		{
			if (t > 0.6f)
				return Palette.LightGray;
			if (t > 0.3f)
				return Palette.DarkGray;
			return Palette.DarkBlue;
		}

		public void Draw(float alpha, bool subpixel)
		{
			// Interpolated ship transform (render position).
			ref var tf = ref _world.Get<Transform>(_ship);
			ref var prev = ref _world.Get<Previous>(_ship);

			float shipX = MathHelper.Lerp(prev.Position.X, tf.Position.X, alpha);
			float shipY = MathHelper.Lerp(prev.Position.Y, tf.Position.Y, alpha);
			float shipRot = MathHelper.Lerp(prev.Rotation, tf.Rotation, alpha);

			// Camera stays smooth; the frac is split off into the blit offset.
			float camX = shipX - Constants.GameWidth / 2f;
			float camY = shipY - Constants.GameHeight / 2f;

			int flooredCamX = (int)MathF.Floor(camX);
			int flooredCamY = (int)MathF.Floor(camY);
			float fracX = camX - flooredCamX;
			float fracY = camY - flooredCamY;
			// Sub-pixel ON: express the camera fraction as whole SCREEN pixels of blit
			// offset (the smooth-movement trick). OFF: integer camera, no blit — the
			// jittery baseline for comparison (matches the demos' [p] toggle).
			int blitX = subpixel ? -RoundHalfUp(fracX * Constants.Scale) : 0;
			int blitY = subpixel ? -RoundHalfUp(fracY * Constants.Scale) : 0;

			// ── PASS A: world content (exhaust particles) → low-res RT ───────────
			// Whole low-res pixels only; cleared transparent so the stars behind
			// show through the RT blit.
			_device.SetRenderTarget(_worldRT);
			_device.Clear(Color.Transparent);

			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null,
				null,
				null
			);
			_world.Query(
				in _particleQuery,
				(ref Particle p, ref Transform ptf) =>
				{
					float t = 1f - p.Age / p.MaxAge;
					Color color = p.Kind == ParticleKind.Smoke ? SmokeColor(t) : FlameColor(t);
					int px = (int)MathF.Floor(ptf.Position.X) - flooredCamX;
					int py = (int)MathF.Floor(ptf.Position.Y) - flooredCamY;
					int size = (int)p.Size;
					_spriteBatch.Draw(_pixel, new Rectangle(px, py, size, size), color);
				}
			);
			_spriteBatch.End();

			// ── PASS B: native scene (1024×768) ──────────────────────────────────
			_device.SetRenderTarget(_sceneRT);
			_device.Clear(Palette.SpaceColor);

			// Above StreakThreshold, fast stars become motion lines along the ship's
			// velocity (source drawStars) so high-speed flight / boost streaks
			// instead of strobing. Direction/length from the ship's current velocity.
			Vector2 shipVel = _world.Get<Velocity>(_ship).Value;
			float speed = shipVel.Length();
			bool streaking = speed > Constants.StreakThreshold;
			float dirX = streaking ? shipVel.X / speed : 0f;
			float dirY = streaking ? shipVel.Y / speed : 0f;
			float streakAngle = streaking ? MathF.Atan2(dirY, dirX) : 0f;
			float baseLen = streaking
				? MathF.Min(
					(speed - Constants.StreakThreshold) * Constants.StreakK,
					Constants.StreakMax
				)
				: 0f;

			// Stars: screen-space parallax, drawn BEHIND the world, sub-pixel at
			// full res, wrapped toroidally.
			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null,
				null,
				null
			);
			_world.Query(
				in _starQuery,
				(ref Star star, ref Transform stf, ref Pulse pulse) =>
				{
					float worldX = stf.Position.X - camX * star.Depth;
					float worldY = stf.Position.Y - camY * star.Depth;
					// Sub-pixel: floor at full res; else floor at low-res then ×Scale.
					int rawX = subpixel
						? (int)MathF.Floor(worldX * Constants.Scale)
						: (int)MathF.Floor(worldX) * Constants.Scale;
					int rawY = subpixel
						? (int)MathF.Floor(worldY * Constants.Scale)
						: (int)MathF.Floor(worldY) * Constants.Scale;
					int sx = Wrap(rawX, StarWrapW) - Constants.Scale;
					int sy = Wrap(rawY, StarWrapH) - Constants.Scale;

					float brightness = 1f - pulse.Amplitude * (0.5f + 0.5f * MathF.Sin(pulse.Time));
					Color tint = Palette.Scale(star.Color, brightness);
					int size = (int)star.Size * Constants.Scale;

					if (streaking)
					{
						// Nearer stars (higher depth) streak longer → depth-cued speed.
						float len = baseLen * star.Depth * Constants.Scale;
						var center = new Vector2(sx + size * 0.5f, sy + size * 0.5f);
						// 1×1 pixel stretched to len×size, pivoted at its left-center so
						// it extends from the star centre along the velocity direction.
						_spriteBatch.Draw(
							_pixel,
							center,
							null,
							tint,
							streakAngle,
							new Vector2(0f, 0.5f),
							new Vector2(len, size),
							SpriteEffects.None,
							0f
						);
					}
					else
					{
						_spriteBatch.Draw(_pixel, new Rectangle(sx, sy, size, size), tint);
					}
				}
			);
			_spriteBatch.End();

			// World RT blit: whole-pixel offset (blit), scaled ×Scale.
			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null,
				null,
				null
			);
			_spriteBatch.Draw(
				_worldRT,
				new Vector2(blitX, blitY),
				null,
				Color.White,
				0f,
				Vector2.Zero,
				(float)Constants.Scale,
				SpriteEffects.None,
				0f
			);
			_spriteBatch.End();

			// Ship: pinned to view centre, only rotation changes (world slides
			// under it). Drawn after the world blit.
			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null,
				null,
				null
			);
			_spriteBatch.Draw(
				_shmup,
				new Vector2(Constants.WindowWidth / 2f, Constants.WindowHeight / 2f),
				ShipFrame,
				Color.White,
				shipRot * DegToRad,
				ShipOrigin,
				(float)Constants.Scale,
				SpriteEffects.None,
				0f
			);
			_spriteBatch.End();

			// ── PASS C: bilinear-upscale the native scene to fill the (DPI-sized)
			// backbuffer. LinearClamp = the smooth non-integer upscale the OS does
			// for a DPI-unaware app (e.g. Love2D), so the window is comfortably
			// sized. Internal pixel art stays crisp (rendered PointClamp above). ────
			_device.SetRenderTarget(null);
			_device.Clear(Palette.SpaceColor);

			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Opaque,
				SamplerState.LinearClamp,
				null,
				null,
				null,
				null
			);
			_spriteBatch.Draw(
				_sceneRT,
				new Rectangle(
					0,
					0,
					_device.PresentationParameters.BackBufferWidth,
					_device.PresentationParameters.BackBufferHeight
				),
				Color.White
			);
			_spriteBatch.End();
		}

		public void Dispose()
		{
			_worldRT?.Dispose();
			_sceneRT?.Dispose();
			_pixel?.Dispose();
		}
	}
}
