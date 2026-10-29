# Worldtree Legacy — Game Design Bible

Source: [Game Design Bible](https://app.notion.com/p/40837083c6b68161ac4ad7011fa9b317)
Source last edited (UTC): 2026-10-29T08:42:29.476Z
Exported: 2026-10-30 (Australia/Sydney)

The content below is a faithful Markdown export of the existing Notion page. Source estimates, illustrative values and unresolved decisions remain unchanged.

## Vision

Original 2.5D fantasy side-scrolling action RPG with Modern MapleStory Heroic-inspired progression. Windows PC; Unity 6 LTS and Blender; offline single-player first, 2–4 player cooperative bossing later. Gameplay-earned power; no paid enhancement.

## Core loop

Grind monsters → earn Lumen and XP → improve gear and skills → learn and defeat boss → collect boss materials and set equipment → unlock the next region and boss tier.

## World

The colossal Worldtree Aelthara connects mortal roots to celestial realms. The Withering corrupts its guardians. Legacy Bearers ascend and restore its branches.

| Region | Level band | Theme |
| --- | --- | --- |
| Rootbound Haven | 1–40 | Villages and beginner forests |
| Hollowroot Depths | 40–90 | Corrupted roots and ruins |
| Verdant Spire | 90–140 | Trunk settlements |
| Celestial Boughs | 140–200 | Floating branches |
| Withered Crown | 200–250 | Distorted endgame |
| Origin of Eternity | 250–300 | Cosmic pinnacle |

## Classes

Ironwarden: melee defense and 120-second burst. Starweaver: elemental burst. Duskblade: mobile sustained DPS. Thornstrider: ranged traps and summons. Dawnfist: combo martial artist. Soulbloom: spirit summons and support.

Unlocks at levels 1, 30, 70, 120, 180, 220 and 260. Prototype Ironwarden first.

## Combat

Fixed XY plane with 3D visual assets and orthographic camera. Movement, jump, double jump, platform drop, configurable skill keys. Multi-hit attacks, critical damage, defense penetration, boss damage, burst cycles, defensive i-frames, super armor and controlled animation cancels. Core gameplay logic must be independent of animation events.

## Boss design

Normal, Hard, Extreme, Mythic difficulties; telegraphs, phase machines, death counts, timers, practice mode, weekly rewards. Example bosses: Thornroot Sentinel, Crystalline Matriarch, Ashen Castellan, Seraphine, Aurelios, Nyxara, Eryndor.

Difficulty defaults: Normal 10 deaths / 30 min; Hard 5 / 30; Extreme 3 / 25; Mythic 1 / 20. Tune per boss.

## Gear

Slots: weapon, secondary, helmet, chest, gloves, boots, cape, belt, pendant, four rings, earrings, emblem, shoulder, relic.

Tiers: Common (+5), Rare (+10), Epic (+15), Legendary (+20), Ancient (+25), Mythic (+30), Transcendent (+30 and awakening).

Forging uses Lumen and materials, with protection and guaranteed-progress resonance. Echo Imprints reroll secondary stats. Soul Awakening alters late-game skill effects. Rates are illustrative until simulated and balanced.

## Endgame

Legacy Constellation (180+), Ancestral Runes (200+), Legacy Ascension (220+), Origin Mastery (260+), Transcendence (280+). Weekly bossing plus unlimited practice and optional min-maxing. Deterministic fragments prevent indefinite bad-luck blocks.

## Economy

Lumen, Forge Embers, Echo Crystals, Ancestral Sap, Legacy Fragments, Origin Shards and Eternal Seeds. No pay-to-win. Keep weekly commitments reasonable.

## Prototype scope

One class (Ironwarden), one region (Rootbound Haven), levels 1–30, one boss (Thornroot Sentinel), two equipment sets, offline saves. Target a complete grind → upgrade → boss loop before scaling content.
