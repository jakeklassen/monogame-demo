using System.Collections.Generic;

namespace CherryBomb
{
	public class EnemyConfig
	{
		public int Id { get; set; }
		public int Score { get; set; }
		public int StartingHealth { get; set; }
	}

	public class Enemies
	{
		public EnemyConfig Boss { get; set; }
		public EnemyConfig GreenAlien { get; set; }
		public EnemyConfig RedFlameGuy { get; set; }
		public EnemyConfig SpinningShip { get; set; }
		public EnemyConfig YellowShip { get; set; }
	}

	public class Entities
	{
		public Enemies Enemies { get; set; }
	}

	public class Wave
	{
		public int Id { get; set; }
		public float AttackFrequency { get; set; }
		public float FireFrequency { get; set; }
		public int[][] Enemies { get; set; }
	}

	public class Config
	{
		public bool Debug = false;
		public const int GameWidth = 128;
		public const int GameHeight = 128;

		public Entities Entities = new()
		{
			Enemies = new Enemies
			{
				Boss = new EnemyConfig
				{
					Id = 5,
					Score = 10_000,
					StartingHealth = 130
				},
				GreenAlien = new EnemyConfig
				{
					Id = 1,
					Score = 100,
					StartingHealth = 3
				},
				RedFlameGuy = new EnemyConfig
				{
					Id = 2,
					Score = 200,
					StartingHealth = 2
				},
				SpinningShip = new EnemyConfig
				{
					Id = 3,
					Score = 300,
					StartingHealth = 4
				},
				YellowShip = new EnemyConfig
				{
					Id = 4,
					Score = 500,
					StartingHealth = 20
				}
			}
		};

		public Dictionary<string, string> Fonts = new()
		{
			{"default", "Fonts/DefaultFont"}
		};

		public Dictionary<int, Wave> Waves = new()
		{
			// space invaders
			{
				1,
				new Wave {
					Id = 1,
					AttackFrequency = 60f / 30f,
					FireFrequency = 20f / 30f,
					Enemies = new[]
					{
						new[] {0, 1, 1, 1, 1, 1, 1, 1, 1, 0},
						new[] {0, 1, 1, 1, 1, 1, 1, 1, 1, 0},
						new[] {0, 1, 1, 1, 1, 1, 1, 1, 1, 0},
						new[] {0, 1, 1, 1, 1, 1, 1, 1, 1, 0},
					}
				}
			},

			// red tutorial
			{
				2,
				new Wave {
					Id = 2,
					AttackFrequency = 60f / 30f,
					FireFrequency = 20f / 30f,
					Enemies = new[]
					{
						new[] {1, 1, 2, 2, 1, 1, 2, 2, 1, 1},
						new[] {1, 1, 2, 2, 1, 1, 2, 2, 1, 1},
						new[] {1, 1, 2, 2, 2, 2, 2, 2, 1, 1},
						new[] {1, 1, 2, 2, 2, 2, 2, 2, 1, 1},
					}
				}
			},

			// wall of red
			{
				3,
				new Wave {
					Id = 3,
					AttackFrequency = 50f / 30f,
					FireFrequency = 20f / 30f,
					Enemies = new[]
					{
						new[] {1, 1, 2, 2, 1, 1, 2, 2, 1, 1},
						new[] {1, 1, 2, 2, 2, 2, 2, 2, 1, 1},
						new[] {2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
						new[] {2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
					}
				}
			},

			// spin tutorial
			{
				4,
				new Wave {
					Id = 4,
					AttackFrequency = 50f / 30f,
					FireFrequency = 15f / 30f,
					Enemies = new[]
					{
						new[] {3, 3, 0, 1, 1, 1, 1, 0, 3, 3},
						new[] {3, 3, 0, 1, 1, 1, 1, 0, 3, 3},
						new[] {3, 3, 0, 1, 1, 1, 1, 0, 3, 3},
						new[] {3, 3, 0, 1, 1, 1, 1, 0, 3, 3},
					}
				}
			},

			// chess
			{
				5,
				new Wave {
					Id = 5,
					AttackFrequency = 50f / 30f,
					FireFrequency = 15f / 30f,
					Enemies = new[]
					{
						new[] {3, 1, 3, 1, 2, 2, 1, 3, 1, 3},
						new[] {1, 3, 1, 2, 1, 1, 2, 1, 3, 1},
						new[] {3, 1, 3, 1, 2, 2, 1, 3, 1, 3},
						new[] {1, 3, 1, 2, 1, 1, 2, 1, 3, 1},
					}
				}
			},

			// yellow tutorial
			{
				6,
				new Wave {
					Id = 6,
					AttackFrequency = 40f / 30f,
					FireFrequency = 10f / 30f,
					Enemies = new[]
					{
						new[] {2, 2, 2, 0, 4, 0, 0, 2, 2, 2},
						new[] {2, 2, 0, 0, 0, 0, 0, 0, 2, 2},
						new[] {1, 1, 0, 1, 1, 1, 1, 0, 1, 1},
						new[] {1, 1, 0, 1, 1, 1, 1, 0, 1, 1},
					}
				}
			},

			// double yellow
			{
				7,
				new Wave {
					Id = 7,
					AttackFrequency = 40f / 30f,
					FireFrequency = 10f / 30f,
					Enemies = new[]
					{
						new[] {3, 3, 0, 1, 1, 1, 1, 0, 3, 3},
						new[] {4, 0, 0, 2, 2, 2, 2, 0, 4, 0},
						new[] {0, 0, 0, 2, 1, 1, 2, 0, 0, 0},
						new[] {1, 1, 0, 1, 1, 1, 1, 0, 1, 1},
					}
				}
			},

			// hell
			{
				8,
				new Wave {
					Id = 8,
					AttackFrequency = 30f / 30f,
					FireFrequency = 10f / 30f,
					Enemies = new[]
					{
						new[] {0, 0, 1, 1, 1, 1, 1, 1, 0, 0},
						new[] {3, 3, 1, 1, 1, 1, 1, 1, 3, 3},
						new[] {3, 3, 2, 2, 2, 2, 2, 2, 3, 3},
						new[] {3, 3, 2, 2, 2, 2, 2, 2, 3, 3},
					}
				}
			},

			// boss
			{
				9,
				new Wave {
					Id = 9,
					AttackFrequency = 60f / 30f,
					FireFrequency = 20f / 30f,
					Enemies = new[]
					{
						new[] {0, 0, 0, 0, 5, 0, 0, 0, 0, 0},
						new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
						new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
						new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
					}
				}
			}
		};
	}
}