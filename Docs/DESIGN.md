# Football Mogul — Design system and screens

## Identity

Dark sports aesthetic, professional dashboards, cinematic football.
No cartoon, pixel, or cel-shaded style — the game should look like
a broadcast control room, not a toy.

## Tokens

- Background: #0F1220
- Surface: #171A2B
- Ink (headings): #FFFFFF
- Body: #C7CBD8
- Muted: #8A90A3
- Brand: #5548F5
- Brand dark: #4538D6
- Mint (success / positive): #34D399
- Danger: #B3261E
- Outline: #2A2E44
- Info banner: #D1ECF1 on #0B5ED7
- Warning: #FFF3CD on #8A5A00

Type: a clean sans (Unity default in v1; a licensed sports sans
in the polished UI). Headings bold, body regular, numbers tabular
where they appear in tables.

## Screens (in order of build)

1. **Main Menu** — title, "Build a legacy. Rule the game.",
   Choose Your Destiny: League / Career / Manager, Offline Ready,
   Continue (if a save exists), Settings.
2. **League picker** — Country → League (tier 1 / tier 2) → Club.
   20 countries, 40 leagues, 799 clubs. Search by club name.
3. **Club home dashboard** — next fixture, table position, funds,
   morale, form, board confidence, news feed, quick actions.
4. **Squad** — players by position, attributes (6 families),
   morale/fitness/form, contract, value, wage, release clause.
5. **Tactics board** — formation picker (8 formations + custom),
   philosophy picker (8), instructions (pressing, line, width,
   mentality, protect/chase), 2D pitch with player dots.
6. **Fixtures & Table** — round-by-round fixtures, sorted table,
   promotion/relegation zones marked, top scorer.
7. **Transfers** — scout reports, 7-stage deal pipeline, market
   list, free agents, loans, Deadline Day countdown + news.
8. **Match centre** — three views from one simulation:
   Instant Simulation (result + stats), Highlights (goals,
   chances, saves, cards), Full Match (minute-by-minute on
   2D pitch, 3D stub in v1).
9. **Career** — created player, 5 chapters, attributes, training,
   lifestyle (houses, cars, business, endorsements, charity,
   family), relationships, awards, Hall of Fame.
10. **Manager** — record, win %, trophies, clubs, players
    developed, transfer profit, tactics history, board confidence,
    legacy record, Hall of Fame.
11. **Departments** — 9 departments, levels 1–10, upgrade costs,
    effects (youth intake, scouting accuracy, injury recovery,
    coaching lift, finance, marketing, stadium income).
12. **Story events** — modal with choices and explicit outcomes
    (captain's ultimatum, board warnings, media, rivalry events).
13. **Settings** — save, load, reset, offline/online toggles,
    audio (later phase), language (later phase).

## Flows that must never break

- Start a season → pick a club → play a round → table updates →
  save survives a quit → continue resumes at the next round.
- A transfer that fails at any stage leaves funds and squads
  unchanged. A transfer that registers moves funds and the player
  atomically.
- A story choice shows its outcome before the next screen, and
  its morale/funds/reputation effects are visible in the club
  dashboard.
- A match result in Match centre is the same result Play
  records in the season (one simulation, three presentations).
