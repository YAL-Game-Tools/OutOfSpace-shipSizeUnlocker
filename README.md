# Ship size unlocker for Out of Space

**Quick links:** [Pre-built binaries](https://yellowafterlife.itch.io/out-of-space-ship-size-unlocker)

This small mod for [Out of Space](https://store.steampowered.com/app/400080/Out_of_Space/) does a few things:

- Allows non-Large ships to be used with challenge modes (Seeded/Random).  
  This is something that has been bothering me for a little while - modifiers are fun, but a Large ship takes a little longer to complete with 1-2 players, making me reluctant to pick modifiers that don't radically change the game.
- Enables the hidden Giant ship size (20 rooms total!).  
  I suspect that it's not enabled by default due to the camera having to zoom out too far if players scatter across the ship.

The mod is multiplayer-compatible (works even if only the host has it installed) and has seemingly no caveats.

## Installing
Extract the binaries to the game directory, run the executable to patch the game.

You can remove the patcher afterwards (or keep it around in case the game updates).

## Uninstalling
Replace `Out of Space_Data/Managed/Assembly-CSharp.dll` with a backup from `Out of Space_Data/Managed/shipSizeUnlocker backup`.

## Building
Open the included Visual Studio solution and compile in Release configuration.

## Technical
The program patches up a few methods in `PlayerSelectUI` using [Mono.Cecil](https://github.com/jbevain/cecil/).  
You can also do this by hand (using ILSpy with Reflexil or dnSpy) if you know IL bytecode.
