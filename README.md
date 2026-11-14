# Outpost Gale

A browser survival game for ages 7–12, made for NASA Space Apps 2026 ("Build a Junior Astronaut Mission Trainer").
You command a tiny Mars base at Gale crater for 20 or 30 sols, head outside to gather real Mars resources,
build upgrades that always help one meter and hurt another, and triage more problems than you can fix.
**Every solar storm in the game really happened**: storm nights come from NASA's DONKI database.

- Plays in the browser (Unity WebGL). No download, no account; progress saves in the browser.
- Two modes: **Cadet** (7–9, 20 sols, words instead of numbers) and **Commander** (10+, 30 sols, real units).
- **Classroom Code**: everyone who types the same code gets the same storms, events and map.
- **Science Logbook**: collect real fact cards (NASA OSDR studies, Curiosity, MOXIE, DONKI storms).

## How to play
- Keep **Power**, **Life Support** and **Crew Health** up. If Life Support or Health hits zero, the crew goes home early.
- Each sol has 8 daylight hours. Click a room to walk there and use it. Keys: `1` Rest, `2` Radio,
  `3` Seal the Storm Shelter, `4` Go outside, `B` Build, `E` use, `WASD` walk, `Esc` menu.
- Go outside to gather regolith, ice, rock samples and spare parts; every trip costs a radiation dose.
- Build upgrades on the grid: place a site, then work on it. Where you build matters
  (panels need open ground, walls shield the room they touch, rooms next to the shelter make it safer).
- Use the Radio to hear whether a storm is coming, then decide whether to seal the shelter.
- Before the next crew lands, build a landing pad (it unlocks in Act 3).

## Build it
Requires Unity **6000.6.7f1** with the WebGL module.

```bash
node tools/fetch_data/index.js          # refresh the NASA archive (optional)
bash tools/check_compile.sh             # fast compile check, no Unity needed
unity test . --mode EditMode            # 40+ tests incl. a 100-seed balance run
"C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod OutpostGale.EditorTools.BuildScript.BuildWebGL -logFile Logs/build.log
```
The WebGL build lands in `Builds/WebGL`. Zip that folder's contents and upload to itch.io as an HTML game.

## Project layout
| Path | What |
|---|---|
| `Assets/Scripts/Sim` | All game rules, plain C# with no Unity dependency (MissionState, Mission, SolResolver, BaseLayout, PlacementRules, StormPlanner, StoryDirector) |
| `Assets/Scripts/Game` | World view, HUD, panels and screens, built from code at runtime |
| `Assets/Resources/Data` | `Balance.json` (every number), `Story.json` (dialogue, words, endings), `Facts.json` |
| `Assets/StreamingAssets/data` | Bundled NASA archive (DONKI seasons, OSDR facts, Curiosity RAD) |
| `Assets/Tests/EditMode` | Formula, placement, expedition, season, save and balance tests |
| `tools/fetch_data` | Node script that builds the NASA archive |
| `docs/PLAN.md` | The design plan |

See [DATA.md](DATA.md) for every dataset and how it was simplified, and [CREDITS.md](CREDITS.md).
