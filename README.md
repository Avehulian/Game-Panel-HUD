# GamePanelHUD — SPT 4.1.6 (SPTushonka) • 4.1.4

Tarkov all-in-one HUD expansion, fixed & edited for **SPT 4.1.6** (BepInEx 5 / Mono).
This build is the SPT 4.1.6 GamePanelHUD fork with the **newer performance
fixes** and the **story chapter markers** edited back, plus the VisitAPI
story-zone integration.

## Features

* Compass (fire markers, static quest / exfiltration / airdrop markers)
* Weapon & ammo HUD, Health / hydration / energy, Hit markers + armor damage,
  Kill feed + XP, Grenade counter
* **Performance fixes (from the latest SPT branch):**
  * `Changed<T>` snapshots gate every HUD view's TextMeshPro / RectTransform /
    GameObject writes, so a frame where nothing changed no longer dirties the
    shared uGUI canvas.
  * Compass static root uses a nested `Canvas` (isolates rebuilds).
  * Hidden compass markers deactivate their image children instead of only
    setting alpha 0.
  * The vanilla ammo panel toggle is re-applied only when the setting, the
    panel instance or its active state changes.
* **Story chapter markers (needs VisitAPI 1.3.7+):**
  * Story compass markers (VisitAPI story zones and quests owned by the
    "Player Trader" placeholder) bind the icon of the story chapter their
    quest belongs to, instead of the generic trader/placeholder avatar.
  * Hidden story objectives with no name of their own fall back to the
    chapter name.
  * Chapter data comes from VisitAPI's quest flags (`ChapterOf` + chapter icon
    urls) with a quest-chain fallback for deeper hidden objectives.
  * VisitAPI stays **optional** — without it the compass uses the trader avatar
    exactly like the stock 4.0.x build.

## Install

Download the release zip and extract it over your SPT 4.1.6 game folder.

```
BepInEx/plugins/kmyuhkyuk-GamePanelHUD/   (core + modules + bundles + localized)
BepInEx/plugins/kmyuhkyuk-KmyTarkovApi/  (framework)
```

Existing configuration files are reused (plugin GUIDs are unchanged).
VisitAPI 1.3.7+ is required for the story chapter icons (optional otherwise).

## Build from source

The projects reference a local `refs/` folder (SPT 4.1.6 client interop
assemblies plus `KmyTarkovApi` / `KmyTarkovReflection` / `KmyTarkovUtils` built
from the companion framework). `refs/` is git-ignored — refresh it for your
installation.

```
dotnet build GamePanelHUDCore/GamePanelHUDCore.csproj -c Release
dotnet build GamePanelHUDCompass/GamePanelHUDCompass.csproj -c Release
... (Grenade, Health, Hit, Kill, Weapon)
```

## Layout

| Project | Purpose |
|---|---|
| GamePanelHUDCore | Canvas, wiring, settings |
| GamePanelHUDCompass | Compass HUD + story/quest/exfil markers |
| GamePanelHUDGrenade | Grenade counter |
| GamePanelHUDHealth | Health / hydration / energy HUD |
| GamePanelHUDHit | Hit markers + armor damage |
| GamePanelHUDKill | Kill feed + XP |
| GamePanelHUDWeapon | Weapon / ammo HUD |

## Version history

### 4.1.4
* Map-to-map transit points are now drawn on the compass with a Dynamic Maps
  style transit icon. Markers come from `TransitController.pointsById` (the
  same set Dynamic Maps uses), so only active transits for the current raid
  appear. New `Compass Static Transit display` option in Main Settings.

### 4.1.3
* Story tasks no longer draw a trader avatar on top of the chapter icon. Story
  task markers use the chapter icon even when a regular trader gives the task,
  and the duplicate VisitAPI zone marker for the same zone is skipped, leaving
  one story marker per objective. Two new `Quest Display Settings` options
  control this: `Trader Icons` (show trader avatars on task markers) and
  `Story Icons` (use chapter icons for story tasks; when off, story tasks fall
  back to the trader avatar).

### 4.1.2
* Compass markers for a task that was failed and accepted again are back. The
  condition hook received the condition's group (`AvailableForFinish`), not the
  quest status, so every counter change - including the reset a restarted task
  performs - removed its markers. Markers are now only cleared when a terminal
  group (`Success` / `Fail` / `FailRestartable` / `Expired`) changes, and tasks
  in `AvailableForFinish` keep their markers until they are handed in.

### 4.1.1
* Story compass markers no longer show the raw quest/condition id when the
  localisation key had no entry. Marker names now resolve from the real locale
  key to the chapter name and then fall back to `Story area` / `Place area`; a
  story objective without a description falls back to `Story objective`.
  Exfil / airdrop / normal quest marker text is unchanged.

## License

This repository (the fixes + the performance/story changes) is released under
the **MIT License** (see `LICENSE`).

GamePanelHUD originates from `kmyuhkyuk/GamePanelHUD`. The story-zone and
chapter data integration uses VisitAPI (`com.sora.visitapi`). Credits for the
original assets stay with their authors.

## Credits

* Original mod: **kmyuhkyuk – [GamePanelHUD](https://github.com/kmyuhkyuk/GamePanelHUD)**
* This fork originates from `kmyuhkyuk/GamePanelHUD` and is fixed & edited for SPT 4.1.6.
* Story zones / chapter data: **VisitAPI** (`com.sora.visitapi`)
