# Changelog

## 1.0.2

- Fix: stash still had no binds because the client kept loading **1.0.0** (DLL locked while the game was open)
- Apply binds locally (`simulate=false`) and raise UI bind events so the quickbar updates in stash
- Also refresh on inventory screen open and after move operations
- Default Debug logging on for easier diagnosis

## 1.0.1

- Fix hotkeys not applying in the **stash / character screen** when moving meds into pockets or rig
- Stash uses a different inventory controller than raids; the mod now handles both

## 1.0.0

- Initial release for SPT 4.0.13
- Auto-bind medkits (4), Esmarch/CAT/CALOK/Zagustin (5), bandages (6)
