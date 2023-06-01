using System.Collections.Generic;
using Components;
using Microsoft.Xna.Framework;

namespace CherryBomb
{
	public static class SpriteSheet
	{
		public class HeartData
		{
			public SpriteData Full { get; set; }
			public SpriteData Empty { get; set; }
		}

		public class PlayerData
		{
			public BoxCollider BoxCollider { get; set; }
			public SpriteData Idle { get; set; }
			public SpriteData BankLeft { get; set; }
			public SpriteData BankRight { get; set; }
			public SpriteData Thruster { get; set; }
		}

		public class EnemiesData
		{
			public SpriteData GreenAlien { get; set; }
			public SpriteData RedFlameGuy { get; set; }
			public SpriteData SpinningShip { get; set; }
			public SpriteData YellowShip { get; set; }
			public BossData Boss { get; set; }
		}

		public class AnimationData
		{
			public int SourceX { get; set; }
			public int SourceY { get; set; }
			public int FrameHeight { get; set; }
			public int FrameWidth { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }
		}

		public class SpriteData
		{
			public Dictionary<string, AnimationData> Animations { get; set; }

			public BoxCollider BoxCollider { get; set; }

			public Rectangle Frame { get; set; }
		}

		public class BossData : SpriteData
		{
			public Rectangle HurtFrame { get; set; }
		}

		public static SpriteData Bullet { get; } =
			new SpriteData { BoxCollider = new BoxCollider(6, 8), Frame = new Rectangle(0, 8, 6, 8) };

		public static SpriteData BigBullet { get; } =
			new SpriteData { BoxCollider = new BoxCollider(8, 8), Frame = new Rectangle(8, 8, 8, 8) };

		public static SpriteData Cherry { get; } =
			new SpriteData { BoxCollider = new BoxCollider(8, 8), Frame = new Rectangle(0, 24, 8, 8) };

		public static HeartData Heart { get; } =
			new HeartData
			{
				Full = new SpriteData { Frame = new Rectangle(104, 0, 8, 8) },
				Empty = new SpriteData { Frame = new Rectangle(112, 0, 8, 8) }
			};

		public static PlayerData Player { get; } =
			new PlayerData
			{
				BoxCollider = new BoxCollider(6, 8, new Vector2(1, 0)),
				Idle = new SpriteData { Frame = new Rectangle(16, 0, 8, 8), },
				BankLeft = new SpriteData { Frame = new Rectangle(8, 0, 8, 8) },
				BankRight = new SpriteData { Frame = new Rectangle(24, 0, 8, 8) },
				Thruster = new SpriteData
				{
					Frame = new Rectangle(40, 0, 8, 8),
					Animations = new Dictionary<string, AnimationData>
					{
						["Thrust"] = new AnimationData
						{
							SourceX = 40,
							SourceY = 0,
							FrameHeight = 8,
							FrameWidth = 8,
							Width = 40,
							Height = 8
						},
					}
				}
			};

		public static SpriteData TitleLogo { get; } = new() { Frame = new Rectangle(32, 104, 95, 14) };

		public static EnemiesData Enemies { get; } =
			new EnemiesData
			{
				GreenAlien = new SpriteData
				{
					BoxCollider = new BoxCollider(8, 8),
					Frame = new Rectangle(40, 8, 8, 8),
					Animations = new Dictionary<string, AnimationData>
					{
						["Idle"] = new AnimationData
						{
							SourceX = 40,
							SourceY = 8,
							FrameHeight = 8,
							FrameWidth = 8,
							Width = 32,
							Height = 8
						},
					}
				},
				RedFlameGuy = new SpriteData
				{
					BoxCollider = new BoxCollider(8, 8),
					Frame = new Rectangle(32, 72, 8, 8),
					Animations = new Dictionary<string, AnimationData>
					{
						["Idle"] = new AnimationData
						{
							SourceX = 32,
							SourceY = 72,
							FrameHeight = 8,
							FrameWidth = 8,
							Width = 16,
							Height = 8
						},
					}
				},
				SpinningShip = new SpriteData
				{
					BoxCollider = new BoxCollider(8, 8),
					Frame = new Rectangle(64, 88, 8, 8),
					Animations = new Dictionary<string, AnimationData>
					{
						["Idle"] = new AnimationData
						{
							SourceX = 64,
							SourceY = 88,
							FrameHeight = 8,
							FrameWidth = 8,
							Width = 32,
							Height = 8
						},
					}
				},
				YellowShip = new SpriteData
				{
					BoxCollider = new BoxCollider(14, 12, new Vector2(1, 2)),
					Frame = new Rectangle(0, 104, 16, 16),
					Animations = new Dictionary<string, AnimationData>
					{
						["Idle"] = new AnimationData
						{
							SourceX = 0,
							SourceY = 104,
							FrameHeight = 16,
							FrameWidth = 16,
							Width = 32,
							Height = 16
						},
					}
				},
				Boss = new BossData
				{
					BoxCollider = new BoxCollider(26, 24, new Vector2(3, 0)),
					Frame = new Rectangle(0, 32, 32, 24),
					HurtFrame = new Rectangle(0, 32, 32, 24),
					Animations = new Dictionary<string, AnimationData>
					{
						["Hurt"] = new AnimationData
						{
							SourceX = 0,
							SourceY = 32,
							FrameHeight = 24,
							FrameWidth = 32,
							Width = 64,
							Height = 24
						},
						["Idle"] = new AnimationData
						{
							SourceX = 64,
							SourceY = 32,
							FrameHeight = 24,
							FrameWidth = 32,
							Width = 96,
							Height = 24
						},
					}
				}
			};

		public static SpriteData EnemyBullet { get; } =
			new()
			{
				BoxCollider = new BoxCollider(6, 6),
				Frame = new Rectangle(0, 16, 8, 8),
				Animations = new Dictionary<string, AnimationData>
				{
					["Pulse"] = new AnimationData
					{
						SourceX = 0,
						SourceY = 16,
						FrameHeight = 8,
						FrameWidth = 8,
						Width = 24,
						Height = 8
					},
				}
			};
	}
}
