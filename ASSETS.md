# Art and audio

Every file the game loads at runtime lives in `Assets/Resources/`. Where each one comes from, and its licence,
is in [CREDITS.md](CREDITS.md). Import settings (sprites at 64 px per unit, compressed, crisp mipmapped
character art; Vorbis audio, streamed music) are applied automatically by `Assets/Scripts/Editor/BuildScript.cs`.

## Art: `Assets/Resources/Art/`

| Files | What | Source |
|---|---|---|
| `bolt.png`, `char_*.png`, `crew_*.png`, `airlock_rack.png` | BOLT, the astronaut (front and 4 walk frames), Mira and Tomas, the airlock suit rack | Drawn by `tools/art/*.py` (AI-assisted, see CREDITS) |
| `hab`, `airlock`, `shelter`, `solar`, `panel`, `wall`, `oxygen`, `greenhouse`, `battery`, `recycler`, `gym`, `dish` (`.png`) | Base modules on the map, and the machines inside | Kenney Sci-Fi RTS (CC0) |
| `rock*.png`, `ground*.png` | Rocks and ground tiles | Kenney Sci-Fi RTS (CC0) |
| `astronaut.png`, `mira.png`, `tomas.png` | Fallback people, used only if the drawn characters are missing | Kenney Sci-Fi RTS (CC0) |
| `icon_*.png` | HUD icons | Kenney Game Icons (CC0) |
| `gale_map.png` | Trip map of Gale crater | NASA/JPL-Caltech/ASU, PIA14290 |
| `trip_*.jpg` | Trip scene photos (Bagnold Dunes, Rocknest, Yellowknife Bay) | NASA/JPL-Caltech/MSSS, Curiosity Mastcam |

To redraw the original characters: `python tools/art/make_bolt.py` (likewise `make_character.py`,
`make_character_side.py`, `make_crew.py`, `make_suit_rack.py`). They need Python 3 with Pillow.

## Audio: `Assets/Resources/Audio/`

| File | Used for | Source |
|---|---|---|
| `mars_wind.mp3` | Title screen and ambience | NASA Perseverance SuperCam recording (public domain) |
| `dust_devil.mp3` | Dust devil on a trip | NASA Perseverance SuperCam recording (public domain) |
| `build`, `chime`, `click`, `error`, `radio`, `trip` (`.ogg`) | Sound effects | Kenney sound packs (CC0) |

The storm siren and footsteps are generated in code.

## Replacing art or sound

The game uses whatever file is in these places, by name, and falls back to code-drawn placeholders when
one is missing. Optional extras it will pick up: `pad.png` (landing pad), `jezero_map.png` (Jezero trip map),
`terrain.png` (map ground), and `music_calm` / `music_tense` audio. Add a line to CREDITS.md for anything new.
