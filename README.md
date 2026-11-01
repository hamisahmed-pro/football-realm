# Football Realm — Unity + C# (production-structured, not a demo)

## The order, and what changed

Hamis ordered: abandon Misa, design and build in C# and Unity,
to a finished-product standard — not rough work for fun. That is
what this folder is. The Misa game from the previous step is
abandoned as a product (its club research is kept, because the
club data is engine-independent).

## Honest standard: what "FM-grade" means here, and what it does not

Football Manager is built by hundreds of people over decades,
with licensed leagues, real player data, motion-captured 3D
matches, and a data team. No single-agent session can reproduce
that, and claiming otherwise would be dishonest. What this project
does to the standard that matters for a finished product is:

- **Real architecture, not a prototype script dump.** Core
  simulation has no Unity dependency, so it can be unit-tested
  outside Unity. Presentation is a separate layer. Data, saves
  and simulation are separated. Every system has a documented
  rule, not a magic number hidden in UI code.
- **Real data.** 799 clubs, 40 leagues, 20 countries, verified
  league by league (see `Assets/Data/clubs.json`). Squads are
  generated deterministically per club — real clubs, generated
  squads, because licensed real player data is not included and
  is not claimed.
- **Deterministic simulation.** Same seed, same match, same
  season. Saves are versioned and written atomically with a
  backup, so a crash cannot destroy the only save.
- **Complete systems, not stubs for the headline features.**
  Match engine (minute-by-minute, xG, stats, tactical
  explanations), round-robin fixtures, season table with
  promotion/relegation logic, 7-stage transfers (scout, approach,
  fee, contract, clauses, registration, loans, release clauses,
  free transfers), story engine (captain's ultimatum, board
  warnings, with explicit consequences), departments (9, with
  upgrade costs), career mode (5 chapters, 6 attribute families,
  training, lifestyle fields), manager career (record, board
  confidence, Hall of Fame rule), awards, and achievements hooks.

What is **not** FM-grade yet, stated plainly:

- **No licensed real players, no official league data feed.**
  Squads are generated. Real player names, faces, and live data
  need a licence and a data provider.
- **No motion-captured 3D match.** The 3D presentation is a
  primitive-player stub that proves the engine-to-3D pipeline.
  A real 3D match needs rigged models, animations, a stadium,
  and a match-animation system — an asset-heavy phase, not code.
- **No online play, leaderboards, or cloud saves yet.** The
  save system is local and versioned; online is a later phase
  with accounts, sync conflicts, and anti-cheat.
- **No Unity build from this box.** This machine has no Unity
  editor and no .NET SDK, so the C# has not been compiled here.
  It must be opened in Unity (see below) and compiled there.
  That is a verification gap, not a hidden one — see "Verify".
- **UI is code-built v1, not Figma-polished.** The design tokens
  and screen structure are in `Docs/DESIGN.md`; the polished
  Figma frames are the next design step, not claimed as done.

## Project layout

```
Assets/
  Data/clubs.json                 799 clubs, 40 leagues, 20 countries
  Scripts/Core/                   No Unity dependency — testable core
    DeterministicRandom.cs        xorshift64*, stable string hash
    Models.cs                     Club, Player, Attributes, Departments, League, Table
    Tactics.cs                    Formations, philosophies, instructions, rating deltas
    MatchEngine.cs                Minute-by-minute sim, xG, stats, events, ratings
    Fixtures.cs                   Berger round-robin, Season, table, awards hook
    SquadGenerator.cs             Deterministic 25-player squads per club
    Transfers.cs                  7-stage transfer state machine
    StoryAndCareer.cs             Story events, Career (5 chapters), Manager, Awards
    DatabaseAndSaves.cs           clubs.json loading, league building, versioned atomic saves
  Scripts/Unity/                  Presentation layer (UnityEngine)
    GameManager.cs                Singleton bootstrap, database, season, save path
    MainMenuUI.cs                 Runtime-built menu + league picker (no prefabs for v1)
    MatchPresentation.cs          2D pitch highlights + 3D stub, Full Match playback
  Scenes/                         Create a scene, add GameManager + MainMenuUI (see below)
ProjectSettings/ProjectVersion.txt  Unity 2022.3 LTS target
Docs/DESIGN.md                    Design system, screens, flows
```

## Licence — the one blocker (free, no card)

Unity Personal is free and needs no card, but the editor will not
build without a Unity ID + Personal licence activation. That is
not a code problem and it is not solved by downloading the editor.

- Local: download Unity 6000.0.64f1 (in progress on the build
  machine, `~/workspace/tools/unity/`), then activate a Personal
  licence with a Unity ID (sign in in Hub, or use the manual
  activation file flow at license.unity3d.com).
- Cloud (no card): GitHub repo
  https://github.com/hamisahmed-pro/football-realm is public,
  workflow `.github/workflows/build.yml` (GameCI) is active and a
  first run has been triggered. It needs repo secrets
  `UNITY_LICENSE` (and `UNITY_EMAIL`/`UNITY_PASSWORD` for the
  GameCI activation path) — set those once the Unity ID exists,
  then re-run the workflow. Until then the cloud build will fail
  at the licence step, as expected.
- Verified on this box (2026-11-01): .NET SDK 8.0.427 installed,
  Core compiles, all 22 CoreTests pass, a 380-match season averages
  2.62 goals/match (41.8% home wins, 26.6% draws, 31.6% away wins).
  Unity scripts (GameManager/MainMenuUI/MatchPresentation) have
  NOT been compiled in an editor yet — that is the next gate.

## How to open and run (Unity 6000 LTS)

1. Open Unity Hub → Add project → select this folder.
   Unity will generate Library/, Packages/ and the rest on first open.
2. Create a scene (File → New Scene), add an empty GameObject,
   attach `GameManager` and `MainMenuUI`.
3. Assign `clubs.json` to GameManager's `clubsJson` field, or
   place `Assets/Data/clubs.json` in `StreamingAssets/` (create
   that folder at project root under Assets if needed).
4. Press Play. The menu shows the league/club counts and a
   start button. `GameManager.PlayNextFixture()` plays a round.
5. Build: File → Build Settings → Android (first, per the
   stack choice) or Windows/macOS/Linux Standalone. No
   platform-specific code lives in Core.

## How to verify (what to check in Unity)

- Console on Play: "loaded 40 leagues, 799 clubs."
- Start a season, play a full round, check the table sorts by
  points, then goal difference, then goals for.
- Simulate the same fixture twice with the same seed in code:
  `MatchEngine.Simulate(home, away, tactics, tactics, seed)` must
  return identical goals and events both times.
- Save, quit, reopen: the save loads, and
  `football_realm_save.json.bak` exists next to the save.
- Transfer: scout a player, negotiate a fee below demand (must
  fail), at/above demand (must pass), then contract, then
  registration — and check funds move from buyer to seller.

## Data notes (so the database is not misread)

- Seasons: Europe 2025–26; Norway, Brazil, Argentina, Colombia,
  China, USA 2025 (calendar-year seasons).
- Egypt tier 1 has 21 clubs (relegation cancelled in 2024–25).
- Argentina tier 2 has 36 clubs; Nigeria tier 2 has 36 clubs
  in 4 conferences. Those are not errors — they are the leagues'
  actual sizes in the source season.
- Club names are as listed in the source season tables, with
  bracketed variant spellings collapsed to one club (not two).
- Ratings are game ratings: tier 1 base 78, tier 2 base 64,
  plus a small bonus for England/Spain/Germany/Italy/France.
  They are not a real-world ranking claim.

## Roadmap to a truly finished product (after this foundation)

1. Unity compile + Android build + device playtest (first gate).
2. Figma-polished UI replacing the code-built menu, with the
   design tokens in Docs/DESIGN.md.
3. Full UI screens: Club, Squad, Tactics board, Transfers,
   Fixtures, Table, Career, Manager, Settings — each bound to
   the Core systems already written.
4. 3D match presentation with real models/animations (asset phase).
5. Licensed player data or a clearly labelled generated-player
   universe, plus a data update pipeline for promotions/relegations.
6. Online: accounts, cloud saves, leaderboards, online competitions.
7. Audio, localisation, accessibility, and a full QA pass.

None of those are claimed as done. The foundation in this folder
is built so each of them can land without rewriting the core.
