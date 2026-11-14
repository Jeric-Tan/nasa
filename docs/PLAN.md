# Outpost Gale — Game Plan (Space Apps 2026)

Nov 13, 2026 · @Jeric

**Outpost Gale** is a browser survival game for ages 7–12. You command a tiny Mars base for 30 sols, head out to gather real Mars resources, build upgrades that always help one meter and hurt another, and triage more emergencies than you can handle. Every storm is a real one from NASA's records.

## Brief coverage

Every requirement in the "Build a Junior Astronaut Mission Trainer" brief maps to a feature below. The brief's own wording is in the left column.

| Brief asks for | How Outpost Gale meets it |
| --- | --- |
| Kids 7–12 run a lunar or Martian outpost over a simulated mission | 30-sol Mars mission at Gale crater; you are the commander |
| Decisions on life support, shielding, power, food, comms delay | Life Support meter (air, water, food), Crew Health (shielding), Power; Radio spot with delayed messages |
| No choice is free | Every upgrade is +one meter / −another; every expedition costs radiation and time; more events than you can fix |
| Mechanics over visuals | 3 meters, about 6 upgrades and about 8 events carry the lesson; art is simple and readable |
| 2–3 core resources | Exactly 3 meters. Materials are inventory, not meters |
| Plain language; terms explained in context | BOLT explains each new word the first time (sol, dose, regolith, CME) |
| Browser, no download or account | Unity WebGL on itch.io; no login, progress saved locally |
| Real NASA data, attributed in the tool | Storms from DONKI, dose from RadLab (Curiosity RAD), health facts from OSDR, terrain from Mars Trek; every card names its source |
| Don't present static data as live | LIVE vs REAL ARCHIVE label on every data-driven card |
| Link choices to real space weather | "Today's Sun" pulls current DONKI activity; each storm card shows the real event and date |
| Replayability; failure not punishing | Real storm seasons, shuffled events, crew traits, unlocks; losing ends in a mission report and "try again" |
| Short explanation of datasets used | In-game Science Logbook plus a `DATA.md` in the repo |
| Works for siblings, parents, classrooms | Cadet and Commander modes; Classroom Code for a shared seed |

## Premise and role

You are the **new commander of Outpost Gale**, a small research base on Mars next to the path of NASA's Curiosity rover. The first crew left behind a bare-bones base. Your job: keep your crew alive for 30 sols and build the base up so it's ready for the **next, bigger crew landing on sol 30**.

- **Where:** Gale crater, Mars. The radiation numbers come from Curiosity's real instrument at this exact spot.
- **Who you are:** an astronaut on Mars, not Mission Control. Earth is a voice on the Radio whose advice always arrives late.
- **Crew:** you, two crewmates and BOLT, the base robot.
- **Why the goal works:** building for the next crew gives every upgrade a purpose. It also mirrors how NASA actually plans: each mission prepares the way for the next.
- **What a win teaches:** you can't fix everything, so a good commander chooses what matters most and accepts the cost.

## Story

The story is told in short, skippable lines (3 at most per beat) from BOLT, the crew and Earth. Each act teaches one main trade-off, and the final act forces you to use all of them at once.

**Cast** (all original characters)

| Character | Role | Gameplay hook |
| --- | --- | --- |
| You, the Commander | Named and styled by the player | Makes every final call |
| Mira | Botanist | Trait: greenhouse tasks go faster. Worries about food |
| Tomas | Geologist | Trait: expeditions are quicker. Pushes for more science |
| BOLT | Base robot, the guide | Explains new words, gives hints, never decides for you |
| Dr. Okafor (CAPCOM) | Mission Control on Earth | Speaks only through the Radio; always one sol behind |

Mira and Tomas sometimes disagree ("we need food" vs "we came for science"), which turns a trade-off into a friendly argument kids can pick a side in.

**The arc: 4 acts over 30 sols**

| Act | Sols | What happens | Trade-off it teaches |
| --- | --- | --- | --- |
| 1. Touchdown | 1–5 | Arrive, meet the crew, first expedition, first upgrade, a small leak | Every meter needs care; upgrades cost something |
| 2. Dust Season | 6–15 | Dust storms cut solar power for days. Batteries vs more panels | Power is unreliable; keep a buffer |
| 3. Solar Maximum | 16–25 | Real solar storms hit more often. Earth's warnings arrive late. A crewmate gets worn out | Shielding vs everything else; decide without Earth |
| 4. Arrival | 26–30 | The next crew's landing window lines up with a big storm. Power the landing beacon or the Storm Shelter? | Everything at once; there is no perfect answer |

**Endings** (graded, never a "game over")

| Ending | When | Message |
| --- | --- | --- |
| Thriving Outpost | All meters healthy and the base can hold 6 | "The next crew walks into a real home." |
| Ready, Just Barely | Crew safe, base mostly ready | "It was close, but you made it." |
| Holding On | Crew safe, base not ready | "Everyone's safe. The next crew has work to do." |
| Early Ride Home | A meter hit zero | "The crew is evacuated. Here's what happened, and what to try next time." |

Each ending closes with a comic-style mission report of your 3 biggest decisions and one real NASA parallel (for example, how the Opportunity rover's mission ended in the 2018 dust storm).

## Core systems

The player tracks **exactly 3 meters**. Everything else (food, shielding, materials, comms) feeds into one of them.

| Meter | Covers | Drains from | Refilled by |
| --- | --- | --- | --- |
| Power | Solar panels and battery | Every powered room each sol; dust on panels; storms | Clean panels, more panels, bigger battery |
| Life Support | Air, water, food | Crew breathing, drinking and eating every sol; leaks | Oxygen maker, water recycler, greenhouse harvests |
| Crew Health | Radiation dose, fitness, rest | Radiation (more outside and during storms), overwork, low food | Shielding, rest, exercise, good meals |

**Trade-off rules** (checked against every upgrade and event during design)

1. Nothing is free. Every action spends time, a material, power or radiation dose.
2. No upgrade is strictly better. Each one is +one meter and −another, or costs something you need elsewhere.
3. Effects are delayed. Food planted today arrives in 3–5 sols; skipping air today costs more tomorrow.
4. Ignored problems drain a meter faster but never cause an instant loss, so kids can recover and learn.
5. Every number shown is explained in plain words ("Air dropped because the oxygen maker had no power").

## The sol loop

Each sol has **8 daylight hours**, shown as a sun moving across the top of the screen. Every action costs hours, so time is the budget kids feel most. A sol takes about 2–4 minutes to play, and a full mission about 60–90 minutes (Cadet's 20 sols: about 45).

1. **Morning briefing (free).** BOLT reports dust level, Today's Sun and yesterday's message from Earth. Up to 3 active events are shown, each with a deadline.
2. **Plan the day (free).** Set the Power split: which rooms get the day's power cells.
3. **Day (8 hours).** Choose what to spend hours on:
   - **Expedition** outside (2–4 h): gather materials, earn Science, take radiation.
   - **Base tasks** (1 h each): fix events, tend the greenhouse, balance the air, exercise. Each is a 5–15 second minigame, with Auto-do after the first time.
   - **Build** (1–3 h): spend materials on an upgrade.
   - **Rest** (2 h): Health recovers.
4. **Night (automatic).** Meters drain, crops grow, and an overnight event may hit (often a storm).
5. **Debrief (about 10 seconds).** Arrows show what went up and down and why, and sometimes a real NASA fact card unlocks.

**Controls** (decide after playtesting; all three are cheap in Unity)

- **Click or tap to move and use** (default). Works for young kids, touchpads and tablets.
- **WASD + E** as an option for keyboard players.
- **Hotkeys 1–4** for the day actions, so planners can skip walking entirely.

The base is a 2.5D cutaway with rooms and corridors (layout inspired by real Mars habitat concepts, not copied from any game). Walking distance costs a little time, so where you build things matters.

## Expeditions and materials

Going outside is how you get anything new, and it's never free: it costs hours and radiation dose, and the farther you go, the better the haul. This mirrors a real NASA idea called **ISRU** (in-situ resource utilization): living off what's on Mars instead of shipping everything from Earth.

**4 materials** (inventory, not meters)

| Material | Real basis | Found | Used for |
| --- | --- | --- | --- |
| Regolith (Mars soil) | Piling soil around a habitat is a studied way to block radiation | Everywhere, near base | Shield walls, landing pad |
| Water ice | Mars has ice buried under the surface | Farther out, needs digging | Water, oxygen, greenhouse |
| Rock samples | What Curiosity studies | Marked science spots | Science score, unlocking blueprints |
| Spare parts | Cargo from supply drops | Rare drop sites, events | Machines: panels, battery, recycler |

**The expedition map** is a small Gale crater area built from Mars Trek imagery, with 4–6 spots at different distances. On screen you see:

- hours each spot costs (there and back),
- the expected radiation (higher on storm-warning days),
- what you'll likely find.

**Push-your-luck moments.** While outside, a dust devil or a storm warning can appear. Head home now with what you have, or grab one more sample and take the extra dose? That's a real astronaut decision in kid size.

## Upgrades

Upgrades are the long-term choices. Each one shows a simple **+ / − card** before you build it, and each has a real-world reason for its downside.

| Upgrade | Costs | + | − | Why (real basis) |
| --- | --- | --- | --- | --- |
| Extra solar panel | 2 parts | +Power | More panels to clean after dust | Dust on panels cut power for NASA's solar rovers |
| Big battery | 3 parts | Survive dark dust days | Uses parts you need for the recycler | Storage is how solar bases get through storms |
| Regolith shield wall | 6 regolith | +Health (less radiation at night) | Many trips outside to build it, so extra dose now | Soil is a cheap, heavy radiation shield |
| Oxygen maker (MOXIE-style) | 2 parts, 1 ice | +Life Support (air) | Heavy Power drain every sol | Perseverance's MOXIE made oxygen from Mars air |
| Water recycler | 3 parts | +Life Support (water) | Breaks down; needs repair tasks | The ISS recycles most of its water |
| Bigger greenhouse | 2 ice, 2 regolith | +Life Support (food), +Health (mood) | −Power and −water every sol | Plants need light, water and warmth |
| Gym corner | 1 part | +Health (bones and muscle) | Costs an hour every sol to use | Low gravity weakens bones (OSDR studies) |
| Comms dish | 2 parts | Earth warnings arrive sooner (still delayed) | −Power | Bigger antennas mean better signals, but light speed doesn't change |
| Landing pad | 8 regolith | Needed for the best ending | Big build time just before the finale | Pads stop rockets blasting dust and rocks |

Upgrades unlock as you go: Act 1 shows 3, and the rest appear as blueprints from rock samples and story beats, so the menu never overwhelms a 7-year-old.

## Events and triage

Events are the short-term choices. Up to 3 can be active at once, each with a deadline in sols, and you usually only have hours for 1–2. Whatever you skip drains its meter until it's handled or expires.

| Event | Trigger | Fix (hours) | If ignored | Real data |
| --- | --- | --- | --- | --- |
| Solar storm (SEP) | Real SEP/CME record from DONKI | Power the Storm Shelter and sleep there (1 h + 3 cells) | Big Health hit tonight | DONKI SEP + CME; dose scaled from RadLab |
| Flare warning | Real flare from DONKI, received via Radio a sol late | Read it (1 h) | You miss the early warning for the next storm | DONKI FLR |
| Dust storm | Seasonal, stronger in Act 2 | Clean panels after (2 h outside) | Power stays low for days | Real Mars dust history (e.g. 2018 global storm) |
| Air leak | Random, more after storms | Seal minigame (1 h) | Life Support drains faster | Habitat maintenance |
| Recycler jam | Random, more with the recycler upgrade | Repair minigame (1 h) | Water drops | ISS water recycling |
| Crop blight | Random, more with a big greenhouse | Tend minigame (1 h) | Food harvest lost | CSA food-in-space toolkit |
| Crew fatigue | Too many work hours in a row | Rest (2 h) | Health drains, tasks go slower | OSDR spaceflight health studies |
| Supply drop spotted | Random | Expedition (3 h) | Parts lost | Cargo resupply planning |

**Science fixes from the last review** (these keep the game accurate)

- **Flares vs particles.** A flare is mainly a burst of light and X-rays. The particles that dose astronauts come from **SEP events**, often with CMEs. So flares are warnings and SEP events are the radiation hits.
- **Earth vs Mars.** DONKI is mostly measured from Earth's point of view. Use events DONKI's analyses flag as reaching Mars where possible; otherwise label the card "based on a real solar storm", not "this hit Mars".
- **Comms delay.** The real one-way delay is about 4–24 minutes. "One sol late" is a game simplification, and BOLT says so the first time.
- **Dose numbers.** Health damage is scaled from RadLab's Curiosity RAD readings, never invented.

## Replayability and progression

No two missions play the same, and losing still unlocks something, so kids want another run.

**What changes each run**

| Variable | How it varies | Why it matters |
| --- | --- | --- |
| Solar weather season | Each run replays a real stretch of DONKI history (a calm year vs a stormy one like 2017 or 2024) | Same base, very different radiation pressure; the card shows which real season it was |
| Dust season | Start sol and strength shuffled | Power plans that worked last time may fail |
| Event deck | Shuffled, weighted by your upgrades (bigger greenhouse → more blight) | Your choices shape your problems |
| Crew traits | 2 of 6 traits per run (green thumb, fast walker, light sleeper…) | Different strategies shine |
| Expedition map | Material spots rearranged | Exploring stays fresh |

**What carries over**

- **Science Logbook:** every real fact card you've found, with its source. Collect them all.
- **Blueprints:** upgrades you've unlocked once start available next time.
- **Badges:** for example "Survived a real X-class storm season", "Thriving Outpost on Commander", "No one went outside for 5 sols".
- **Second site:** finishing a mission unlocks **Jezero crater** (Perseverance's home), with more ice and different terrain.

**Shared play**

- **Classroom Code:** everyone plays the same seed and compares strategies and endings.
- **Sol of the Week:** a fixed seed tied to that week's real DONKI activity (stretch goal).

## Kid experience and accessibility

**Two modes**

|  | Cadet (7–9) | Commander (10–12+, parents, classrooms) |
| --- | --- | --- |
| Length | 20 sols | 30 sols |
| Numbers | Icons and words ("Radiation: HIGH") | Real units appear (mSv, watt-hours) |
| Events at once | Up to 2 | Up to 3 |
| Upgrades | Fewer, unlocked slowly | Full list |
| Real storms | Milder seasons | Includes the big historical storms |

**First 5 minutes (tutorial built into Act 1)**

1. BOLT walks you to each room once; one meter is introduced at a time.
2. First expedition is short and safe; you come back with regolith.
3. First upgrade shows the + / − card and asks you to say which you'd give up.
4. First small event (a leak) teaches "fix it now or let it drain".

**Rules for every screen**

- Plain words. A new term gets a tap-to-explain bubble the first time (sol, radiation dose, regolith, SEP).
- Every meter change says why.
- Colour is never the only signal; icons and shapes are used too, and text keeps 4.5:1 contrast.
- Big buttons (44 px or more), works with mouse, touch and keyboard.
- No timers on reading; story lines wait for a tap.
- Failure is a mission report with one tip, not a red GAME OVER screen.
- Saved in the browser automatically; no account.

## Real NASA data

The game ships with a bundled archive (always works offline) plus one optional live call. Every card that uses data shows **LIVE** or **REAL ARCHIVE** and names its source.

| Source | Status (checked 2026-10-28) | Used for |
| --- | --- | --- |
| [DONKI](https://ccmc.gsfc.nasa.gov/tools/DONKI/) | Works at the new URL `ccmc.gsfc.nasa.gov/DONKI/WS/get/<FLR, CME, SEP, GST>?startDate=…&endDate=…`, no key. Moved on 2026-09-30; the old api.nasa.gov route failed | Storm seasons, flare warnings, Today's Sun |
| [RadLab](https://visualization.osdr.nasa.gov/radlab/gui/data-api/) | Reachable; Curiosity RAD (`MSL-RAD-Surface`) rows exist, but dose columns didn't return in my test. Fix the query | Baseline dose per sol, storm dose scaling |
| [OSDR](https://osdr.nasa.gov/osdr/data/search?term=radiation) | Search API works | Health fact cards (e.g. bone loss in microgravity, OSD-489) |
| [Mars Trek WMTS](https://trek.nasa.gov/tiles/apidoc/trekAPI.html?body=mars) | Docs reachable | Gale and Jezero expedition maps (pre-baked images) |
| [CSA HALO activity](https://www.asc-csa.gc.ca/eng/youth-educators/activities/multi-functionality-in-space.asp) and [food toolkit](https://www.asc-csa.gc.ca/eng/youth-educators/toolkits/food-production/) | Reachable | Kid-scale habitat size, needs vs wants, food tasks |

**How data reaches players**

- **Storm card:** "This storm really happened on \[date\]" + DONKI event ID.
- **Dose meter:** filled from RadLab readings, explained as "about as much as \[N\] chest X-rays" in Commander mode, once the numbers are checked.
- **Fact cards:** one OSDR finding each, with study ID.
- **Today's Sun banner:** live DONKI call at startup; falls back to the archive and changes its label if it fails.
- **Science Logbook and DATA.md:** every dataset, why it was used and how it was simplified.

## Art, audio and feel

**Look: bold cartoon 2.5D.** Flat sprites on depth layers (parallax), thick outlines, a warm Mars palette, and rooms that glow when powered and go dark when they aren't. That way kids see the power trade-off without reading anything.

| Need | Source | License |
| --- | --- | --- |
| Base modules, astronauts, props | [Kenney](https://kenney.nl/assets) Space Kit and 2D packs | CC0 |
| UI icons | Kenney icons or [game-icons.net](https://game-icons.net) | CC0 / CC BY 3.0 |
| Mars backgrounds and map | Mars Trek tiles, Curiosity and Perseverance photos | NASA media (no NASA logo, no implied endorsement) |
| Mars ambience | Perseverance's real microphone recordings of Mars wind | NASA media |
| Sound effects | Kenney audio, [freesound.org](https://freesound.org) (CC0 only) | CC0 |
| Music | Kenney loops or [Incompetech](https://incompetech.com) | CC0 / CC BY |
| Font | Nunito + Baloo 2 (Google Fonts) | OFL |

Track everything in `CREDITS.md` and the in-game credits.

**Feel touches (cheap, high impact)**

- Title screen plays real Mars wind with "This is what Mars really sounds like".
- Storms: radiation dots fall through the screen; shielded rooms block them.
- Dust: a haze layer dims the sky and the panels visibly brown over.
- Greenhouse plants grow through 4 visible stages.
- Earth's message crawls in on a "signal travelling" bar.
- Crew faces show tired, hungry or happy.
- Small screen shake and a siren when an event lands; a satisfying "clunk" when a build completes.

## Tech architecture

**Engine:** Unity 6000.6.7f1 (WebGL module installed), URP 2D renderer for lights and parallax, hosted on itch.io. GitHub repo for the code.

**Code layout** (simulation separate from visuals, so it can be tested and balanced by seed)

| Folder / class | Job |
| --- | --- |
| `Sim/MissionState` | All numbers: 3 meters, inventory, upgrades, active events, sol, hours |
| `Sim/SolResolver` | Runs the night: drains, growth, event outcomes |
| `Sim/EventDeck` | Draws events from a seeded `System.Random`, weighted by upgrades and act |
| `Sim/UpgradeCatalog` | ScriptableObjects: cost, + / − effects, real-basis text |
| `Data/DataLoader` | Reads bundled JSON from StreamingAssets via `UnityWebRequest` (required on WebGL) |
| `Data/LiveSun` | Optional DONKI call; on failure keeps the archive and sets the label |
| `Story/StoryDirector` | Act beats and dialogue lines from a JSON script |
| `UI/*` | HUD, cards, minigames, Logbook; reads `MissionState`, never owns rules |
| `Tests/` | Unity Test Framework (EditMode): balance runs over 100 seeds |

**Data pipeline** (`/tools/fetch_data`, Node or Python, run before each build)

1. DONKI SEP, CME and FLR by year → curate 6–10 "seasons" (calm and stormy).
2. RadLab Curiosity RAD → per-sol baseline and storm spike values.
3. Hand-picked OSDR findings with study IDs.
4. Write `Assets/StreamingAssets/data/*.json`, each record with `source` and `retrieved_on`.

**Build commands Claude Code can run** (Editor closed): `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode` for tests, and a `BuildScript.BuildWebGL` editor method via `-executeMethod` for builds.

**WebGL checks:** keep the build small (compressed textures, no unused packages), test the DONKI call for CORS early, and enable Decompression Fallback if hosting outside itch.io.

## Solo roadmap

The game stays **playable at the end of every phase**, so if time runs out you still submit something complete. Estimates are working days for one person; set real dates once the event dates are confirmed.

| Phase | Days | Done when |
| --- | --- | --- |
| 1. Data spike | 1 | DONKI and RadLab queries work; first JSON archive committed |
| 2. Sim core (no art) | 3 | 3 meters, hours, night resolver, 4 events, 3 upgrades; tests over 100 seeds |
| 3. Playable base | 3 | Grey-box base, click-to-move, HUD, build menu, debrief; full 20-sol Cadet run possible |
| 4. Expeditions | 2 | Map from Mars Trek, 4 materials, push-your-luck return |
| 5. Story + endings | 2 | Act beats, cast lines, 4 endings, mission report |
| 6. Real-data layer | 2 | Storm cards with real dates/IDs, dose meter, fact cards, Logbook, LIVE/ARCHIVE labels |
| 7. Art + audio pass | 3 | Kenney assets, glow and parallax, Mars wind, feel touches |
| 8. Replay + modes | 2 | Seasons, crew traits, blueprints, Commander mode, Classroom Code |
| 9. Playtest + submit | 2 | 2–3 kid or family playtests, balance by seed, video, README and DATA.md |

Total: about 20 working days.

**MVP cut (if time is short, ship this):** Cadet mode only, 20 sols, 3 meters, 3 materials (drop spare parts), 5 upgrades, 5 events, Acts 1–2 + finale, 2 endings (Thriving / Early Ride Home), storm cards with real DONKI dates, Logbook with 5 cards.

**Minigames:** start every base task as "hold to fix" (a progress ring). Upgrade the 3 most-played ones (Storm Shelter, panel cleaning, air valves) to real minigames only after phase 7.

## Submission, risks and open questions

**Submission package**

- [ ] Playable link on itch.io (no login)
- [ ] Public GitHub repo with README: pitch, how to play, how to build
- [ ] `DATA.md`: each NASA dataset, what it drives in the game, and how it was simplified
- [ ] `CREDITS.md`: all assets and licenses
- [ ] 1–2 minute demo video: a storm card with its real date, a + / − upgrade choice, a triage moment, an ending

**Risks**

| Risk | Fallback |
| --- | --- |
| Scope too big for one person | Ship the MVP cut; story beats and minigames are add-ons |
| WebGL loads slowly on school laptops | Small textures, no 3D, test on a low-end machine in phase 9 |
| DONKI blocked by CORS in the browser | Archive-only, labelled REAL ARCHIVE (still meets the brief) |
| RadLab dose columns don't come back | Use published Curiosity RAD values with citation, labelled as such |
| Too hard for 7-year-olds | Cadet mode tuning by seed, playtests with real kids |
| Gathering and building become the whole game | Keep expeditions short; every upgrade card states its real trade-off |

**Open questions**

- [ ] Event and submission dates (the official challenge page was taken down after our copy of the brief; re-check it when it's back)
- [ ] Final challenge text and resource list, diffed against the version we planned from
- [ ] Controls default after the first playtest (click-to-move vs WASD)
- [ ] Game name: keep "Outpost Gale" or pick another

## Claude Code handoff

This doc is the source of truth for the build. Export it as Markdown to `docs/PLAN.md` in the repo; when the design changes, update the doc and re-export (or make `PLAN.md` the main copy from then on). The playable [browser demo](https://claude.ai/artifact/JL7HpPXnvjRe2xGRZUirtT) is the reference for the simulation rules.

**Put this in `CLAUDE.md` at the repo root:**

```markdown
# Outpost Gale
Unity 6000.6.7f1, URP 2D, WebGL target. Design and plan: docs/PLAN.md (read the
"Simulation spec", "Base building spec" and "Implementation plan" sections first).

## Rules
- Game rules live in Assets/Scripts/Sim as plain C# (no MonoBehaviour, no UnityEngine
  calls except Mathf). UI and world scripts only read MissionState and call Sim methods.
- Every number comes from Assets/Data/Balance.json, never hard-coded.
- Every Sim change needs an EditMode test in Assets/Tests/EditMode.
- Randomness only through the seeded MissionRng (System.Random), never UnityEngine.Random.
- Do not edit .unity scene or .prefab YAML unless asked; build UI and spawn objects
  from code or tell me what to set up in the Editor.
- Real NASA data is read from Assets/StreamingAssets/data/*.json via UnityWebRequest.
  Every record keeps "source" and "retrieved_on". Never invent data values.
- Third-party assets go in Assets/ThirdParty/<pack>/ and get a line in CREDITS.md.

## Commands (close the Editor first)
- Tests: "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode
  -projectPath . -runTests -testPlatform EditMode -testResults Logs/tests.xml -quit
- WebGL build: same exe with -executeMethod BuildScript.BuildWebGL -quit
- Data refresh: node tools/fetch_data/index.js
```

**How to work with Claude Code**

- Give it one milestone from the implementation plan at a time ("Do M2 from docs/PLAN.md").
- Run Claude Code on Windows (not WSL) so it can call Unity.exe.
- You do Editor-only steps (scene setup, Player Settings, importing packs); each milestone lists them under "You".

## Simulation spec

These are the v0 numbers from the playable demo (10 sols). They go in `Balance.json` and get tuned in playtests; for 20- and 30-sol modes, keep the per-sol values and stretch the storm seasons and act boundaries. Meters run 0–100 and are clamped.

**Start of mission**

| Value | Start |
| --- | --- |
| Power / Life Support / Crew Health | 70 / 70 / 80 |
| Daylight hours per sol | 8 |
| Inventory | 0 regolith, 0 ice, 3 parts, 0 samples |
| Storm season | One of `[4,8]`, `[3,6,9]`, `[5,9]`, `[2,7,10]` (storm on those sols' nights) |

**Night resolution (in this order)**

1. **Power:** production = (24 + 8 × panels) × (0.5 if Dusty panels active); use = 14 + 8 (oxygen maker) + 5 (greenhouse) + 12 (shelter sealed). Power += production − use. If the result is 0 or below, set `systemsDown` for this night.
2. **Life Support:** −12 crew use; +10 oxygen maker (unless systemsDown); +7 greenhouse if built 2+ sols ago, no Sick crops and not systemsDown; −6 if Air leak active; −8 if systemsDown.
3. **Crew Health:** −2 baseline radiation (−1 with shield wall). Storm night: −22 unsealed or −6 sealed, halved (rounded) with shield wall. −5 if Life Support is below 25.
4. Each line of the night report states the change and its reasons (the debrief text).
5. Next sol: hours reset to 8, Radio and shelter reset, roll one morning event.

**Day actions**

| Action | Hours | Effect | Radiation |
| --- | --- | --- | --- |
| Crater rim trip | 2 | +2 regolith | −3 health (−5 on a storm sol) |
| Ice field trip | 3 | +2 ice, 35% chance +1 part | −5 (−7 on a storm sol) |
| Science trip | 3 | +1 sample | −5 (−7 on a storm sol) |
| Rest | 2 | +10 health | none |
| Radio Earth | 1 | Reveals whether a storm hits tonight (message "sent yesterday") | none |
| Seal Storm Shelter | 1 | Tonight's storm damage drops; costs 12 power tonight even if no storm comes | none |
| Fix an event | 1 | Removes it (Dusty panels also −2 health: it's outside) | see left |
| Build an upgrade | 2 (grid version: see building spec) | Spends materials, adds the module | none |

**Upgrades**

| Upgrade | Cost | Effect | Downside |
| --- | --- | --- | --- |
| Extra solar panel | 2 parts | +8 power production | Adds a second Dusty panels entry to the event pool |
| Regolith shield wall | 6 regolith | Baseline and storm radiation halved | Needs 3 crater trips (−9 health) to gather |
| Greenhouse | 2 ice, 2 regolith | +7 life support per sol from 2 sols after building | −5 power per sol; enables Sick crops |
| Oxygen maker | 2 parts, 1 ice | +10 life support per sol | −8 power per sol |

**Events** (one roll each morning from sol 2: 45% chance, 70% on sols 4–7 for dust season; max 3 active, no duplicates)

| Event | Pool | While active | Fix |
| --- | --- | --- | --- |
| Air leak | Always | −6 life support per night | 1 h |
| Dusty panels | Always (twice if extra panel built) | Power production ×0.5 | 1 h, −2 health |
| Sick crops | Only with greenhouse | Greenhouse makes nothing | 1 h |

**Endings**

- **Early Ride Home:** Life Support or Crew Health reaches 0 after any night. Power at 0 does not end the game; it causes systemsDown.
- **Thriving Outpost:** after the last sol, all three meters 40+ and 2+ upgrades built.
- **Holding On:** after the last sol, any other result.
- Full game adds **Ready, Just Barely** (crew safe, landing pad built, some meters under 40).

## Base building spec

Players really build: pick a module, place it on a grid around the base, then spend crew hours to construct it. **Where** you place it matters, through a few rules based on real science. If time gets tight, fall back to fixed build pads (same rules, fewer positions).

**Grid**

- 16 × 10 tiles, 1 Unity unit per tile, drawn in 2.5D (orthographic camera tilted about 35°, sprites billboarded or drawn in a 3/4 view).
- Starting base: Hab core (living quarters, 3×2) in the centre with the Airlock (1×1) attached.
- Modules must connect to the base by a corridor tile (1×1, free, auto-placed) except solar panels, which sit outside on open ground.
- Tiles marked as rocks or a slope can't be built on (from the Mars Trek height image, simplified).

**Modules**

| Module | Footprint | Placement rule | Placement bonus / penalty |
| --- | --- | --- | --- |
| Hab core | 3×2 | Fixed at start | None |
| Storm Shelter | 2×2 | Inside the base footprint | −1 storm damage for each module touching it (max −3): neighbours add shielding |
| Extra solar panel | 2×1 | Open ground, 3+ tiles from the base | 6+ tiles away: Dusty panels chance halved for that panel, but cleaning costs +1 h |
| Oxygen maker | 2×1 | Connected to the base | Next to the greenhouse: +2 life support |
| Greenhouse | 2×2 | Connected to the base | Next to the oxygen maker: +2 life support (same bonus, counted once) |
| Regolith shield wall | 1×1 per segment | Touching a module | Shields only the modules it touches; full effect when the Hab core and Shelter are covered |
| Landing pad (full game) | 3×3 | Open ground, 5+ tiles from any module | Required for the best ending |

**Walking cost.** Walking time is 0.25 h per 8 tiles (rounded), so a sprawling base costs hours every sol. The sim gets distances from a precomputed path length per station, recalculated after each build.

**Build flow**

1. Open the build menu (B key or the hammer button) and pick a module. Its + / − card and cost show.
2. A ghost outline follows the cursor: green where it's allowed, red where it isn't, with the rule shown as one line of text.
3. Place it. Materials are reserved, and a construction site appears.
4. Walk to the site and work: each hour spent adds progress (Oxygen maker 2 h, Greenhouse 3 h, Panel 2 h, Shelter 3 h, Wall 1 h per segment).
5. Finished modules switch on at the start of the next sol, with a build sound and the room lighting up.
6. Cancel before finishing to get all materials back; no demolishing after (keeps the save simple).

**Sim interface** (so placement stays testable): `BaseLayout` in `Sim/` holds a grid of tile states and placed modules; `PlacementRules.CanPlace(module, x, y)` returns ok or the reason; `BaseLayout.Bonuses()` returns adjacency effects that `SolResolver` adds in.

## Scenes, UI and controls

**Scenes** (Unity scenes; overlays are UI panels, not scenes)

| Scene | Contents |
| --- | --- |
| `Title` | Parallax Gale crater, Mars wind audio (starts on first click), Today's Sun banner with LIVE / REAL ARCHIVE tag, Play / Continue / Logbook / Credits |
| `Setup` | Cadet or Commander, commander name and suit colour, Classroom Code field, BOLT intro |
| `Outpost` | The base grid, player and crew, HUD, build mode, all day and night overlays (about 90% of play) |
| `Expedition` | Top-down Gale map from Mars Trek imagery with 4–6 spots; return to `Outpost` with materials |
| `Ending` | Comic-style mission report (3 panels), ending title, badges earned, Play again |

**Overlays inside `Outpost`:** Morning briefing, Build menu with + / − cards, Minigame panel, Event card, Night report, Fact card, Science Logbook, Pause/settings.

**HUD layout (1280×720 reference, scales with Canvas Scaler)**

- Top left: the 3 meters with numbers (Cadet: words such as LOW / OK / GOOD).
- Top centre: Sol counter and 8 hour pips (sun icon moves along).
- Top right: inventory chips (regolith, ice, parts, samples) and Science stars.
- Bottom left: BOLT line with tap-to-explain words.
- Bottom right: Build button, Radio message icon with unread badge.
- Interaction prompt floats above the station the player is near ("E / tap: Seal shelter · 1 h").

**Camera:** orthographic, fixed angle, follows the player with damping, clamps to the grid; pull back at night for the overview; small shake on storm hits (off when reduced motion is set).

**Input** (new Input System, one action map, rebinding not needed)

| Action | Mouse / touch | Keyboard |
| --- | --- | --- |
| Move | Click or tap a tile or station (NavMesh-free A\* on the grid) | WASD / arrows |
| Use station | Click the station again, or tap the prompt | E / Space |
| Build mode | Hammer button | B |
| Rotate ghost | Right-click / rotate button | R |
| Day actions shortcut | Action bar buttons | 1–4 |
| Pause | Menu button | Esc |

**Look tokens** (match the mockup)

| Token | Value |
| --- | --- |
| Ink (outlines, dark panels) | #2A1A2E |
| Mars ground | #E9773E, deep #B9502A |
| Panel paper | #FFF6EA |
| Power / Life / Health | #F2B705 / #2F95D6 / #3FAE5A |
| Warning | #E5483B |
| Fonts | Baloo 2 (headings), Nunito (body) |
| Outlines | 4 px at 1080p, rounded corners |

## Implementation plan

Hand Claude Code one milestone at a time. Each one ends with a playable, committed build. Days are rough solo estimates and replace the phase table in "Solo roadmap".

**Folder layout**

```
Assets/
  Scripts/Sim/        MissionState, SolResolver, EventDeck, Upgrades, BaseLayout, PlacementRules, MissionRng
  Scripts/Data/       DataLoader, LiveSun, Records (DonkiEvent, DoseSample, FactCard)
  Scripts/World/      GridView, PlayerController, StationInteractable, BuildGhost, DayNight
  Scripts/UI/         Hud, BriefingPanel, BuildMenu, NightReport, EventCard, Logbook, Minigames/
  Scripts/Story/      StoryDirector, Dialogue
  Scripts/Editor/     BuildScript (WebGL)
  Data/               Balance.json, Story.json, Facts.json
  StreamingAssets/data/  donki_*.json, radlab_*.json, osdr_facts.json
  ThirdParty/<pack>/  Kenney packs, fonts, audio
  Tests/EditMode/
tools/fetch_data/     Node script for NASA data
docs/PLAN.md, DATA.md, CREDITS.md, CLAUDE.md
```

| # | Milestone | Claude Code does | You do | Done when |
| --- | --- | --- | --- | --- |
| M0 | Project setup (½ d) | Folder layout, asmdefs (Sim has no UnityEngine ref), `BuildScript.BuildWebGL`, `.gitignore`, `CLAUDE.md`, empty test suite | Create the project from the URP 2D template in Unity Hub; install Input System and Newtonsoft JSON packages; set WebGL as the build target; create the GitHub repo | Batchmode tests run green; an empty WebGL build opens in a browser |
| M1 | NASA data (1 d) | `tools/fetch_data`: DONKI FLR/CME/SEP by year from the new ccmc.gsfc.nasa.gov URL, RadLab Curiosity RAD query (fix dose columns), curated OSDR facts; writes JSON with `source` + `retrieved_on`; `DataLoader` + tests | Check the curated storm seasons and fact wording read well for kids | JSON files committed; DataLoader test loads them |
| M2 | Sim core (2 d) | Port the Simulation spec into `Sim/` with `Balance.json`; night report reasons; endings; tests for every formula plus a 100-seed balance run that prints win rates | Play the browser demo twice to sanity-check numbers | All tests green; balance run shows Cadet win rate roughly 50–80% for a sensible scripted strategy |
| M3 | Grid and building (2–3 d) | `BaseLayout`, `PlacementRules`, adjacency bonuses (tests first), `GridView`, `BuildGhost` with green/red preview and reason text, construction progress | Build the `Outpost` scene: camera, a grid object, sorting layers; drop in placeholder sprites | You can place, construct and cancel modules; rules and bonuses match the spec |
| M4 | Player and stations (2 d) | Grid A\* pathfinding, click/tap-to-move, WASD option, `StationInteractable` prompts, hour cost incl. walking, crew NPC idle loops | Place station anchor objects in the scene; tune walk speed by feel | Every day action from the spec can be done in the world and spends the right hours |
| M5 | Day/night loop and UI (2 d) | HUD, briefing panel, night report overlay, fact card, event list, end-of-sol flow, local save (PlayerPrefs JSON) | Lay out the Canvas in the Editor if Claude's code-built UI looks off | A full 20-sol Cadet mission is playable with placeholder art |
| M6 | Events and minigames (2 d) | Event deck with seasons, hold-to-fix default, 3 minigames (seal shelter, clean panels, balance air valves), Auto-do after first play | Playtest minigame feel; say which feel too long | Events spawn by the spec; ignored ones drain meters; minigames take 5–15 s |
| M7 | Expeditions (1–2 d) | `Expedition` scene, spot generation, dose while outside, push-your-luck return, supply crates | Export a Gale crater image from Mars Trek (see Assets) | Trips return materials and dose as in the spec |
| M8 | Story and endings (2 d) | `StoryDirector` reading `Story.json` (act beats, cast lines), 4 endings, comic report screen | Write or edit the dialogue lines in `Story.json` (Claude drafts them) | Acts trigger on the right sols; each ending is reachable in a scripted test |
| M9 | Real-data layer (1–2 d) | Storm cards with real dates and IDs, dose meter from RadLab, LIVE/ARCHIVE labels, `LiveSun` call with fallback, Logbook, `DATA.md` | Check the CORS result of the live DONKI call in the WebGL build | Every data card names its source; offline build still works |
| M10 | Art and audio (3 d) | Swap placeholders for imported packs, 2D lights for powered rooms, parallax, storm particles, dust haze, audio manager | Download and import the asset packs listed under Assets; pick music | Looks like the mockup style; no missing sprites |
| M11 | Replay and modes (1–2 d) | Seasons, crew traits, blueprints carry-over, badges, Commander mode, Classroom Code seeds, Jezero unlock | Decide final trait list | Two runs with different seeds feel different; same Classroom Code gives the same run |
| M12 | WebGL polish and submit (2 d) | Build size pass, loading screen, `README.md`, `CREDITS.md`, itch.io upload notes | Player Settings (compression + Decompression Fallback), upload to itch.io, record the demo video, kid playtests | Public itch.io link and repo ready; video recorded |

Total: about 22–26 working days. If time runs short, stop after M6 + M9 and ship the MVP cut.

## Assets: where, how, who

Claude Code can fetch anything that's a plain public download or API. **You** need to get anything behind a login, a click-through download page, or a manual export, and import packs into Unity yourself. All third-party files go in `Assets/ThirdParty/<pack>/` with a line in `CREDITS.md` (name, author, license, URL).

| Asset | Where | License | Who gets it | How |
| --- | --- | --- | --- | --- |
| Base modules, astronauts, props | [Kenney](https://kenney.nl/assets) (filter by Space; e.g. Space Kit, which is 3D low-poly and can be rendered to sprites or used directly in 2.5D) | CC0 | **You** | Download the zip from the pack page, unzip into `ThirdParty/Kenney/`. Check exact pack names on the site |
| UI frames and buttons | Kenney UI Pack | CC0 | **You** | Same as above |
| Icons (power, air, radiation, tools) | Kenney Game Icons or [game-icons.net](https://game-icons.net) | CC0 / CC BY 3.0 (credit required) | **You** | Download the PNGs you need; note game-icons authors in CREDITS |
| Fonts: Nunito, Baloo 2 | [Google Fonts](https://fonts.google.com) (source files also on the google/fonts GitHub repo) | OFL | **Claude Code** fetches the TTFs; **you** create the TextMeshPro font assets (Window › TextMeshPro › Font Asset Creator) |  |
| Mars photos (Curiosity, Perseverance) | NASA Image and Video Library API (images-api.nasa.gov) | NASA media: free to use, no NASA logo, no implied endorsement | **Claude Code** | Script searches by keyword, downloads chosen images, records NASA ID and credit line |
| Gale crater map image | [Mars Trek](https://trek.nasa.gov/mars/) | NASA data | **You** (easiest) or Claude Code | Export one image of Gale from the Mars Trek portal; scripting the WMTS tiles is possible but needs layer IDs from GetCapabilities |
| Mars wind sound | NASA's Perseverance "Sounds of Mars" recordings | NASA media | **You** | Download the clip from NASA's Mars sounds page; import as OGG/MP3 |
| Sound effects | Kenney audio packs (e.g. Sci-fi Sounds, Interface Sounds); [freesound.org](https://freesound.org) filtered to CC0 | CC0 | **You** (freesound needs an account) | Download, convert to OGG if needed |
| Music | Kenney music loops or [Incompetech](https://incompetech.com) | CC0 / CC BY (credit required) | **You** | Download one calm loop and one tense loop |
| NASA data (DONKI, RadLab, OSDR) | APIs in the Real NASA data section | NASA open data | **Claude Code** | `tools/fetch_data` (M1) |
| Unity packages (Input System, Newtonsoft JSON, Test Framework) | Unity Package Manager | Unity | **Claude Code** adds them to `Packages/manifest.json`; **you** open Unity once to resolve |  |

**Import settings (Claude Code can apply these with an `AssetPostprocessor` script):** sprites at 64 pixels per unit, Bilinear filter, max size 1024, WebGL compression ASTC or DXT with Crunch; audio as Vorbis, music streamed, effects decompressed on load.

**Fallback while you collect assets:** Claude Code generates simple placeholder sprites (flat coloured shapes with outlines, like the mockup) so M2–M9 never wait on art.
