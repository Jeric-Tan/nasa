# Assets to add (art and audio)

The game runs today with placeholder art and sounds generated in code. Drop real files in these
exact places and names, and the game picks them up automatically (no code changes). Then add a line
to `CREDITS.md` for each pack.

Import settings (64 px per unit, bilinear, max 1024, compressed; audio as Vorbis, music streamed) are
applied automatically by `Assets/Scripts/Editor/BuildScript.cs` to `Assets/Resources/Art`,
`Assets/Resources/Audio` and `Assets/ThirdParty`.

## Sprites: `Assets/Resources/Art/<name>.png`
Top-down or 3/4-view sprites; each is stretched to its module's footprint (in tiles, width × height).

| File | Module | Footprint |
|---|---|---|
| `hab.png` | Hab core | 3 × 2 |
| `airlock.png` | Airlock | 1 × 1 |
| `shelter.png` | Storm Shelter | 2 × 2 |
| `solar.png` | Starting solar array | 3 × 1 |
| `panel.png` | Extra solar panel | 2 × 1 |
| `wall.png` | Regolith shield wall segment | 1 × 1 |
| `oxygen.png` | Oxygen maker | 2 × 1 |
| `greenhouse.png` | Greenhouse | 2 × 2 |
| `battery.png` | Big battery | 1 × 1 |
| `recycler.png` | Water recycler | 2 × 1 |
| `gym.png` | Gym corner | 1 × 1 |
| `dish.png` | Comms dish | 1 × 1 |
| `pad.png` | Landing pad | 3 × 3 |
| `rock.png` | Rocks on the grid | 1 × 1 |
| `gale_map.png` | Expedition map (square, about 1024 px), Gale crater from Mars Trek | — |
| `jezero_map.png` | Expedition map for Jezero (unlocks after a finished mission) | — |

Where to get them:
- **Kenney** (kenney.nl/assets, CC0, no login): pick a space/sci-fi pack (check the exact pack names on the site).
  Unzip the pack into `Assets/ThirdParty/Kenney/<pack>/`, then copy or rename the sprites you want into `Assets/Resources/Art/`.
- **Mars Trek** (trek.nasa.gov/mars): find Gale crater, export one image, save as `gale_map.png`. NASA data; no NASA logo.

## Audio: `Assets/Resources/Audio/<name>.ogg` (or .mp3 / .wav)
| File | Used for |
|---|---|
| `mars_wind` | Title screen and ambience. Use NASA Perseverance's real Mars wind recording ("Sounds of Mars"); the title then says "this is what Mars really sounds like". |
| `music_calm` | Daytime loop |
| `music_tense` | Storm nights |
| `build` | A module finishes |
| `siren` | A storm hits |
| `chime` | Shelter sealed / problem fixed |
| `click` | Buttons |
| `step` | Footsteps |
| `error` | Can't do that |

Sources: Kenney audio packs (CC0), freesound.org filtered to CC0 (needs an account), Incompetech (CC BY, credit required).
