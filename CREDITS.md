# Credits

Outpost Gale is a free, non-commercial learning game made for the NASA Space Apps Challenge 2026.
**NASA does not endorse this game** and is not responsible for it. No NASA or JPL logos are used.
A short version of this list is on the in-game Credits screen (`Assets/Resources/Data/Credits.txt`).
How each dataset drives the game is explained in [DATA.md](DATA.md).

## NASA data

| What | Used for | Source |
|---|---|---|
| DONKI space weather database: solar energetic particle (SEP) events and flares, 2012–2025 | Every storm night (real dates and event IDs), Earth's radio warnings | NASA CCMC, https://ccmc.gsfc.nasa.gov/tools/DONKI/ |
| DONKI live: flares from the last 30 days | "Today's Sun" on the title screen, briefings and NASA News screen | Same, queried at startup |
| Open Science Data Repository: studies OSD-489, OSD-104, OSD-7, OSD-678, OSD-334 | Fact cards about bones, muscles, plants and radiation | NASA OSDR, https://osdr.nasa.gov |
| Curiosity RAD surface radiation (published average, 0.64 mSv a sol) | Commander-mode real units, NASA News | Hassler, D. M. et al. (2014), *Science* 343, 1244797 |
| Curiosity SAM: water in Gale crater soil (1.5–3% by weight) | Why water is baked out of soil | Leshin, L. A. et al. (2013), *Science* 341, 1238937 |

Each bundled data file in `Assets/StreamingAssets/data/` records its `source` and `retrieved_on` date.
Fact cards in `Assets/Resources/Data/Facts.json` each name their source.

## NASA images and sounds

Used under the [JPL Image Use Policy](https://www.jpl.nasa.gov/jpl-image-use-policy/) and NASA's media guidelines
(free educational use, credit given, no endorsement implied).

| File | Credit | Notes |
|---|---|---|
| `Art/gale_map.png` | Courtesy NASA/JPL-Caltech/ASU, PIA14290 "Gale Crater: Future Home of Mars Rover Curiosity" (Mars Odyssey) | Trip map; cropped and tinted |
| `Art/trip_dunes.jpg` | Courtesy NASA/JPL-Caltech/MSSS, PIA19928, Bagnold Dunes, Curiosity Mastcam, 25 Sept 2015 | Trip scene; mosaic borders cropped |
| `Art/trip_rocknest.jpg` | Courtesy NASA/JPL-Caltech/MSSS, PIA16204, Rocknest, Curiosity Mastcam, 2012 | Trip scene; mosaic borders cropped |
| `Art/trip_yellowknife.jpg` | Courtesy NASA/JPL-Caltech/MSSS, PIA16701, Yellowknife Bay, Curiosity Mastcam, released 15 Jan 2013 | Trip scene; mosaic borders cropped |
| `Audio/mars_wind.mp3` | NASA/JPL-Caltech/LANL/CNES/CNRS/ISAE-Supaero, "Perseverance rover's SuperCam records wind on Mars" (2021) | Public domain, via Wikimedia Commons |
| `Audio/dust_devil.mp3` | NASA/JPL-Caltech/LANL/CNES/CNRS/INTA-CSIC/Space Science Institute/ISAE-Supaero, "NASA's Perseverance Records a Martian Dust Devil" (2021) | Public domain, via Wikimedia Commons |

Photos were found through the NASA Image and Video Library (https://images.nasa.gov); each caption and credit
is shown in the game next to the photo (`Assets/Resources/Data/Trips.json`).

## Third-party art and sound

| Pack | Author | Licence | Used for |
|---|---|---|---|
| [Sci-Fi RTS](https://kenney.nl/assets/sci-fi-rts) | Kenney | CC0 1.0 | Base modules, rocks, ground tiles, fallback astronauts |
| [Game Icons](https://kenney.nl/assets/game-icons) | Kenney | CC0 1.0 | HUD icons |
| [Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney | CC0 1.0 | Sound effects |
| [Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | Kenney | CC0 1.0 | Sound effects |

The sound effects (build, chime, click, error, radio, trip) come from the two Kenney sound packs.
Licence files: `Assets/ThirdParty/Kenney/`.

## Fonts

| Font | Copyright | Licence |
|---|---|---|
| Nunito | Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito) | SIL Open Font License 1.1, `Assets/ThirdParty/Fonts/Nunito_OFL.txt` |
| Baloo 2 | Copyright 2019 The Baloo 2 Project Authors (https://github.com/EkType/Baloo2) | SIL Open Font License 1.1, `Assets/ThirdParty/Fonts/Baloo2_OFL.txt` |

The font files ship in `Assets/Resources/Fonts/`; the web loading screen loads the same fonts from Google Fonts.

## Original work and use of AI

The game idea, design and plan ([docs/PLAN.md](docs/PLAN.md)) are the team's own. An AI coding assistant
(Claude, by Anthropic) helped write the code, tests, data scripts and the drawing scripts below.

**AI-assisted images.** These were drawn by Python scripts written with the AI assistant (no image-generation
model was used). They are labelled as AI-assisted on the title screen and in the in-game credits:

| Image | Script |
|---|---|
| BOLT the robot (`bolt.png`) | `tools/art/make_bolt.py` |
| The astronaut, front and side walk (`char_*.png`) | `tools/art/make_character.py`, `make_character_side.py` |
| Mira and Tomas (`crew_*.png`) | `tools/art/make_crew.py` |
| Airlock suit rack (`airlock_rack.png`) | `tools/art/make_suit_rack.py` |

Generated in code at runtime: the Mars terrain, sky, base interior, UI shapes, storm siren and footsteps.
None of the NASA images, recordings or data were generated or altered beyond cropping, tinting and resizing.
