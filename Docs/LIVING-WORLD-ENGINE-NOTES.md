# Living World Engine — Study Notes (third ChatGPT link, 2026-11-01)

Source: the third shared link Hamis sent (chatgpt.com/s/t_6ae68aa360748191a72c9665c69c1bee).
Read in full via browser, 2026-11-01. This file is a paraphrase in my own
words, not a copy of that document.

## What this link actually is — and is not

It is a foundational architecture document, not a complete game design.
It defines how the world should work, not the detailed contents of
tactics, transfers, leagues, UI, audio, or art. Those details are
explicitly absent from this page, not hidden:

- No named formations, philosophies, instructions, or set pieces.
- No transfer stages, agent mechanics, loan terms, or contract clauses.
- No club/league/player lists, no ratings, currencies, or prices.
- No awards, achievements, or Hall of Fame list.
- No UI screens, flows, audio, or art direction.
- No game images at all on the page — no screenshots, concept art,
  or logos to reuse. The only visuals are ChatGPT's own interface.

Its own next step, stated at the end, is a separate Master Database
and Entity Architecture document, to be designed before implementing
individual features. That document is not on this page.

## The core idea (paraphrased)

One persistent, interconnected football universe, not three separate
games. Football, business, personal life, relationships, society and
history all influence each other. Every person has a life, every club
has a history, every decision has consequences, and every save creates
a different universe. The three modes are perspectives on the same
world, not isolated saves:

- League: play as a club institution (ownership, finances, appointments,
  transfers, academy, facilities, commercial work, supporters).
- Career: play as an individual footballer (football plus personal
  finances, relationships, education, lifestyle, commercial work,
  career transitions).
- Manager: play as a professional manager (tactics, squads, staff,
  relationships, board expectations, development, reputation).

Platforms: PC and Android, same fundamental simulation and save
structures, platform-specific presentation. Offline-first, optional
cloud sync only. Tech: Unity + C#.

## Architecture: World Orchestrator + 11 specialist engines

A central World Orchestrator coordinates but does not calculate
everything: advancing the calendar, coordinating systems, processing
scheduled activities and event order, updating relationships and
memories, coordinating financial transactions, competition progression,
histories/records, background simulation, and consistent save/load.

Under it, 11 engines (paraphrased):

1. Football Simulation — matches, movement/behaviour, tactics, roles,
   fitness/injuries, training/development, squad selection, transfers/
   contracts, recruitment/scouting, competitions/standings, youth,
   refereeing/discipline, national teams, coaching/staff development.
2. Club and Institutional Simulation — ownership, board/executives,
   sporting directors, finances, commercial work, stadium, facilities,
   supporter relations, identity/reputation, academy administration,
   recruitment departments, financial regulation, ownership changes.
3. Personal Life Simulation — family, friendships, romance, social life,
   personal decisions, property/lifestyle, personal finances, investments/
   businesses, education/qualifications, development, celebrity/commercial
   appearances, crises, retirement/alternative careers.
4. World Economy — club economies, salaries, transfer markets, sponsorship,
   property, business, investment returns, loans/debt, interest, taxes,
   inflation, regional differences.
5. Social and Relationship — relationships with partners, family, friends,
   teammates, managers, coaches, agents, business partners, journalists,
   supporters, executives. Multi-dimensional (trust, compatibility,
   affection, loyalty, communication, conflict, expectations, shared
   history) — explicitly not a single happiness score.
6. Media and Public Opinion — newspapers, sites, TV, radio, journalists,
   pundits, press conferences, transfer rumours, social media,
   controversies, supporter reactions, reputation. Stories must originate
   from actual simulated events, not fabricated news.
7. Supporter and Football Culture — supporter groups, associations,
   rivalries, traditions, chants/banners, satisfaction, protests/boycotts,
   ticket demand, attendance, loyalty, local culture. Clubs/regions must
   have distinctive supporter identities.
8. Education and Professional Progression — coaching qualifications,
   management education, sports science, business education, financial
   literacy, languages, media training, experience, practical assessments,
   qualification requirements. Qualifications need prerequisites and must
   not automatically grant professional authority.
9. Governance and Football Politics — associations, league/competition
   organisers, disciplinary bodies, financial regulations, registration
   rules, club licensing, ownership regulations, appeals/hearings,
   rule changes, competition restructuring.
10. World History and Character Memory — milestones, relationships,
    transfers/negotiations, promises and broken promises, trophies/defeats,
    rivalries, controversies, business ventures, club transformations,
    major personal decisions, retirements/transitions. Characters should
    remember significant interactions and use them in future decisions.
11. Event and Consequence — connects the other systems. One event
    (e.g. a major transfer, a commercial appearance, a takeover) ripples
    into squad quality, finances, relationships, supporter reaction, media,
    competition expectations, budgets, staff appointments, identity,
    time/fitness/selection, and future endorsements.

Technical modules (15, paraphrased): World Core, World Scheduler,
Football Domain, Match Simulation, Club Simulation, Personal Life,
Economy and Finance, Relationships, Media and Supporters, Governance,
Education and Careers, Events and Memory, Presentation and 3D,
Platform Services, Save and Persistence.

Technical constraints stated: modular domain-driven design, shared
world database, SQLite for local structured data, versioned saves,
central event/consequence framework, deterministic simulation where
practical, background simulation scheduling, data-driven configuration,
offline-first, separation of simulation from presentation.

Match presentation: the same simulation must support full 3D matches,
3D highlights, and instant simulation. Simulation logic must stay
separate from visual presentation, and 3D must not determine the result;
the simulation produces structured events displayable in any view.

Calendar: unified, covering minutes/hours for matches, daily routines,
weekly training, monthly financial obligations, seasonal competitions,
annual contracts/qualification renewals, and multi-year/generational
careers. Every activity has a duration and scheduling requirement;
the player controls simulation speed and pauses where appropriate;
the simulation resolves conflicting schedules/priorities.

Performance/scale approach: simulate important matches in greater
detail, process background matches efficiently, update distant
characters less frequently, use scheduled economic calculations,
avoid recalculating unchanged information, store history efficiently,
manage memory/assets carefully, support mobile performance without
compromising shared simulation rules.

Structured events: each event should have a unique identifier, trigger
conditions, involved characters/organisations, relevant history,
available choices, immediate and delayed consequences, follow-up
conditions, priority, cooldown/recurrence rules, and resolution status.
Types include sporting, financial, romantic, family, educational,
commercial, political, legal, social, media-related, and organisational.
The system must distinguish rumours, allegations, verified evidence,
investigations, and final outcomes.

Distinctive claims (paraphrased): one stable identity per person across
a lifetime (player → retired player → assistant coach → manager →
sporting director → club owner, keeping history, qualifications, wealth,
relationships, reputation); cross-system consequences as the core loop;
characters with memory, goals, fears, values, risk tolerance, and
knowledge-based decisions, richer for important characters, efficient
for background characters; media/supporter reactions grounded in actual
events; life simulation treated as equal to football, not flavour.

Acceptance requirements (12, paraphrased): all three modes in one world;
identities preserved across career changes; football events affect
personal/financial/social systems and vice versa; clubs, supporters,
media and governance react to actual events; the economy changes over
time and influences decisions; important characters preserve relevant
memories; the calendar coordinates football and personal activities;
historical records remain accessible across seasons; the universe
evolves through many generations; all major pillars have meaningful
gameplay; core game remains playable offline on PC and Android.

## What this changes for our Unity build — gap analysis

Already aligned:
- Unity + C#, offline-first, PC/Android, deterministic simulation,
  versioned saves, simulation separated from presentation, same event
  stream for instant/highlights/full match.
- Three modes on one world (League/Career/Manager) — currently as
  separate screens/systems, not yet as one shared world database.
- Match engine, fixtures, season, transfers, departments, story events,
  career, manager — present in Core, but at a simpler level than this
  architecture (see below).

Needs upgrading to meet this architecture:
1. World Orchestrator — GameManager is currently a bootstrap, not an
   orchestrator that advances a unified calendar and coordinates engines.
2. Unified calendar/scheduler — currently round-based, not
   minutes/days/weeks/months/years with durations, speed control, pause,
   and schedule-conflict resolution.
3. Multi-dimensional relationships — currently loyalty/morale single
   scores; this architecture explicitly rejects a single score.
4. Structured events — currently simple choices with immediate deltas;
   needs ids, triggers, delayed consequences, follow-ups, cooldowns,
   and rumour vs allegation vs verified vs outcome distinction.
5. Media and Supporter engines — not built yet; stories must come from
   actual simulated events, supporters need distinctive local identities.
6. Governance engine — not built (regulations, licensing, disciplinary
   bodies, appeals, rule changes).
7. Education/qualifications engine — career training is currently a
   simple attribute bump; needs prerequisites and qualification gates.
8. History and Character Memory engine — currently season totals only;
   needs milestones, promises, rivalries, controversies, and memory
   influencing future decisions.
9. World Economy engine — currently funds only; needs salaries, markets,
   sponsorship, property, loans/debt, interest, tax, inflation, regional
   differences.
10. Personal Life engine — career lifestyle fields exist, but family/
    romance/property/business/education/crises/retirement as a full
    equal system is not built.
11. Lifetime identity / role transitions — a person cannot yet move
    from player to coach to manager to owner keeping one identity.
12. Persistence divergence to decide: this architecture specifies SQLite
    for local structured data; our current saves are versioned JSON
    (MiniJson, dependency-free). Either adopt SQLite in Unity or
    document JSON as the equivalent versioned persistence — do not
    silently ignore the difference.

Already beyond this document (implementation choices, not from it):
- Transfer system: our 7-stage state machine, loans, release clauses,
  free transfers — this document does not specify transfer stages.
- Departments list (9), tactics (8 formations, 8 philosophies),
  awards, and the 799-club database — these come from the earlier
  orders and club research, not from this document, which gives no
  club/league/player lists or numbers.

## Next step implied by the document itself

Design the Master Database and Entity Architecture before implementing
individual game features. That is the document's own stated next step,
and it is the logical next build step after these notes.
