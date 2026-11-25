# Progress (Unity)

## Phase plan (owner, 2026-11-24)
**Phase 1 — every system, graybox** (now): the remaining Godot systems plus the academy start, a
galaxy → planet → region world, many more skills per class and bigger dungeons. Milestones merge to `main`
when CI is green. **Phase 2 — visuals** (later): HD / realistic look, real VFX and UI art (owner references,
e.g. the combo-flame style, come then).

| # | Phase 1 milestone | Status |
|---|---|---|
| M1–M3 | Movement, combat, stats & skills | done |
| P1-4 | Core services: game state, save/load (3 slots + autosave), Codex notifications, clock & weather, interaction, pause | done (v0.5) |
| P1-5 | Items, inventory, equipment, loot, shop, crafting | done (v0.5) |
| P1-6 | Quests, dialog, NPCs, daily quest + Penalty Zone, academy hub & ranks | done |
| P1-7 | Four classes with many skills each | done |
| P1-8 | World: galaxy/planet/region data, streaming, regions I–VII, waystones, dynamic Gates | done (streaming: per-region scenes for now) |
| P1-9 | Gates & dungeons: abstract room/corridor generator, Red/Double Gates, bosses | done |
| P1-10 | Jobs, hidden jobs, shadow army | done |
| P1-11 | Secrets, Mystery Journal, puzzles | done (35 secrets; target 120+) |
| P1-12 | Endgame: Hollow Throne, [ ??? ], planets & galaxies, the final war | done (first world; later worlds = data slots) |
| P1-13 | Audio, localization (ID/EN), settings, quality presets | done (graybox: placeholder SFX, menus localized) |

The Godot prototype (`../aethvirse/docs/PROGRESS.md`) is the reference for what each system should do.

## M0 — Setup (2026-11-22 → 11-24)
- Unity 6.3 LTS project, boot scene, GameCI builds Windows + Android on push to `main`.
- Packages: URP 17.3, Input System 1.20, Cinemachine 3.1 (see DECISIONS.md).
- CI runs the EditMode tests before building.

## P1-21 — Seamless world (branch `p1-16-next`)

### Done
- **No loading screen between the regions of one world (§7).** The regions of a world sit side by side along the
  road north (300 m apart, joined by a walled road). Walking toward an open neighbour streams its scene in next to
  yours in the background; crossing the border makes it the current region (name, level band, weather table, map,
  autosave scene) and the player rig moves into it; walking far away unloads the region behind you.
- Regions still closed (journal page or academy rank) show a **mist wall** across the road that says why.
- Every scene is saved with the player rig (player, camera, HUD, services) inactive; a tiny activator wakes it when
  the scene is opened on its own and drops it when the scene is only streamed in, so there is never a second
  player or camera. Streamed regions keep their own monsters, Gate spots and Gate rolls.
- Fast travel, star gates and Gates (dungeon instances) still load their scene, as designed.

## P1-20 — Minimap and region map (branch `p1-16-next`)

### Done
- **Minimap** (top left, under the bars): terrain baked by looking straight down at the scene, fog of war,
  enemies (red), and landmarks once seen — waystones, roads, gates, star gates, people, chests. Secrets never show.
- **Region map** (M, or the Map touch button): the whole region with fog, landmarks, your position and facing,
  explored %, the secret tracker "Secrets 3/9" and "Sealed missions: open · done · ??? unknown" (§9.5).
- Fog of war is remembered per region in the save (hex-packed 52×52 grid).

## P1-19 — Sealed missions and cipher locks (branch `p1-16-next`)

### Done
- **14 sealed missions** (§9.1): never announced. They wake by themselves from hidden triggers — a secret found,
  a duel won, a lock opened, a page read, even the hour (03:00 with level 80+ starts "ERR: page not found") — and
  the Codex only says "[ ??? ] A sealed mission has been detected." Each is a 2–4 stage chain across regions and
  worlds (Something Breathes Below, The Debt of Bread, Names on the Cinder Seat, A Tear That Did Not Fall, The Index
  of Unwritten Books, Who Swung First, The Courier Who Never Arrived, The Valley's Slow Heartbeat, The Last
  Rehearsal, Your Name on the Wall, The Fourth Name, ERR: page not found, Measure the Dark, The Unclaimed
  Commission) and ends in a named unique, a set piece or a rune, often with a Codex line.
- **14 cipher locks**, one per region, each with a mural somewhere in the town that holds the key: Caesar shifts
  (three crows looking back = shift 3), mirror alphabets (Atbash), words written backwards, Morse taps and letter
  numbers. Letter dials work with touch. Opening one pays a reward and feeds the sealed missions.
- Every found secret now also sets a `secret_<id>` flag (sealed missions start from them).
- Builder order: … RegionBuilder → SecretDataBuilder → MysteryBuilder.

## P1-18 — Aerial combat and rival duels (branch `p1-16-next`)

### Done
- **Launcher → juggle → plunge (§6)** for all four weapons: heavy from neutral while locked on throws light
  enemies up; jump after it and the air chain (three hanging strikes; staff and bow shoot) keeps them up; heavy in
  the air plunges and ends in a shockwave that spikes juggled foes down. Big bodies, bosses and elites cannot be
  thrown. Launched enemies tumble, hang while being hit, and are knocked down when they land.
- **Duel ring at the academy** (east of the training ground): Kaelen Dray (wind, dashes and blade volleys),
  Ilyra Vance (shadow, blinks behind you), Torvald Ash the champion (earth novas). Rivals side-step your swings,
  telegraph their skills (red marks), speak at half health and scale to your level. First wins pay academy
  points, XP and a reward (Kaelen's Spare Sword, the Umbral Rune, an Ashwalker piece). After the champion, a
  fourth name appears on the board — yours.

## P1-17 — Gear depth: sets, gems, enchants, reforge, stash, mimics (branch `p1-16-next`)

### Done
- **329 items** (was 36): hunter gear in nine level bands across all worlds (Thornwarden Lv 50 → Seraphic Lv 700;
  12 pieces each incl. greatsword, twinfangs, staff, longbow), 8 named **gear sets** × 6 pieces, 28 gems,
  14 enchant scrolls, 20 named uniques with their own story (Blade of the Cinder Seat, Nyra's Reading Lamp, Hammer
  That Split the World, The First Line's Quill …), 14 region materials, more potions and food, 24 skill books,
  28 runes. Drops pick bases from your level band.
- **Gear sets** (2 / 4 / 6-piece bonuses): Ashwalker, Verdant Oath, Dunestrider, Frostveil Regalia, Drowned
  Archivist, Fracture Knight, Hollow Regent, Echo of the First Vanguard (Mythic). Set pieces drop from elites and
  bosses near your level; the Bag shows the set, worn count and which bonuses are active.
- **Gems**: Ruby / Emerald / Sapphire / Diamond / Topaz / Onyx / Bloodstone in four grades (Chipped → Royal);
  three fuse into the next grade (Craft). Generated gear rolls 0–3 sockets by rarity; unsocket for gold.
- **Enchant scrolls**: eight weapon enchants turn normal attacks into an element (Kindling Steel = Fire … the
  Mythic Scroll That Was Never Written = Void) and set up reactions; six armour enchants (DEF, HP, speed,
  cooldowns, attack speed, crit). Crafted from region materials.
- **Reforge**: reroll a generated item's affixes for gold + 2 scrap (rarity, level and sockets stay).
- New affixes: Savage (ATK%), Bastion (DEF%), Hale (HP%), of Haste (CDR), of the Gale (attack speed), of the Fox
  (move speed), of Ruin's Edge (crit damage).
- **Stash** (200 slots): every save crystal opens it; saved with the player.
- **Chests and mimics**: six chests per region refill daily; one in five is a mimic (decided per day) that fights
  back and drops loot like a boss.
- Recipes and Quartermaster stock now live in the item database (more stock: mana draughts, royal potions, gems,
  scrolls).

## P1-16 — Runes, secret skills, job skill names (branch `p1-16-next`)

### Done
- **Skill runes** (28, Uncommon → Mythic): socket one per skill in Status → Skills ("Socket" cycles the fitting
  runes in your bag, "Remove" gives it back). Overcharge / Heart of the Furnace (more damage, more MP), Quiet /
  Hermit's (cheaper), Quickening / Hourglass (shorter cooldown), Splinter / Hydra (+2 / +4 projectiles), Needle
  (pierce), Echo / Twin Moon (every hit repeats for 40 % / 75 %), eight element runes (incl. the Void rune of the
  Unwritten), Headsman's (execute), Leech / Crimson Pact, Keen, Wide / Colossus, Lingering, Mending, Thunderclap.
  Runes drop rarely (0.4 % normal, 6 % elite, 35 % boss), hide behind hollow walls, and rarer runes are rarer.
- **24 secret skills, any class**, each taught by a skill book with its own lore (Ash-Bound Hymnal, Page Without
  Edges, [ ERR: page not found ] …): Ashen Requiem, Glacier Coffin, Fracture Line (Void), Hollow Crown, Requiem of
  Echoes (two echoes of you), Phoenix Feather, C0dex Gl1tch, The First Line … The books are hidden in puzzle and
  storm-night secrets across every world, rarer ones deeper (test-enforced: every book is in a secret).
- **All 210 job skills have hand-written names and descriptions** (Shieldbreaker, Reaper's Due, Hall of Mirrors,
  Meteor Sentence, Fated Shot, Great Bear's Fall, Answer Me …).
- Builder order: … ItemDataBuilder → SecretSkillBuilder (appends books and runes) → … SecretDataBuilder.

## P1-15 — Elements, statuses, elites (branch `p1-14-beyond`)

### Done
- **Elemental auras and reactions (§6.1):** an elemental hit leaves its aura for 8 s; a second element triggers the
  reaction (name pops over the target, the Codex names each one the first time): Melt 2×, Firestorm (spreads fire),
  Magma (burn + slow), Overload (blast + stun), Blizzard, Permafrost (root + DEF down), Shatter (heavy blow on a
  frozen target, 2.5×), Superconduct (takes 35 % more), Sandstorm (blind), Thunderstorm (chains to 4), Magnetize
  (pulls), Eclipse, Corrode, Purge, Erase (Void: % max HP).
- **Status effects:** Burn / Bleed / Poison / Shock damage over time, Chill stacks (3 = Freeze), Freeze / Stun /
  Petrify / Root stop the AI, Slow, Blind (half of the blows miss), Corrode, Exposed.
- **Elites:** 8 % of camp spawns, tougher and named after 1–2 affixes: Berserk, Shielded (8 hits break the shield),
  Vampiric, Splitter, Teleporter.

## P1-14 — Beyond Aethvirse (branch `p1-14-beyond`)

### Done
- **Three galaxies, four worlds, 15 regions:** Aethvirse (I–VII + [ ??? ]); **Seren** (Glasswood Expanse, Mirror
  Shallows, The Silent Choir — Lv 400–550); **Kethra** in the Drowned Stars (Cinder Reach, Hollow Moon, The Last
  City — Lv 550–700); **The God's Seat** beyond the last sky (Lv 700–800). Each new region is a full graybox scene
  (town, huts, landmark, camps, waystones, Gate spots, roads, journal secrets).
- **Star gates:** the last page in [ ??? ] opens Seren; the Conductor (Silent Choir) opens the Drowned Stars; the
  Warden of Kethra opens the Seat. Every later world has a gate back. Fast travel lists regions per galaxy / world.
- **World guardians** (phased bosses that set their world's flag) and **the war at the Seat**: allies from every
  world (Vanguards on your side), three waves of the God's hosts, then the God in four phases calling its hosts
  again; its fall ends the game (THE END screen).
- **Ranged enemies:** they keep their distance, telegraph, then fire a dodgeable fan of shots (9 types, e.g. Sand
  Wraith, Ice Wyvernling, Bell Wraith, Star Archer, Choir Lancer). 13 new enemy types in total.
- **126 secrets** (9 per region): dawn-only, storm-at-night, a second bell song and a lonely hollow wall join the
  earlier five kinds. The §9 target of 120+ is met (test-enforced).
- Test cheat **F11**: every journal page full and every world open.

## P1-13 — Settings, language, sound (branch `p1-13-settings`)

### Done
- **Settings** (Pause → Settings; saved): camera sensitivity, invert Y, camera distance, volume, graphics preset
  Low / Medium / High / Ultra (quality level, shadow distance, LOD bias), FPS limit (VSync / 30 / 60 / 120),
  language English / Bahasa Indonesia.
- **Localization** table (EN / ID): menus, title, status, journal, bag, shops, fast travel and HUD callouts; item, quest and dialog text stay English until the Phase 2 UI.
- **Placeholder sound** (procedural): swings, hits, dodges, kills, pickups, level-up chime, the Codex ding.

## P1-12 — Endgame of the first world (branch `p1-12-endgame`)

### Done
- **Hollow Throne:** The Unwritten wakes near the empty throne (boss ×60 HP, phases); its fall writes ending A / B
  (20+ shadows) / C (30+ secrets) and points to [ ??? ].
- **[ ??? ]:** a white ring where the small gate opened in Ashfall, only at 03:xx and only with the three keys (the
  Frostveil, Archive and Fracture journal pages full). Inside: the last page; reading it opens the next world on the
  atlas (flag `last_seal_opened`); the planet / galaxy slots are ready for Part 2 / 3 content.

## P1-11 — Secrets and the Mystery Journal (branch `p1-11-secrets`)

### Done
- **Secrets framework** (data-driven): category (room, sealed mission, puzzle, chest, item, lore, anomaly), at least two
  hints each, conditions (night, weather, hour, items, flags …) or a flag that finds it, rewards. Found at their
  **SecretSpot** (walk in or use it), by **puzzles** (ordered bells; wrong step resets), or **cracked walls** (only heavy
  hits break them). A faint shimmer shows within your Sense range (higher SEN = further).
- **Mystery Journal** (Status → C): one page per region with a cryptic synopsis, "lines revealed x/y", entries found
  [*] / hinted [?] (hint text) / unknown [ ]. Hints come from notes in the world.
- **Page full → "Every line has been written." → [ SYSTEM UPDATE ]** and the road to the next region opens (the
  STORY rule: mist until the page is complete). The internal word "seal" never shows (test-enforced).
- **35 secrets**: Ashfall's five come from the public prologue (the breathing drain in the rain, the bread seller who
  waits, the small gate of eight years ago, the humming that fades, the room the ash closed off); every other
  region has a night secret, a weather secret, a bell puzzle, a hollow wall and an offering.
- Test cheat **F10**: find every secret of the current region (`reveal_secrets`). Saves v6 keep secrets and hints.

## P1-10 — Jobs and the shadow army (branch `p1-10-jobs`)

### Done
- **21 jobs** (Godot job tree): Job Change I at Lv 30 (two per class + Summoner for any class), II at Lv 60
  (Bastion, Warlord, Nightreaper, Illusionist, Archmage, Oracle, Deadeye, Beastwarden, Conjurer), hidden jobs through
  flags set by sealed missions (Necromancer, Shadow Sovereign at Lv 90, Echo Warden). Jobs add stats, ATK / DEF / HP
  multipliers, MP cost multipliers and **10 skills each** (7 actives with their own looks + 3 passives).
- **Job trial:** Status (C) → Job → "Take the trial" opens a trial Gate; clearing it grants the job. Hidden jobs are
  accepted at once.
- **Summons:** Summoner familiars fight beside you; enemies switch to a minion that is clearly closer.
- **Shadow army** (Necromancer line): "Rise, Echo" binds a recently fallen enemy; "Answer me" calls your shadows;
  shadows earn XP from their own kills and rank up Soldier → Knight → Elite → Commander → Marshal (power ×1 … ×2.4).
- Saves v5 keep the job and the shadow army.
- Fixed: region data lived in the wrong file (the file-name guard test caught it), so the atlas was empty.

## P1-8 / P1-9 — World, Gates and dungeons (branch `p1-8-world`)

### Done
- **World atlas:** galaxy → planet → region (Resources/WorldAtlas). Planet Aethvirse: regions I–VII (Ashfall Outskirts
  Lv 1–50 … Hollow Throne Lv 300–350) + the hidden [ ??? ]; slots for the next planet and galaxy (story Part 2 / 3).
- **Seven region scenes** (graybox, `AETHVIRSE > Build World`): walled town, village, landmark, rocks, 8 monster camps
  (level rises with distance from the entry), 3 waystones, 4 Gate spots, roads to the previous / next region
  (misty until the academy rank is high enough), Ashfall's back road to the academy. Each region has its own
  palette, weather table and two monster types (14 new enemy types: size, speed, power, knockback / throws).
- **Waystones + fast travel** across regions (attune by walking past).
- **Dynamic Gates:** every 2 game hours a region may open a Gate (rank E–S, sometimes a Red or Double Gate; max 2);
  left for a game day it **breaks** — a wave pours out; beat it and the Gate collapses with a reward.
- **Gate dungeons** (seeded, abstract): five map shapes (winding, spiral, ring, fork, hub), five room shapes (rect, cross,
  L, round, pillared) as platforms over a pit with bridges; 5–15 rooms by rank; monsters wake per room; treasure room;
  **boss** with phases at 70 / 40 / 15 % (enrage, adds); **Red Gate**: no exit until the boss falls, stronger monsters,
  triple gold; **Double Gate**: a cracked wall hides a secret room with a sealed chest. Clearing pays gold, academy
  points, boss loot and counts for daily quests.
- Saves v4 keep the open Gates. Tests: dungeon reachability / determinism / sizes, Gate timing, atlas consistency.

## P1-7 — Four classes (branch `p1-7-classes`)

### Done
- **Class select at New Game:** Paladin (greatsword), Assassin (twin daggers), Mage (staff, magic damage),
  Marksman (bow). Stats, weapon combo, starter weapon and the graybox weapon follow the class; saves keep it.
- **15 skills per class** (11 actives + ultimate + 3 passives, unlocking from level 1 to 25) and 3–4 skill combos
  each — 60 skills in total. Every active skill of a class has its own look (test-enforced).
- **New skill kinds:** projectiles (homing, piercing, spreads), heals, stealth (enemies lose you; first hit out of
  it is a guaranteed crit; attacking ends it), traps (burst when an enemy steps in), plus melee / dash / AoE / buffs.
- **Projectiles** for staves and bows: light chains of homing bolts / arrows, heavy branches with piercing lances,
  fans and orbs; walls stop them.
- New graybox looks: nova, pillar / smite, blink, barrier, heal rings, shadow, volley, bolt flash, trap ring,
  storm / thunder bolts, frost spikes, spin ring — tinted with the skill's colour.

## P1-6 — Quests, dialog, academy (branch `p1-6-quests`)

### Done
- **New games start at Aster Vey Academy** (story start, graybox hub): classroom (Instructor Horne, Headmaster
  Ilsveth), library (Nyra; closed 22:00–06:00), dormitory (bed = save + sleep), training ground (dummies, rival
  Kaelen), Quartermaster, quest board, Expedition Gate → Ashfall Outskirts (and a gate back).
- **Quests:** data-driven objectives (kill, talk, collect, reach area, clear Gate, flag, attack, dodge, distance,
  pickup, craft, use item, skill, secret), stages that run in order with optional objectives, rewards (items, gold,
  XP, flags, academy points, Codex points, follow-up quests), auto-start by conditions, timed quests, HUD tracker.
- **Story start:** The Cinder Seat → First Field Assignment (Ashfall, 3 Ash Hounds) → Prove You Belong (rival);
  side quest Ink and Ash (Nyra); board bounty Ash Hound Cull.
- **Academy rank E → S** from academy points (= Guild rank), rank-up message.
- **Codex daily quest:** training + hunting + gathering task each day, scaled by level; missing a day → warning →
  **Penalty Zone** (survive 90 s of growing waves, then the exit portal takes you back). **Emergency quests**
  (12 % per game hour): defeat 6 monsters in 3 minutes.
- **Dialog:** branching entries chosen by conditions (flags, quest stages, items, time …), daily small talk,
  choices with gold costs and effects (items, gold, flags, quests, shop), typing effect; NPC schedules.
- **Conditions** shared by quests, dialog and (next) secrets. **Game events hub** for kills, attacks, dodges,
  distance, pickups, crafting, items, skills, talks, areas, Gates.
- Scene travel carries the player (stats, bag, skills) between scenes; saves v3 add quests, academy rank,
  Codex points and the pending penalty.
- Ash Hound is a prefab now (runtime spawns). A test guards that every component / asset class lives in a
  file of the same name (Unity cannot load it otherwise).

## P1-5 — Items & loot (branch `p1-5-items`)

### Done
- **Items** (36, from Godot; `AETHVIRSE > Build Item Data`): consumables, materials, keys, issued weapons for all
  four classes, 9 drop bases (weapon, off-hand, helm, armor, gloves, boots, ring, amulet, rune core), named /
  secret / cursed / evolving gear (Rusty Pipe → Fanged Pipe → First Fang after kills), costumes.
- **Inventory:** 60 stacks, 11 equipment slots (two rings), unique copies never merge, cursed gear will not come off.
- **Generated gear:** rarity Common → Mythic (stat ×1 … ×2.6), item level, prefix / suffix affixes
  (STR/AGI/INT/VIT/SEN, crit, lifesteal); upgrade +1 … +15 (cost grows, risk of dropping a level from +11).
- **Loot:** enemies drop gold (from level scaling) and items by tier (gear chance 8 % / 60 % / 100 %, herbs, scrap,
  potions, mystery boxes); loot cubes in rarity colours fly to you. Mystery boxes can upgrade their tier while opening.
- **Equipment feeds stats** (weapon ATK, DEF, stat bonuses, crit, lifesteal); curses drain HP; starter kit =
  issued greatsword + 3 potions.
- **Inventory window (I):** bag sorted by rarity, details, equip / unequip / use / sell / salvage / upgrade;
  Quartermaster shop (NPC by the spawn); Codex Shop (8 rare offers that change every game day); crafting
  (potions, mystery box). Quick potion: H / D-pad down.
- Saves now include the inventory (save format v2; v1 saves keep the starter kit).

## P1-4 — Core services (branch `p1-4-core-services`)

### Done
- **Game session** (lives across scenes): world clock (24 real min = 1 game day), weather per region table
  (clear / rain / fog / storm / snow / sandstorm / ashfall), world flags + counters, gold, play time.
- **Save / load:** 3 manual slots + autosave (every 5 min and at save crystals), versioned JSON, gzip, SHA-256
  checksum, atomic write with a .bak; corrupted / truncated / foreign / future files are refused. Saves the
  player (level, XP, points, skills, slots, position, HP/MP) and the world (clock, weather, flags, gold, region).
- **Title screen:** Continue (latest save), New Game, Load. Boot → Title → Playground.
- **Pause menu** (P / Start / ⏸ / Esc with a free cursor): resume, save, load, title, quit; time stops.
- **Codex notifications:** slide-in cards with a procedural "ding"; level up, new skill, autosave, new day,
  weather and world objects talk through it.
- **Interaction:** E / D-pad up / touch contextual button on the nearest usable thing in front. Samples by the
  spawn: Save Crystal (save + heal), notice board (Codex text), rest bench (sleep until 08:00).
- **Day / night + weather (graybox):** the sun follows the clock, light dims at night, fog by weather.
- HUD: day, time, weather (top right), interaction prompt. Cheats F8 (+1 h), F9 (next weather).

## M3 — Stats & skills (branch `m3-stats-skills`) — done 2026-11-24

### Done
- **Stats** (unit tested against the Balancing sheet): XP curve `round(100 × L^1.8)`, 5 stat points + 1 skill point
  per level, STR/AGI/INT/VIT/SEN → HP, MP, ATK, DEF, crit, crit damage, attack speed, move speed, CDR, MP regen.
  Starter weapon = the sheet's average weapon for the level (items come in M6).
- **Enemy scaling** by level and tier (normal / elite / boss) from a reference hero; enemies give XP on death
  (more while the combo flame is hot). Ash Hound is level 2 (~5 kills to reach level 2).
- **Paladin** (id `vanguard`, data from Godot): 6 actives + ultimate + 3 passives, unlocking at levels 1–10,
  3 skill combos. `AETHVIRSE > Build Class Data`.
- **Skill caster:** keys / gamepad / touch, MP, cooldowns with CDR, skill combos, buffs; damaging skills run
  through the attack state (they cut normal attacks short and come out of a late dash).
- **Every skill looks different:** Iron Charge afterimages, Rending Slash blue arc, Flame Edge burning blade +
  fire arc + embers, Earthsplitter shockwave + rock spikes, War Cry roar rings + red aura while the buff lasts,
  Titan's Verdict golden sky pillar + giant ring.
- **HUD:** level + XP, HP, MP, stamina, skill bar with cooldowns; LEVEL UP / skill combo / +XP callouts.
- **Status window (C):** spend stat and skill points, put skills on slots 1–4.
- Test cheats: F6 level up, F7 full HP/MP and no cooldowns.

### How to test
1. Fight the Ash Hounds (arena north-east) → XP; level up → LEVEL UP, 5 stat points (C to spend).
2. Skills 1/2 (Iron Charge, Rending Slash) from the start; F6 a few times for Earthsplitter (Lv 3), War Cry (Lv 5),
   Flame Edge (Lv 8, put it on a slot in C), Titan's Verdict (Lv 10, T).
3. Combos: 1 then 2 quickly = BREAKER!; War Cry then Earthsplitter = FURY QUAKE!.

## M2 — Combat core (branch `m2-combat`) — done 2026-11-24, approved by owner

### Done
- **Combat core** (unit tested): damage formula, 9 elements, combo rank D→SSS, health pool, attack tokens,
  lock-on scoring. Greatsword combo graph as assets (`AETHVIRSE > Build Combat Data`, numbers from Godot).
- **Player attacks:** 5-hit light chain + 5 heavy branches, lunge, steering while attacking, cancels into
  dodge / jump / next attack, camera-forward aim with soft assist (28°, 9 m), hit-stop, damage numbers,
  heavy-hit camera shake (Cinemachine impulse), perfect dodge → slow motion + guaranteed crit counter.
- **Lock-on:** best target near the view, switch with mouse wheel / stick flick, camera frames player + target.
- **Training dummies** (3, next to the spawn): immortal, wobble + flash, last hit and DPS.
- **Ash Hound** (3 in the arena north-east, 6 s respawn): patrol → chase → windup with a red ground
  telegraph → bite with lunge → cooldown; max 2 attackers at once; staggered by enough poise damage;
  leash back home (heals); dies and respawns. HP bar after the first hit.
- **Player health:** HP 300, hit reactions (stagger ≥ 15 interrupts unless in a heavy attack; dash breaks
  out of it), death → respawn at the spawn after 2 s, fall damage 10%.
- **HUD (graybox):** HP and stamina bars, lock-on target name, PERFECT DODGE callout.
- **Combo flame** (owner request, replaces D…SSS): Spark → Ember → Flame → Blaze → Inferno → Phoenix, the flame
  grows and recolours with the hit count. Each attack in the chain swings differently.

### How to test
1. Dummies: J/K combos (`L L L L L`, `L L H` …), watch DPS and the combo counter; Tab to lock on.
2. Arena (north-east, beyond the pillars): fight 3 Ash Hounds. Dash just as the red circle fills → PERFECT DODGE.
3. Let them hit you: stagger, dash out of it; die → back at the spawn.

## M1 — Core movement (branch `m1-core-movement`) — done 2026-11-24, approved by owner (PC + phone)

### Done
- **Player** (`PlayerController`, CharacterController): camera-relative walk / run / sprint with eased
  acceleration, buffered + coyote jump with variable height (release early = lower), dash with
  i-frames and stamina (one air dash per jump; late part cancels into jump / dash), fall recovery
  to the last safe ground. Same tuning numbers as Godot (`Assets/Data/Player/DefaultMovement.asset`).
- **Camera** (`PlayerCameraRig` + Cinemachine OrbitalFollow / RotationComposer / Deoccluder): orbit with
  mouse or right stick, wall collision, wider FOV while sprinting.
- **Playground scene** (built by `AETHVIRSE > Build Playground Scene`): steps 0.2–2.5 m, ramps 20° / 40° /
  55°, jump gaps 2 / 4 / 6 m, wall + pillars + low tunnel for the camera, an 8 m tower with a ramp.
  Boot loads it automatically.
- F3 overlay: FPS, state, speed, stamina, position (dark backing, readable over the sky).
- Camera without a mouse: arrow keys (eased) and drag-to-look; CI also builds native Linux.
- 19 EditMode tests (movement math, stamina).

### How to test
On the Kali laptop: `tools/play.sh` (or menu "AETHVIRSE (Unity)") downloads the newest successful CI
build for Linux and starts it — nothing is built locally. `tools/play.sh m1-core-movement` picks a branch.
Windows: the run's Artifacts. Note: under Wine the captured mouse sends no movement on Hyprland — use the
native Linux build.
1. Click to capture the mouse, or hold a touchpad button and drag, or use the arrow keys. Walk/run/sprint around; push the stick lightly on a gamepad to walk.
2. Steps: 0.2 / 0.35 m walk up, 0.5+ need a jump; 1.5 m needs a full jump, 2.5 m is out of reach.
3. Ramps: 20° and 40° walkable, 55° not.
4. Gaps: 2 m run-jump, 4 m sprint-jump, 6 m sprint-jump + air dash (Ctrl in the air).
5. Camera: back into the wall / pillars / tunnel — the camera must not go through walls.
6. Fall recovery: walk off the edge of the 120 m ground; you land back on the last safe spot.
7. Climb the tower ramp and jump down 8 m (no fall damage yet; health comes in M2).

- **Touch controls** (phones, or `-touch-ui` on PC): floating joystick on the left 45%, camera drag on the
  rest, Jump and Dash buttons (holding Dash after the dash keeps sprinting), safe-area aware. They feed a
  virtual gamepad, so gameplay code has no touch special cases.

- Phones are landscape only (left or right side up).

### Not yet / next
- Hero model + animations (the body is a capsule with a visor that shows facing).
- Camera options (sensitivity, invert Y, distance) are fields on `PlayerCameraRig`; the Settings menu comes later.
- Camera shake and lock-on come with combat (M2).
