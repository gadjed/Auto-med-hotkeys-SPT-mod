# Auto Med Hotkeys

**SPT 4.0.13 only**

Client mod that automatically binds medical items to quick-use slots **4 / 5 / 6** when they are in your **pockets** or **tactical rig** — both in the **stash** and in raid.

[Latest release](https://github.com/gadjed/Auto-med-hotkeys-SPT-mod/releases/latest) · [License: MIT](LICENSE)

## Hotkeys

| Slot | Items |
|------|--------|
| **4** | Any medkit (AI-2, Car, Salewa, IFAK, AFAK, Grizzly, …) |
| **5** | Esmarch, CAT, CALOK-B, Zagustin |
| **6** | Any bandage (Aseptic, Army, and other light-bleed bandages) |

If you move a bound item into a backpack (or elsewhere that clears the quickbind), then move a matching item back into pockets/rig, the hotkey is restored automatically.

## Install

1. Download `AutoMedHotkeys-*.zip` from [Releases](https://github.com/gadjed/Auto-med-hotkeys-SPT-mod/releases)
2. Extract into your **SPT game root** (folder with `EscapeFromTarkov.exe` / `BepInEx/`)
3. Launch the game

Zip layout:

```text
BepInEx/plugins/AutoMedHotkeys.dll
```

No server mod is required.

## Config (F12)

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Master toggle |
| `OverwriteExisting` | `true` | Replace non-matching items already on slots 4/5/6 |
| `Debug` | `false` | Verbose bind logging |

Config file: `BepInEx/config/gadjed.automedhotkeys.cfg`

## Notes

- Only bindable places count (vanilla: pockets + tactical vest), same as manual quickbinds
- Among several matching items, the one with the most remaining resource is preferred
- Surgical kits (CMS / Surv12) and splints are **not** auto-bound to these slots
- Compatible with Fika observed clients (remote inventories are ignored)

## Build from source

Requires **.NET SDK** and game/BepInEx DLLs under `References/` (see `References/README.md`).

```bash
dotnet build AutoMedHotkeys.csproj -c Release
```

Output: `Build/SPT/BepInEx/plugins/AutoMedHotkeys.dll`

## License

MIT — see [LICENSE](LICENSE).
