using System;
using System.IO;
using System.Text.Json;

namespace CherryBomb.Lib
{
	// Replaces the web build's localStorage highscore with a small JSON file in the
	// per-user local app-data folder. Reads/writes are best-effort: any IO error
	// degrades gracefully to a 0 highscore rather than crashing the game.
	// Source: scenes/game-over-screen.ts (localStorage "highscore").
	public static class Highscore
	{
		private sealed class HighscoreData
		{
			public int Highscore { get; set; }
		}

		private static string FilePath
		{
			get
			{
				var dir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"CherryBomb"
				);

				return Path.Combine(dir, "highscore.json");
			}
		}

		public static int Load()
		{
			try
			{
				var path = FilePath;

				if (!File.Exists(path))
				{
					return 0;
				}

				var json = File.ReadAllText(path);
				var data = JsonSerializer.Deserialize<HighscoreData>(json);

				return data?.Highscore ?? 0;
			}
			catch
			{
				return 0;
			}
		}

		public static void Save(int highscore)
		{
			try
			{
				var path = FilePath;

				Directory.CreateDirectory(Path.GetDirectoryName(path));

				var json = JsonSerializer.Serialize(new HighscoreData { Highscore = highscore });
				File.WriteAllText(path, json);
			}
			catch
			{
				// Best-effort persistence; ignore IO failures.
			}
		}

		// Loads the current highscore, updates it with score if higher (persisting),
		// and returns the (possibly new) highscore plus whether it was a new record.
		public static (int Highscore, bool IsNew) Update(int score)
		{
			var current = Load();

			if (score > current)
			{
				Save(score);

				return (score, true);
			}

			return (current, false);
		}
	}
}
