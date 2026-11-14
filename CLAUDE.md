# Outpost Gale
Unity 6000.6.7f1, URP 2D, WebGL target. Design and plan: docs/PLAN.md (read the
"Simulation spec", "Base building spec" and "Implementation plan" sections first).

## Rules
- Game rules live in Assets/Scripts/Sim as plain C# (asmdef has noEngineReferences). UI and world
  scripts (Assets/Scripts/Game) only read MissionState and call Mission methods.
- Every number comes from Assets/Resources/Data/Balance.json, never hard-coded.
- Every Sim change needs an EditMode test in Assets/Tests/EditMode.
- Randomness only through the seeded MissionRng, never UnityEngine.Random (cosmetic animation excepted).
- The game builds its world, UI and sounds from code at runtime (GameManager boots via
  RuntimeInitializeOnLoadMethod into Assets/Scenes/Main.unity). Don't edit .unity or .prefab YAML.
- Real NASA data is read from Assets/StreamingAssets/data/*.json via UnityWebRequest.
  Every record keeps "source" and "retrieved_on". Never invent data values.
- Optional art/audio: drop sprites in Assets/Resources/Art/<name>.png (hab, panel, rock, gale_map …)
  and clips in Assets/Resources/Audio/<name> (mars_wind, music_calm, music_tense, build, siren …).
  The code uses them when present and falls back to placeholders.
- Third-party assets go in Assets/ThirdParty/<pack>/ and get a line in CREDITS.md.

## Commands (close the Editor first)
- Fast compile check (no Unity): bash tools/check_compile.sh
- Tests: unity test . --mode EditMode --output Logs/tests.xml
  (or "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/tests.xml)
- WebGL build: "C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe" -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod OutpostGale.EditorTools.BuildScript.BuildWebGL -logFile Logs/build.log
- Data refresh: node tools/fetch_data/index.js
