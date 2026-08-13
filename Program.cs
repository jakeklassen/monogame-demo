using System;

// DPI-unaware fallback for hosts launched WITHOUT our app.manifest (the manifest
// is the authoritative method and wins where present). Must be set before the game
// is constructed: MonoGame's base Game ctor runs SDL_Init, and this SDL hint is
// only read at video-init time.
if (OperatingSystem.IsWindows())
{
	Environment.SetEnvironmentVariable("SDL_WINDOWS_DPI_AWARENESS", "unaware");
}

using var game = new CherryBomb.Game1();
game.Run();
