# Outpost Gale

**Keep your crew alive on Mars, using real NASA space weather.**

A browser survival game for ages 7–12, made for the NASA Space Apps Challenge 2026
("Build a Junior Astronaut Mission Trainer"). You command a small base at Gale crater, where NASA's Curiosity
rover explores. Keep three meters up (Power, Life support, Crew health) until the next crew lands, and learn that
every choice costs something: an oxygen maker eats power, a trip outside costs radiation, sealing the storm shelter
uses tonight's power. **Every solar storm in the game really happened**, taken from NASA's DONKI database.

- Plays in the browser (Unity WebGL). No download, no account; progress saves in the browser.
- Two modes: **Cadet** (20 sols, meters in words) and **Commander** (30 sols, real units such as mSv).
- **Classroom Code**: everyone who types the same code gets the same storms, events and map.
- Real NASA data in the game, always with its source: DONKI storms and a live "Today's Sun", NASA OSDR health
  studies, Curiosity's radiation and soil measurements, and real Curiosity photos of the places you visit.

## How to play

- You start inside the base, on the **command deck**. Its three screens explain everything: the **Status board**
  (your meters and how they work), the **Directory** (every room and its trade-off) and **NASA News**
  (today's real space weather and science).
- Each sol has 8 daylight hours, and every job costs some of them. When you're done, sleep to end the sol;
  the night report shows what changed and why, including the radiation the crew took.
- Go outside through the airlock to gather regolith, water (baked out of soil, as Curiosity found), rock samples
  and spare parts. Every trip costs time and a radiation dose.
- Use the Radio to hear whether a storm is coming, then decide whether to seal the Storm Shelter.
- Build upgrades: each one helps one meter and costs another. Before the next crew lands, build a landing pad.

**Controls:** click a room or object to walk there and use it. Keys: `A`/`D` walk inside (`WASD` on the map),
`E` use, `I` switch between inside and the map, `1` Rest, `2` Radio, `3` Seal the shelter, `4` Go outside,
`B` Build, `Esc` menu.

## NASA data

| Data | What it does in the game |
|---|---|
| DONKI (NASA CCMC) | Storm nights replay a real year's solar particle events, with their real dates; "Today's Sun" is a live call |
| OSDR | Fact cards from real space-biology studies (bones, muscles, plants, radiation) |
| Curiosity RAD | Normal radiation at Gale: about 0.64 mSv a sol, about 6 chest X-rays |
| Curiosity SAM | Gale's soil is about 2% water, so water is baked out of soil (Gale has no shallow ice) |
| Curiosity Mastcam, Mars Odyssey | Real photos of Bagnold Dunes, Rocknest and Yellowknife Bay; the Gale crater map |

Live data is labelled **LIVE**, bundled data **REAL ARCHIVE**. Details and simplifications: [DATA.md](DATA.md).

## Build it

Requires Unity **6000.6.7f1** with the WebGL module (close the Editor before running these).

```bash
node tools/fetch_data/index.js          # refresh the NASA archive (optional, no API keys)
bash tools/check_compile.sh             # fast compile check, no Unity needed
unity test . --mode EditMode --output Logs/tests.xml   # 56 tests, including a 100-seed balance run
"C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod OutpostGale.EditorTools.BuildScript.BuildWebGL -logFile Logs/build.log
```

The WebGL build lands in `Builds/WebGL` (not committed). Zip that folder's contents and upload it to itch.io as
an HTML game, or serve it locally with `python -m http.server 8765 --directory Builds/WebGL`.

## Project layout

| Path | What |
|---|---|
| `Assets/Scripts/Sim` | All game rules, plain C# with no Unity dependency (Mission, SolResolver, StormPlanner, …) |
| `Assets/Scripts/Game` | The map, the base interior, HUD, popups and screens, all built from code at runtime |
| `Assets/Resources/Data` | `Balance.json` (every number), `Story.json` (dialogue, words, endings), `Facts.json`, `Trips.json` |
| `Assets/Resources/Art`, `Audio`, `Fonts` | Art, sound and fonts (see [ASSETS.md](ASSETS.md)) |
| `Assets/StreamingAssets/data` | Bundled NASA archive (DONKI seasons, OSDR facts, Curiosity RAD) |
| `Assets/Tests/EditMode` | Rule, placement, trip, season, save and balance tests |
| `Assets/ThirdParty` | Licence files for third-party art, sound and fonts |
| `tools/fetch_data` | Node script that builds the NASA archive |
| `tools/art` | Python scripts that draw the original characters |
| `docs/PLAN.md` | The design plan |

## Credits and use of AI

All data, images, sounds, fonts and their licences are listed in [CREDITS.md](CREDITS.md). NASA does not endorse
this game. The game idea and design are the team's own; an AI coding assistant (Claude, by Anthropic) helped write
the code, tests, data scripts and the scripts that draw the characters and help create the necessary references and documents in this repository, which are labelled as AI-assisted in the
game and in CREDITS.md. `CLAUDE.md` holds the instructions given to that assistant. 
