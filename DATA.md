# NASA data in Outpost Gale

Every data-driven card in the game says **LIVE** or **REAL ARCHIVE** and names its source.
The archive is bundled in `Assets/StreamingAssets/data/` so the game works offline. Refresh it with
`node tools/fetch_data/index.js` (no API keys). Each file records `source` and `retrieved_on`.

| Dataset | File | What it drives | How it was simplified |
|---|---|---|---|
| [DONKI](https://ccmc.gsfc.nasa.gov/tools/DONKI/) solar energetic particle (SEP) events, 2012–2025 | `donki_seasons.json` | Storm nights. Each mission replays one real year: its SEP days are squeezed in order into the 20 or 30 sols. The storm card shows the real date, DONKI event ID, instrument and linked flare class. | One storm per real day. Cadet uses calm and moderate years, 3–5 storms; Commander uses any year, 5–8 storms (evenly sampled when a year had more). The last 1–2 sols always have a storm (the finale). |
| DONKI flares (FLR), M5 and above | `donki_seasons.json` | Earth's Radio warnings name the linked flare class. | Flares are shown as *warnings*; the radiation hit comes from the SEP event (science fix from the plan). |
| DONKI live (last 30 days of flares) | fetched at startup | "Today's Sun" banner on the title screen and in morning briefings. | If the call fails (offline, blocked), the banner falls back to the archive and its label changes to REAL ARCHIVE. CORS was checked on 2026-11-14: DONKI sends `Access-Control-Allow-Origin: *`. |
| Curiosity RAD (via the published average) | `radiation.json` | Commander-mode real units: normal radiation at Gale ≈ 0.64 mSv a sol ≈ 6 chest X-rays. | **Published value, not a live query.** The RadLab API (`visualization.osdr.nasa.gov/radlab/api/?query=…`) returns MSL-RAD-Surface rows, but no dose column name we tried returns values. Source: Hassler et al. 2014, *Science* 343, 1244797. Storm damage in health points is a game number. |
| [OSDR](https://osdr.nasa.gov) studies OSD-489, OSD-104, OSD-7, OSD-678, OSD-334 | `osdr_facts.json` | Fact cards about bones, muscles, plants and radiation. | Kid-friendly text written by us; the study title is fetched from OSDR so each card can be checked. |
| [Mars Trek](https://trek.nasa.gov/mars/) | (not yet) | Expedition map image. | Placeholder crater drawing until a Gale image is exported (see README). |

Other facts (MOXIE, the 2018 dust storm, light delay, ISS water recycling) are in
`Assets/Resources/Data/Facts.json`, each with a source line.

## Known gaps
- RadLab dose columns: find the right column name (RadLab GUI → Data API page was returning HTTP 500 on 2026-11-14).
- Storm cards say "measured from near Earth" unless a DONKI instrument is at Mars (none in the current archive).
