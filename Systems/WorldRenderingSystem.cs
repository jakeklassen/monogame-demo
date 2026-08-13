using System;
using System.Collections.Generic;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using SpaceDrift.Components;

namespace SpaceDrift.Systems
{
	// The whole space-drift render pipeline. Ported from space-drift/render.ts
	// (renderFrame + drawWorld + drawStars + drawEntities + drawReticle +
	// drawMinimap + updatePlanetLight) and the HUD from main.ts.
	//
	// The smooth-movement crux: world content is drawn into a low-res RT at
	// floor(worldPos) - floor(cam) (whole low-res pixels), then that RT is blitted
	// ×Scale at a whole-SCREEN-pixel offset carrying the camera fraction. The whole
	// native scene renders into _sceneRT, which PASS C bilinear-upscales to fill
	// the (DPI-sized) backbuffer.
	public sealed class WorldRenderingSystem : IDisposable
	{
		private const float DegToRad = MathF.PI / 180f;

		private const int StarWrapW = (Constants.GameWidth + 2) * Constants.Scale;
		private const int StarWrapH = (Constants.GameHeight + 2) * Constants.Scale;

		// 8×8 sprite frames in shmup.png, addressed by (col, row) tiles.
		private static readonly Rectangle ShipStandard = new(16, 0, 8, 8); // (2, 0)
		private static readonly Rectangle ShipBankLeft = new(8, 0, 8, 8); // (1, 0)
		private static readonly Rectangle ShipBankRight = new(24, 0, 8, 8); // (3, 0)
		private static readonly Rectangle BulletFrame = new(48, 0, 8, 8); // (6, 0)
		private static readonly Rectangle EnemyFrame = new(88, 64, 8, 8); // (11, 8)
		private static readonly Vector2 SpriteOrigin = new(4f, 4f);

		// Bank-sprite smoothing (render.ts BANK_*): EMA + hysteresis + cross-fade.
		private const float BankSmooth = 0.15f;
		private const float BankEnter = 1.3f; // deg/step to start banking
		private const float BankExit = 0.4f; // deg/step to return to level
		private const float BankFadeTime = 0.09f; // seconds of cross-fade on a swap

		private readonly World _world;
		private readonly Entity _ship;
		private readonly GraphicsDevice _device;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _shmup;
		private readonly BitmapFont _font;
		private readonly Dictionary<string, Texture2D> _textures;
		private readonly Random _rng = new();

		private readonly RenderTarget2D _worldRT;
		private readonly RenderTarget2D _minimapRT;
		private readonly RenderTarget2D _sceneRT;

		// Shader-free CRT/bloom chain (toggle C), built at ¼ and ⅛ scene res for a
		// cheap bloom, plus generated scanline + vignette overlays.
		private readonly RenderTarget2D _bloomRT;
		private readonly RenderTarget2D _bloomRT2;
		private readonly Texture2D _scanlines;
		private readonly Texture2D _vignette;
		private readonly BlendState _multiply;

		private readonly Texture2D _pixel;
		private readonly Texture2D _shipLight; // white ship silhouette, for planet glow

		private readonly QueryDescription _particleQuery = new QueryDescription().WithAll<
			Particle,
			Transform
		>();
		private readonly QueryDescription _starQuery = new QueryDescription().WithAll<
			Star,
			Transform,
			Pulse
		>();
		private readonly QueryDescription _enemyQuery = new QueryDescription().WithAll<
			Enemy,
			Transform,
			Previous
		>();
		private readonly QueryDescription _bulletQuery = new QueryDescription().WithAll<
			Bullet,
			Transform,
			Previous
		>();
		private readonly QueryDescription _planetQuery = new QueryDescription().WithAll<
			Planet,
			Transform,
			Pulse
		>();

		private readonly List<Entity> _planets = [];
		private readonly List<Entity> _minimapEnemies = [];

		// Reticle breathing + bank-sprite state (persist across frames).
		private float _reticleAnim;
		private float _bankTurn;
		private int _bankState; // -1 left, 0 level, 1 right
		private Rectangle _bankFrame = ShipStandard;
		private Rectangle _fadeFrame = ShipStandard;
		private float _bankFade;

		public WorldRenderingSystem(
			World world,
			Entity ship,
			GraphicsDevice device,
			SpriteBatch spriteBatch,
			Texture2D shmup,
			BitmapFont font,
			Dictionary<string, Texture2D> textures
		)
		{
			_world = world;
			_ship = ship;
			_device = device;
			_spriteBatch = spriteBatch;
			_shmup = shmup;
			_font = font;
			_textures = textures;

			_worldRT = new RenderTarget2D(device, Constants.CanvasWidth, Constants.CanvasHeight);
			_minimapRT = new RenderTarget2D(
				device,
				Constants.MinimapDiameter,
				Constants.MinimapDiameter
			);
			_sceneRT = new RenderTarget2D(device, Constants.WindowWidth, Constants.WindowHeight);
			_bloomRT = new RenderTarget2D(
				device,
				Constants.WindowWidth / 4,
				Constants.WindowHeight / 4
			);
			_bloomRT2 = new RenderTarget2D(
				device,
				Constants.WindowWidth / 8,
				Constants.WindowHeight / 8
			);
			_pixel = new Texture2D(device, 1, 1);
			_pixel.SetData([Color.White]);
			_shipLight = BuildShipLight(device, shmup, ShipStandard);
			_scanlines = BuildScanlines(device);
			_vignette = BuildVignette(device, 128);
			// Multiply blend (src.rgb × dst.rgb) for the scanline + vignette overlays.
			_multiply = new BlendState
			{
				ColorSourceBlend = Blend.DestinationColor,
				ColorDestinationBlend = Blend.Zero,
				AlphaSourceBlend = Blend.DestinationAlpha,
				AlphaDestinationBlend = Blend.Zero,
			};
		}

		// A 1×3 vertical pattern: two bright lines then a darker one, tiled (PointWrap)
		// down the backbuffer under a multiply blend to lay down CRT scanlines.
		private static Texture2D BuildScanlines(GraphicsDevice device)
		{
			var tex = new Texture2D(device, 1, 3);
			var dim = new Color(0.6f, 0.6f, 0.6f, 1f);
			tex.SetData([Color.White, Color.White, dim]);
			return tex;
		}

		// A radial gradient (bright centre → ~0.5 at the corners), multiplied over the
		// backbuffer to darken the edges like a CRT vignette.
		private static Texture2D BuildVignette(GraphicsDevice device, int size)
		{
			var data = new Color[size * size];
			float c = (size - 1) / 2f;
			float maxD = MathF.Sqrt(2f) * c;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x - c;
					float dy = y - c;
					float d = MathF.Sqrt(dx * dx + dy * dy) / maxD; // 0 centre → 1 corner
					float b = MathHelper.Clamp(1f - 0.5f * d * d, 0.5f, 1f);
					data[y * size + x] = new Color(b, b, b, 1f);
				}
			}
			var tex = new Texture2D(device, size, size);
			tex.SetData(data);
			return tex;
		}

		// A white silhouette of the ship frame (opaque pixels → white, keeping
		// alpha) so a planet's hue can wash the whole hull via an additive draw.
		private static Texture2D BuildShipLight(
			GraphicsDevice device,
			Texture2D shmup,
			Rectangle frame
		)
		{
			var src = new Color[frame.Width * frame.Height];
			shmup.GetData(0, frame, src, 0, src.Length);
			for (int i = 0; i < src.Length; i++)
			{
				byte a = src[i].A;
				src[i] = new Color((byte)a, (byte)a, (byte)a, a); // premultiplied white
			}
			var tex = new Texture2D(device, frame.Width, frame.Height);
			tex.SetData(src);
			return tex;
		}

		private static int RoundHalfUp(float v) => (int)MathF.Floor(v + 0.5f);

		private static int Wrap(int value, int range)
		{
			int r = value % range;
			return r < 0 ? r + range : r;
		}

		private static Color FlameColor(float t) =>
			t > 0.75f ? Palette.Yellow
			: t > 0.5f ? Palette.Orange
			: t > 0.25f ? Palette.Red
			: Palette.DarkPurple;

		private static Color SmokeColor(float t) =>
			t > 0.6f ? Palette.LightGray
			: t > 0.3f ? Palette.DarkGray
			: Palette.DarkBlue;

		private Texture2D CircFill(int r) => _textures[$"circfill-{Math.Clamp(r, 1, 32)}"];

		private Texture2D Circ(int r) => _textures[$"circ-{Math.Clamp(r, 1, 32)}"];

		// Draw a filled/outline circle texture centred on (cx, cy) at the current
		// (whole-pixel) grid, tinted `color` (colours are premultiplied so a plain
		// AlphaBlend works for translucent fills like `color * alpha`).
		private void DrawCircFill(int cx, int cy, int r, Color color)
		{
			int n = Math.Clamp(r, 1, 32);
			_spriteBatch.Draw(CircFill(n), new Vector2(cx - n, cy - n), color);
		}

		public void Draw(
			float alpha,
			bool subpixel,
			Entity? lockTarget,
			bool charging,
			float dt,
			in HudState hud
		)
		{
			ref var tf = ref _world.Get<Transform>(_ship);
			ref var prev = ref _world.Get<Previous>(_ship);

			float shipX = MathHelper.Lerp(prev.Position.X, tf.Position.X, alpha);
			float shipY = MathHelper.Lerp(prev.Position.Y, tf.Position.Y, alpha);
			float shipRot = MathHelper.Lerp(prev.Rotation, tf.Rotation, alpha);
			float turnDelta = tf.Rotation - prev.Rotation;

			float camX = shipX - Constants.GameWidth / 2f;
			float camY = shipY - Constants.GameHeight / 2f;
			int flooredCamX = (int)MathF.Floor(camX);
			int flooredCamY = (int)MathF.Floor(camY);
			float fracX = camX - flooredCamX;
			float fracY = camY - flooredCamY;
			int blitX = subpixel ? -RoundHalfUp(fracX * Constants.Scale) : 0;
			int blitY = subpixel ? -RoundHalfUp(fracY * Constants.Scale) : 0;

			int viewLeft = flooredCamX;
			int viewTop = flooredCamY;
			int viewRight = flooredCamX + Constants.GameWidth + 1;
			int viewBottom = flooredCamY + Constants.GameHeight + 1;

			// Snapshot planet entities once (drawn in three places this frame).
			_planets.Clear();
			_world.Query(in _planetQuery, (Entity e, ref Planet p) => _planets.Add(e));

			// ── PASS A: world content → low-res RT (planets, exhaust, entities) ──
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

			DrawPlanets(flooredCamX, flooredCamY, viewLeft, viewTop, viewRight, viewBottom);

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

			_world.Query(
				in _enemyQuery,
				(ref Enemy en, ref Transform etf, ref Previous eprev) =>
				{
					if (en.RespawnTimer > 0f)
						return;
					float ex = MathHelper.Lerp(eprev.Position.X, etf.Position.X, alpha);
					float ey = MathHelper.Lerp(eprev.Position.Y, etf.Position.Y, alpha);
					float er = MathHelper.Lerp(eprev.Rotation, etf.Rotation, alpha);
					var pos = new Vector2(
						MathF.Floor(ex) - flooredCamX,
						MathF.Floor(ey) - flooredCamY
					);
					float scale = en.HitFlash > 0f ? 1.4f : 1f;
					_spriteBatch.Draw(
						_shmup,
						pos,
						EnemyFrame,
						Color.White,
						er * DegToRad,
						SpriteOrigin,
						scale,
						SpriteEffects.None,
						0f
					);
				}
			);

			_world.Query(
				in _bulletQuery,
				(Entity e, ref Bullet b, ref Transform btf, ref Previous bprev) =>
				{
					float bx = MathHelper.Lerp(bprev.Position.X, btf.Position.X, alpha);
					float by = MathHelper.Lerp(bprev.Position.Y, btf.Position.Y, alpha);
					float br = MathHelper.Lerp(bprev.Rotation, btf.Rotation, alpha);
					var pos = new Vector2(
						MathF.Floor(bx) - flooredCamX,
						MathF.Floor(by) - flooredCamY
					);
					Color tint = _world.Has<Homing>(e) ? Palette.Orange : Color.White;
					_spriteBatch.Draw(
						_shmup,
						pos,
						BulletFrame,
						tint,
						br * DegToRad,
						SpriteOrigin,
						1f,
						SpriteEffects.None,
						0f
					);
				}
			);

			DrawReticle(lockTarget, charging, dt, flooredCamX, flooredCamY);
			_spriteBatch.End();

			// ── PASS A.5: minimap → its own low-res RT ───────────────────────────
			if (hud.Minimap)
				DrawMinimap(shipX, shipY, shipRot);

			// ── PASS B: native 1024×768 scene ────────────────────────────────────
			_device.SetRenderTarget(_sceneRT);
			_device.Clear(Palette.SpaceColor);

			DrawStars(camX, camY, subpixel);

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

			DrawShip(shipRot, turnDelta, dt);
			DrawPlanetLight(shipX, shipY, shipRot);

			if (hud.Minimap)
			{
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
					_minimapRT,
					new Vector2(
						(Constants.GameWidth - Constants.MinimapMargin - Constants.MinimapDiameter)
							* Constants.Scale,
						Constants.MinimapMargin * Constants.Scale
					),
					null,
					Color.White,
					0f,
					Vector2.Zero,
					(float)Constants.Scale,
					SpriteEffects.None,
					0f
				);
				_spriteBatch.End();
			}

			// Bloom (toggle C): downsample the finished scene twice for a cheap glow,
			// composited additively in PASS C. Done while sceneRT is still resolvable.
			bool crt = hud.Crt;
			if (crt)
				BuildBloom();

			// ── PASS C: bilinear-upscale the scene to fill the backbuffer, with a
			// whole-frame boost shake applied as a present offset. ───────────────
			_device.SetRenderTarget(null);
			_device.Clear(Palette.SpaceColor);

			int bbW = _device.PresentationParameters.BackBufferWidth;
			int bbH = _device.PresentationParameters.BackBufferHeight;
			float shakeAmp = 0f;
			if (hud.BoostHeldTime > Constants.BoostShakeDelay)
			{
				float ease = MathF.Min(
					(hud.BoostHeldTime - Constants.BoostShakeDelay) / Constants.BoostShakeRamp,
					1f
				);
				shakeAmp = Constants.BoostShakeAmp * ease;
			}
			float fill = bbW / (float)Constants.WindowWidth;
			int shakeX = RoundHalfUp(
				(_rng.NextSingle() * 2f - 1f) * shakeAmp * Constants.Scale * fill
			);
			int shakeY = RoundHalfUp(
				(_rng.NextSingle() * 2f - 1f) * shakeAmp * Constants.Scale * fill
			);

			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Opaque,
				SamplerState.LinearClamp,
				null,
				null,
				null,
				null
			);
			_spriteBatch.Draw(_sceneRT, new Rectangle(shakeX, shakeY, bbW, bbH), Color.White);
			_spriteBatch.End();

			if (crt)
			{
				var full = new Rectangle(0, 0, bbW, bbH);

				// Additive bloom: the two downsampled levels glow bright areas.
				_spriteBatch.Begin(
					SpriteSortMode.Deferred,
					BlendState.Additive,
					SamplerState.LinearClamp,
					null,
					null,
					null,
					null
				);
				_spriteBatch.Draw(_bloomRT, full, Color.White * 0.35f);
				_spriteBatch.Draw(_bloomRT2, full, Color.White * 0.5f);
				_spriteBatch.End();

				// Scanlines: the 1×3 pattern tiled down the screen (multiply).
				_spriteBatch.Begin(
					SpriteSortMode.Deferred,
					_multiply,
					SamplerState.PointWrap,
					null,
					null,
					null,
					null
				);
				_spriteBatch.Draw(_scanlines, full, new Rectangle(0, 0, bbW, bbH), Color.White);
				_spriteBatch.End();

				// Vignette: radial darkening toward the edges (multiply).
				_spriteBatch.Begin(
					SpriteSortMode.Deferred,
					_multiply,
					SamplerState.LinearClamp,
					null,
					null,
					null,
					null
				);
				_spriteBatch.Draw(_vignette, full, Color.White);
				_spriteBatch.End();
			}

			// HUD on the backbuffer (NOT the scene) so the text stays crisp: the scene
			// is bilinear-upscaled (soft, on purpose), which would blur the font. Point
			// sampling + a scale to the backbuffer keeps it sharp and window-relative.
			DrawHud(hud, fill);
		}

		// Downsample the finished scene to ¼ then ⅛ res (bilinear = blur). On the
		// near-black space background this naturally emphasises bright pixels, so the
		// additive composite in PASS C reads as a threshold bloom.
		private void BuildBloom()
		{
			_device.SetRenderTarget(_bloomRT);
			_device.Clear(Color.Black);
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
				new Rectangle(0, 0, _bloomRT.Width, _bloomRT.Height),
				Color.White
			);
			_spriteBatch.End();

			_device.SetRenderTarget(_bloomRT2);
			_device.Clear(Color.Black);
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
				_bloomRT,
				new Rectangle(0, 0, _bloomRT2.Width, _bloomRT2.Height),
				Color.White
			);
			_spriteBatch.End();
		}

		// Planets: five stacked circles (glow, shadow disc, lit midtone, highlight,
		// spec dot). Ported from render.ts drawWorld.
		private void DrawPlanets(
			int flooredCamX,
			int flooredCamY,
			int viewLeft,
			int viewTop,
			int viewRight,
			int viewBottom
		)
		{
			foreach (var pe in _planets)
			{
				ref var ptf = ref _world.Get<Transform>(pe);
				ref var pl = ref _world.Get<Planet>(pe);
				ref var pulseC = ref _world.Get<Pulse>(pe);
				float r = pl.Radius;
				float x = ptf.Position.X;
				float y = ptf.Position.Y;
				if (
					x < viewLeft - r - 4
					|| x > viewRight + r + 4
					|| y < viewTop - r - 4
					|| y > viewBottom + r + 4
				)
					continue;

				int cx = (int)MathF.Floor(x) - flooredCamX;
				int cy = (int)MathF.Floor(y) - flooredCamY;
				float pulse = 0.5f + 0.5f * MathF.Sin(pulseC.Time);

				// Soft glow.
				DrawCircFill(
					cx,
					cy,
					RoundHalfUp(r + 2f + pulse),
					pl.Light * (0.05f + 0.05f * pulse)
				);
				// Shadow disc.
				DrawCircFill(cx, cy, RoundHalfUp(r), pl.Dark);
				// Lit midtone, offset toward the light.
				DrawCircFill(
					(int)MathF.Round(cx + Constants.LightDirX * r * 0.18f),
					(int)MathF.Round(cy + Constants.LightDirY * r * 0.18f),
					RoundHalfUp(r * 0.92f),
					pl.Base
				);
				// Highlight.
				DrawCircFill(
					(int)MathF.Round(cx + Constants.LightDirX * r * 0.4f),
					(int)MathF.Round(cy + Constants.LightDirY * r * 0.4f),
					RoundHalfUp(r * 0.5f),
					pl.Light
				);
				// Specular dot — solid square (circfill at radius 1 is a plus).
				int specR = Math.Max(1, RoundHalfUp(r * 0.14f));
				int specX = (int)MathF.Round(cx + Constants.LightDirX * r * 0.55f);
				int specY = (int)MathF.Round(cy + Constants.LightDirY * r * 0.55f);
				_spriteBatch.Draw(
					_pixel,
					new Rectangle(specX - specR, specY - specR, specR * 2, specR * 2),
					Color.White * 0.9f
				);
			}
		}

		private void DrawStars(float camX, float camY, bool subpixel)
		{
			Vector2 shipVel = _world.Get<Velocity>(_ship).Value;
			float speed = shipVel.Length();
			bool streaking = speed > Constants.StreakThreshold;
			float streakAngle = streaking ? MathF.Atan2(shipVel.Y, shipVel.X) : 0f;
			float baseLen = streaking
				? MathF.Min(
					(speed - Constants.StreakThreshold) * Constants.StreakK,
					Constants.StreakMax
				)
				: 0f;

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
						float len = baseLen * star.Depth * Constants.Scale;
						var center = new Vector2(sx + size * 0.5f, sy + size * 0.5f);
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
		}

		// Ship, pinned to view centre. Bank frame follows the smoothed turn with
		// hysteresis and a short cross-fade on each swap (render.ts bank logic).
		private void DrawShip(float shipRot, float turnDelta, float dt)
		{
			_bankTurn = _bankTurn * (1f - BankSmooth) + turnDelta * BankSmooth;
			if (_bankTurn > BankEnter)
				_bankState = 1;
			else if (_bankTurn < -BankEnter)
				_bankState = -1;
			else if (MathF.Abs(_bankTurn) < BankExit)
				_bankState = 0;

			Rectangle next =
				_bankState < 0 ? ShipBankLeft
				: _bankState > 0 ? ShipBankRight
				: ShipStandard;
			if (next != _bankFrame)
			{
				_fadeFrame = _bankFrame;
				_bankFade = 1f;
				_bankFrame = next;
			}
			_bankFade = MathF.Max(0f, _bankFade - dt / BankFadeTime);

			var center = new Vector2(Constants.WindowWidth / 2f, Constants.WindowHeight / 2f);

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
				center,
				_bankFrame,
				Color.White,
				shipRot * DegToRad,
				SpriteOrigin,
				(float)Constants.Scale,
				SpriteEffects.None,
				0f
			);
			// Outgoing frame dissolves out over the swap so the change doesn't pop.
			if (_bankFade > 0f)
			{
				_spriteBatch.Draw(
					_shmup,
					center,
					_fadeFrame,
					Color.White * _bankFade,
					shipRot * DegToRad,
					SpriteOrigin,
					(float)Constants.Scale,
					SpriteEffects.None,
					0f
				);
			}
			_spriteBatch.End();
		}

		// Planet light: nearby planets wash the hull with their hue via an additive
		// white silhouette. Ported from render.ts updatePlanetLight.
		private void DrawPlanetLight(float shipX, float shipY, float shipRot)
		{
			float r = 0f,
				g = 0f,
				b = 0f,
				dirX = 0f,
				dirY = 0f,
				total = 0f;
			foreach (var pe in _planets)
			{
				ref var ptf = ref _world.Get<Transform>(pe);
				ref var pl = ref _world.Get<Planet>(pe);
				float dx = ptf.Position.X - shipX;
				float dy = ptf.Position.Y - shipY;
				float dist = MathF.Sqrt(dx * dx + dy * dy);
				float range = pl.Radius * 5f + 30f;
				float surface = dist - pl.Radius;
				if (surface >= range)
					continue;
				float i = 1f - surface / range;
				if (i <= 0f)
					continue;
				i *= i;
				r += pl.Base.R / 255f * i;
				g += pl.Base.G / 255f * i;
				b += pl.Base.B / 255f * i;
				float inv = i / MathF.Max(dist, 0.001f);
				dirX += dx * inv;
				dirY += dy * inv;
				total += i;
			}

			if (total <= 0f)
				return;

			float ox = 0f,
				oy = 0f;
			float dlen = MathF.Sqrt(dirX * dirX + dirY * dirY);
			if (dlen > 0f)
			{
				float push = Constants.Scale * 0.5f * MathF.Min(total, 1f);
				ox = dirX / dlen * push;
				oy = dirY / dlen * push;
			}

			float a = MathF.Min(total, 1f) * 0.6f;
			// Additive blend multiplies source RGB by source alpha, so pass the base
			// hue at full RGB with alpha = a (a single application, matching pixi 'add').
			var tint = new Color(MathF.Min(r, 1f), MathF.Min(g, 1f), MathF.Min(b, 1f), a);
			var pos = new Vector2(
				Constants.WindowWidth / 2f + ox,
				Constants.WindowHeight / 2f + oy
			);

			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Additive,
				SamplerState.PointClamp,
				null,
				null,
				null,
				null
			);
			_spriteBatch.Draw(
				_shipLight,
				pos,
				null,
				tint,
				shipRot * DegToRad,
				SpriteOrigin,
				(float)Constants.Scale,
				SpriteEffects.None,
				0f
			);
			_spriteBatch.End();
		}

		// A ship-centred circular radar, top-right. Ported from render.ts
		// drawMinimap; rendered into its own low-res RT, blitted ×Scale in PASS B.
		private void DrawMinimap(float shipX, float shipY, float shipRot)
		{
			int r = Constants.MinimapRadius;
			float zoom = Constants.MinimapZoom;

			_minimapEnemies.Clear();
			_world.Query(
				in _enemyQuery,
				(Entity e, ref Enemy en) =>
				{
					if (en.RespawnTimer <= 0f)
						_minimapEnemies.Add(e);
				}
			);

			_device.SetRenderTarget(_minimapRT);
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

			// Backdrop disc, lifted just above the space colour.
			DrawCircFill(r, r, r, Palette.Scale(Palette.DarkBlue, 0.45f) * 0.85f);

			// Planets as dots.
			foreach (var pe in _planets)
			{
				ref var ptf = ref _world.Get<Transform>(pe);
				ref var pl = ref _world.Get<Planet>(pe);
				float dx = (ptf.Position.X - shipX) * zoom;
				float dy = (ptf.Position.Y - shipY) * zoom;
				int dotR = Math.Max(1, RoundHalfUp(pl.Radius * zoom));
				if (dx * dx + dy * dy > (r + dotR) * (r + dotR))
					continue;
				int ax = (int)MathF.Floor(r + dx);
				int ay = (int)MathF.Floor(r + dy);
				// Solid square dot — circfill at radius 1 is a plus, not a disc.
				int d = dotR * 2;
				_spriteBatch.Draw(_pixel, new Rectangle(ax - dotR, ay - dotR, d, d), pl.Base);
			}

			// Enemies as red blips, clamped to the rim if off-disc.
			foreach (var ee in _minimapEnemies)
			{
				ref var etf = ref _world.Get<Transform>(ee);
				float dx = (etf.Position.X - shipX) * zoom;
				float dy = (etf.Position.Y - shipY) * zoom;
				float dist = MathF.Sqrt(dx * dx + dy * dy);
				float max = r - 1;
				if (dist > max)
				{
					dx = dx / dist * max;
					dy = dy / dist * max;
				}
				_spriteBatch.Draw(
					_pixel,
					new Rectangle((int)MathF.Floor(r + dx), (int)MathF.Floor(r + dy), 1, 1),
					Palette.Red
				);
			}

			// The ship: the only white pixel on the map.
			_spriteBatch.Draw(_pixel, new Rectangle(r, r, 1, 1), Palette.White);

			// Heading tick riding the rim (approximated as blips along a short arc).
			float rad = shipRot * DegToRad;
			float headingAngle = MathF.Atan2(-MathF.Cos(rad), MathF.Sin(rad));
			int tickR = r - 1;
			float startA = headingAngle - Constants.MinimapTickSweep / 2f;
			for (int k = 0; k <= 8; k++)
			{
				float ang = startA + Constants.MinimapTickSweep * (k / 8f);
				int tx = (int)MathF.Round(r + tickR * MathF.Cos(ang));
				int ty = (int)MathF.Round(r + tickR * MathF.Sin(ang));
				_spriteBatch.Draw(_pixel, new Rectangle(tx, ty, 1, 1), Palette.Blue);
			}

			// Rim.
			_spriteBatch.Draw(Circ(r), new Vector2(0f, 0f), Palette.Lavender * 0.8f);

			_spriteBatch.End();
		}

		private static string Box(bool on) => on ? "[x]" : "[ ]";

		// HUD: fps / speed, homing-charge meter, boost-fuel meter, and the toggle
		// status line along the bottom. Ported from main.ts. Drawn into the scene
		// (so a later CRT pass would cover it). Text uses the pico-8 bitmap font.
		private void DrawHud(in HudState hud, float fill)
		{
			// Drawn to the backbuffer in scene (1024×768) coordinates scaled up by the
			// fill ratio, with PointClamp — same on-screen size as the scene, but crisp.
			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null,
				null,
				Matrix.CreateScale(fill)
			);

			var big = new Vector2(2f, 2f);
			var small = new Vector2(1.5f, 1.5f);

			_spriteBatch.DrawString(
				_font,
				$"fps {hud.Fps}",
				new Vector2(6f, 4f),
				Palette.White,
				0f,
				Vector2.Zero,
				big,
				SpriteEffects.None,
				0f
			);
			_spriteBatch.DrawString(
				_font,
				$"spd {hud.Speed}",
				new Vector2(6f, 22f),
				Palette.White,
				0f,
				Vector2.Zero,
				big,
				SpriteEffects.None,
				0f
			);

			// Homing charge meter.
			_spriteBatch.DrawString(
				_font,
				"charge",
				new Vector2(6f, 42f),
				Palette.White,
				0f,
				Vector2.Zero,
				small,
				SpriteEffects.None,
				0f
			);
			float chargeFrac = MathF.Min(1f, hud.ChargeSeconds / Constants.HomingChargeMax);
			Color chargeColor =
				hud.ChargeCount >= 8 ? Palette.Red
				: hud.ChargeCount >= 5 ? Palette.Orange
				: hud.ChargeCount >= 3 ? Palette.Yellow
				: Palette.DarkGray;
			DrawBar(
				Constants.ChargeBarX,
				Constants.ChargeBarY,
				Constants.ChargeBarW,
				Constants.ChargeBarH,
				chargeFrac,
				chargeColor
			);
			// Tier ticks at 1s and 2s (thirds of the window).
			foreach (float t in stackalloc[] { 1f / 3f, 2f / 3f })
			{
				_spriteBatch.Draw(
					_pixel,
					new Rectangle(
						Constants.ChargeBarX + (int)MathF.Floor(Constants.ChargeBarW * t),
						Constants.ChargeBarY - 1,
						1,
						Constants.ChargeBarH + 2
					),
					Palette.LightGray * 0.9f
				);
			}
			if (hud.ChargeCount > 0)
			{
				_spriteBatch.DrawString(
					_font,
					$"x{hud.ChargeCount}",
					new Vector2(Constants.ChargeBarX + Constants.ChargeBarW + 8f, 42f),
					Palette.Yellow,
					0f,
					Vector2.Zero,
					small,
					SpriteEffects.None,
					0f
				);
			}

			// Boost fuel meter.
			_spriteBatch.DrawString(
				_font,
				"boost",
				new Vector2(6f, 58f),
				Palette.White,
				0f,
				Vector2.Zero,
				small,
				SpriteEffects.None,
				0f
			);
			Color fuelColor =
				hud.Boosting ? Palette.Orange
				: hud.Fuel < 0.25f ? Palette.Red
				: Palette.Blue;
			DrawBar(
				Constants.FuelBarX,
				Constants.FuelBarY,
				Constants.FuelBarW,
				Constants.FuelBarH,
				hud.Fuel,
				fuelColor
			);

			// Toggle status line along the bottom.
			string status =
				$"interp {Box(hud.Interpolation)} i   subpix {Box(hud.Subpixel)} p   "
				+ $"smooth {Box(hud.Smoothing)} o   map {Box(hud.Minimap)} m   "
				+ $"crt {Box(hud.Crt)} c"
				+ (hud.Gamepad ? "   gamepad" : "");
			_spriteBatch.DrawString(
				_font,
				status,
				new Vector2(6f, Constants.WindowHeight - 22f),
				Palette.White,
				0f,
				Vector2.Zero,
				small,
				SpriteEffects.None,
				0f
			);

			_spriteBatch.End();
		}

		// A HUD meter: translucent backdrop, floored fill, and a light border.
		private void DrawBar(int x, int y, int w, int h, float frac, Color fill)
		{
			_spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), Palette.DarkBlue * 0.85f);
			if (frac > 0f)
				_spriteBatch.Draw(_pixel, new Rectangle(x, y, (int)MathF.Floor(w * frac), h), fill);
			// 1px border.
			Color line = Palette.LightGray * 0.8f;
			_spriteBatch.Draw(_pixel, new Rectangle(x, y, w, 1), line);
			_spriteBatch.Draw(_pixel, new Rectangle(x, y + h - 1, w, 1), line);
			_spriteBatch.Draw(_pixel, new Rectangle(x, y, 1, h), line);
			_spriteBatch.Draw(_pixel, new Rectangle(x + w - 1, y, 1, h), line);
		}

		// Lock-on brackets around the targeted enemy (render.ts drawReticle).
		private void DrawReticle(
			Entity? lockTarget,
			bool charging,
			float dt,
			int flooredCamX,
			int flooredCamY
		)
		{
			if (lockTarget is not Entity target)
				return;
			if (!_world.IsAlive(target) || !_world.Has<Transform>(target))
				return;

			_reticleAnim += dt;
			var pos = _world.Get<Transform>(target).Position;
			int cx = (int)MathF.Floor(pos.X) - flooredCamX;
			int cy = (int)MathF.Floor(pos.Y) - flooredCamY;

			int half = 6 + (MathF.Sin(_reticleAnim * 7f) > 0.4f ? 1 : 0);
			const int arm = 2;
			Color color = charging ? Palette.Orange : Palette.Red;

			void Corner(int px, int py, int dx, int dy)
			{
				_spriteBatch.Draw(
					_pixel,
					new Rectangle(dx < 0 ? px : px - arm + 1, py, arm, 1),
					color
				);
				_spriteBatch.Draw(
					_pixel,
					new Rectangle(px, dy < 0 ? py : py - arm + 1, 1, arm),
					color
				);
			}

			Corner(cx - half, cy - half, -1, -1);
			Corner(cx + half, cy - half, 1, -1);
			Corner(cx - half, cy + half, -1, 1);
			Corner(cx + half, cy + half, 1, 1);
		}

		public void Dispose()
		{
			_worldRT?.Dispose();
			_minimapRT?.Dispose();
			_sceneRT?.Dispose();
			_bloomRT?.Dispose();
			_bloomRT2?.Dispose();
			_scanlines?.Dispose();
			_vignette?.Dispose();
			_multiply?.Dispose();
			_pixel?.Dispose();
			_shipLight?.Dispose();
		}
	}
}
