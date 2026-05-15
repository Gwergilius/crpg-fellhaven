[adr-003]: 003-graph-based-dungeon.md "World Data Model"
[adr-005]: 005-lua-scripting.md "Lua Scripting for Conditions and Actions"

# ADR-004: Condition Vocabulary and Combat System

**Status**: Accepted

**Date**: 2026-05-15

## Context

[ADR-003][adr-003] established that edges, nodes, and TraverseEntry chains all accept
a `Condition` object. That ADR deferred the definition of what a condition *is* — its
types, formula syntax, quantifiers, and logical connectives — to a dedicated ADR.

The same decision applies to the combat system. Combat resolution requires:

- Dice rolls with stat modifiers — structurally identical to the `stat_check`
  anticipated in ADR-003
- Quantifiers over combatants: a berserker attacks *all* enemies; a priest provides
  a defense bonus to *all* allies if *any* cleric is present
- Logical connectives: a special attack fires *if* a condition holds, else a fallback
  applies — the same `and`/`or`/`not` composition used in world conditions

Since the condition system and the combat resolution kernel are the **same machinery**
operating on different subjects, they are defined together rather than in two separate
ADRs that would inevitably duplicate the formula syntax, quantifier rules, and
connective semantics.

## Decision

Define a **unified Condition Vocabulary** and **Combat Resolution System** that share:

- A single formula evaluation engine with `{STAT}` references and dice terms
- A single set of logical connectives (`and`, `or`, `not`)
- A single subject/quantifier syntax, extended with combat-context subjects
- A single `EvaluateCondition(condition, context)` entry point in the engine

### Subject syntax

The subject of a condition identifies **who** is being tested. All condition types
use the same discriminated string syntax.

**World / party context** (valid everywhere):

| Subject | Meaning |
|---|---|
| `"party"` | Whole party — for stat checks, uses the best value among all members |
| `"party.any"` | At least one party member satisfies the check |
| `"party.all"` | All party members satisfy the check |
| `"party.member:<n>"` | Specific party slot, 0-indexed |
| `"monster:<id>"` | A specific named monster instance |
| `"npc:<id>"` | A specific NPC entity |
| `"world"` | No stat subject; formula uses only constants and dice |

**Combat context** (valid only during an encounter):

| Subject | Meaning |
|---|---|
| `"self"` | The acting Companion (or NPC combatant) — the one whose turn is currently resolving |
| `"ally"` | Everyone fighting on the player's side in this encounter: the `party` **plus** any combat-capable NPCs present at the node who join the fight (e.g. city guards fighting a dragon). Best value for stat checks. |
| `"ally.any"` | At least one combatant on the player's side satisfies the check |
| `"ally.all"` | All combatants on the player's side satisfy the check |
| `"ally:<slot>"` | Specific combatant on the player's side by combat slot index |
| `"enemy"` | The opposing group (whole group, or current single target) |
| `"enemy.any"` | At least one enemy satisfies the check |
| `"enemy.all"` | All enemies satisfy the check |
| `"enemy:<slot>"` | Specific enemy by combat slot index |

> **`party` vs `ally` in combat context**: `party` always refers strictly to the
> player's own Companion group (up to 6 members). `ally` is a broader combat-time concept:
> `party` ∪ NPC combatants who are fighting on the player's side in the current
> encounter. Outside of combat, `ally.*` subjects are not meaningful; use `party.*`.

#### The player's Companion and the `__death__` trigger

There is no special "player character" subject. `"self"` during the player's turn
refers to whichever Companion the player is currently acting with — just as `"self"`
during an NPC combatant's turn refers to that NPC.

The player's lead Companion is a **full Companion instance** with one distinguishing
property: an `on_death` script that navigates to `__death__` (Game Over). Every other
Companion's death simply removes them from the active party; the remaining members can
continue the encounter, and a new Companion may be recruited later. This behaviour is
implemented as a **script assignment**, not a special engine rule:

```yaml
id: companion_hero
class: knight
on_death: game/player_death
```

The script `game/player_death` navigates to `__death__`. Any Companion can be given an
`on_death` script; the engine treats them identically.

#### Companion state-change triggers

Companion state changes (HP, mana, status effects) can fire scripts. This is the same
mechanism as node `on_enter` and edge `on_traverse` — a named script reference
evaluated by the Lua sandbox. Examples:

| Trigger | `on_*` hook | Typical use |
|---|---|---|
| HP reaches 0 | `on_death` | Navigate to `__death__` (lead), or remove from party |
| HP drops below threshold | `on_low_hp` | Companion flees, morale break, special dialogue |
| Spell cast with low skill | `on_spell_backfire` | Damage redirected to caster or whole party |
| Status effect applied | `on_status_applied` | Chain reactions, immunity checks, visual feedback |

The `on_spell_backfire` case is the canonical example of Companion state-change
triggering a script: a formula-based chance check fires when a Companion casts a spell
with insufficient skill, and the `effect` targets `"self"` or `"ally.all"` rather than
the intended enemy.

Combat-context subjects are a **superset** of world-context subjects. Every world
condition is valid inside a combat encounter; the converse does not hold.

### Dice formula syntax

A formula is an arithmetic expression composed of any combination of the following
terms, joined by `+` and `-` operators, in any order:

| Term | Syntax | Examples | Meaning |
|---|---|---|---|
| Dice | `<count>d<sides>` | `2d10`, `3d6` | Roll `count` dice with `sides` faces, sum results |
| Integer literal | plain integer | `18`, `-2`, `0` | Constant value |
| Stat reference | `{STAT_NAME}` | `{DEX}`, `{MIGHT}`, `{AC}` | Substituted with the subject's stat value before evaluation |

Examples: `"2d10+{DEX}+1"`, `"{MIGHT}"`, `"3d5-2"`, `"18"`, `"1d20+{ATK}"`.

The engine evaluates the formula at check time; results are not stored. A formula
with no dice terms produces the same value every evaluation (deterministic).

Stat names are defined by the character data model. The set of valid stat names is
open: any stat registered in the character schema is a valid `{STAT}` reference.

### Tier 1 — Built-in condition vocabulary

#### Leaf conditions

| Condition type | Valid subjects | Parameters | Example use |
|---|---|---|---|
| `has_item` | `party`, `party.any`, `party.all`, `party.member:<n>` | `item_id`, `quantity?` | Key required to open a door |
| `has_gold` | `party` | `amount`, `op` (`>=`/`==`) | Toll gate |
| `has_class` | `party`, `party.any`, `party.all`, `party.member:<n>`, `ally`, `ally.any`, `ally.all` | `class` | Priest aura requires a cleric in the party |
| `stat` | `party`, `party.any`, `party.all`, `party.member:<n>`, `self`, `ally.*`, `enemy.*` | `stat`, `op`, `value` | Deterministic threshold: Strength ≥ 15 |
| `stat_check` | same as `stat` | `formula`, `op`, `target_formula`, `target_subject?` | Dice-based skill check or opposed roll |
| `has_flag` | `world` | `flag_id`, `value` | Gate depends on a world flag |
| `monster_defeated` | `monster:<id>` | — | Boss dead before exit opens |
| `npc_alive` | `npc:<id>` | — | NPC must be present |

#### `stat_check` — dice-based check

`stat_check` evaluates a single **opposed formula**:

```
eval(formula, subject) op eval(target_formula, target_subject)
```

Both sides are formula strings evaluated against a subject. Stats are referenced
inside the formula using `{STAT_NAME}` placeholders. There are no separate `stat`,
`bonus`, or `target_stat` fields — all arithmetic lives in the formula itself.

Parameters:

| Parameter | Type | Description |
|---|---|---|
| `formula` | string | Formula evaluated against the subject, e.g. `"2d10+{DEX}+1"`, `"{MIGHT}"` |
| `op` | string | Comparison operator: `>=`, `>`, `==`, `<=`, `<` (default `>=`) |
| `target_formula` | string | Formula evaluated against the target side, e.g. `"3d5"`, `"18"`, `"{AC}"`. **Required.** |
| `target_subject` | string? | Whose stats resolve `{STAT}` references in `target_formula`. Omit when `target_formula` contains no `{STAT}` references (`"world"` is the implicit default). |

The `subject` field governs both **quantification** (`party.any`, `ally.all`) and
**stat resolution**: `{DEX}` in a formula refers to the DEX of whichever member is
being tested under the current quantifier.

```yaml
# Party's best DEX + 2d10 + 1  vs.  static threshold 18
type: stat_check
subject: party
formula: 2d10+{DEX}+1
op: ">="
target_formula: "18"

# Fully opposed: party member 0's DEX + 1d6  vs.  lock's random resistance
type: stat_check
subject: party.member:0
formula: 1d6+{DEX}
op: ">="
target_formula: 3d5-2

# Obstacle rolls actively; subject contributes stat only (no dice on subject side)
type: stat_check
subject: party
formula: "{DEX}"
op: ">="
target_formula: 3d5

# Combat: attacker's attack roll vs. defender's AC
type: stat_check
subject: self
formula: 1d20+{ATK}
op: ">="
target_formula: "{AC}"
target_subject: enemy
```

#### Composite conditions — logical connectives

The three logical connectives — **`and`**, **`or`**, and **`not`** — are first-class
built-in condition types. They compose any other conditions (including other
composites) into a **condition tree** that the engine evaluates recursively. No Lua
script is needed for boolean combinations of leaf conditions.

| Type | Semantics | Field(s) |
|---|---|---|
| `and` | All sub-conditions must pass (short-circuits at first `false`) | `conditions: Condition[]` |
| `or` | At least one sub-condition must pass (short-circuits at first `true`) | `conditions: Condition[]` |
| `not` | The sub-condition must fail (negation) | `condition: Condition` |

Order matters for `and` and `or` when sub-conditions can have side effects —
built-in leaf conditions do not, but a Lua `script` condition may.

Composite conditions are **fully transparent to static analysis**: the analyser
recursively descends the condition tree without executing any code.

```yaml
# OR — key in inventory OR strong enough to force the door
type: or
conditions:
  - type: has_item
    subject: party
    item_id: iron_key
  - type: stat
    subject: party
    stat: might
    op: ">="
    value: 18

# NOT — gate is open only while the cave troll is still alive
type: not
condition:
  type: monster_defeated
  subject: monster:cave_troll

# AND + OR nested: (has key OR strong enough) AND the quest is active
type: and
conditions:
  - type: or
    conditions:
      - type: has_item
        subject: party
        item_id: iron_key
      - type: stat
        subject: party
        stat: might
        op: ">="
        value: 18
  - type: has_flag
    subject: world
    flag_id: quest_iron_door
    value: true
```

The `subject` axis and the composite axis are **orthogonal dimensions of quantification**:

| Axis | Syntax | Quantifies over |
|---|---|---|
| Subject (`party.all`, `ally.any`, …) | within a leaf condition | Members of a homogeneous group |
| Composite (`and` / `or`) | `conditions: [...]` | A heterogeneous set of conditions of any type |

The two axes combine freely. Example: *all party members must carry armour, AND the
magic gate flag is set*:

```yaml
type: and
conditions:
  - type: has_item
    subject: party.all
    item_id: armor
  - type: has_flag
    subject: world
    flag_id: magic_gate_open
    value: true
```

### Tier 2 — Lua script condition

For conditions requiring **stateful logic, random chance, or external lookups** not
expressible as a condition tree:

```yaml
type: script
package: conditions/riddle_gate
fail_message_key: door.wrong_answer
```

The script returns `true` (allow) or `false` (block). The engine handles messaging
for refusal via the containing `condition` or `TraverseEntry`'s `fail_message_key` —
the script itself does not need to produce text.

---

## Combat Resolution

### Overview

Fellhaven combat is **turn-based and round-based**:

1. **Encounter start** — an encounter table (or scripted trigger) instantiates an
   enemy group. The party is present as a group of up to 6 combatants.
2. **Initiative** — formula-based roll per combatant determines action order.
3. **Action phase** — each combatant, in initiative order, selects and executes one
   action. Actions may target a single enemy, a subset, or the entire opposing group.
4. **End-of-round effects** — ongoing effects (poison, regeneration, auras) resolved
   in declaration order.
5. **Victory / defeat check** — all enemies dead → encounter ends, rewards applied;
   all party members dead → `__death__` node reached.

### Combatants and stats

Both party members and monsters are **combatants** during an encounter. Each combatant
has a stat block compatible with the formula engine (`HP`, `AC`, `ATK`, `DEX`,
`MIGHT`, etc.). Monster stat blocks are defined in the monster database (to be defined
in a future Monster ADR). The formula engine makes no distinction between a party member's
stat block and a monster's — `{AC}` resolves correctly for both.

### Combat actions

A combat action is a data object:

```
CombatAction {
    id:                 string          // unique identifier for this action type
    condition:          Condition?      // when this action is available (full vocabulary)
    target:             string          // TargetSpec — who is affected (quantifier)
    hit_formula:        string?         // attacker's roll formula (subject = self)
    hit_target_formula: string?         // defender's roll formula (subject = target)
    hit_op:             string?         // comparison: hit_formula op hit_target_formula (default >=)
    effect:             EffectSpec[]    // applied on a hit
    fail_effect:        EffectSpec[]?   // applied on a miss (optional)
    on_execute:         ScriptRef?      // Lua script for complex multi-step actions
}
```

`target` uses the same subject quantifier syntax: `"enemy"` (single target),
`"enemy.all"` (all enemies), `"ally.all"` (all allies), `"self"`, etc.

The `condition` field uses the full condition vocabulary defined above, including
composite connectives and combat-context subjects. The engine does not distinguish
between a "world condition" and a "combat condition" — there is one vocabulary.

### Quantifiers in combat — examples

**Berserker cleave — attacks all enemies simultaneously:**

```yaml
id: berserker_cleave
condition: null
target: enemy.all
hit_formula: 1d20+{ATK}
hit_target_formula: "{AC}"
hit_op: ">="
effect:
  - type: damage
    formula: 1d8+{MIGHT}
```

The engine iterates over every living enemy; the hit check and damage formula are
evaluated independently for each. No special-case "cleave logic" exists in the engine —
`"enemy.all"` as the target spec causes the iteration. This is the existential/universal
quantification principle applied to action targets.

**Cleric defense aura — passive damage reduction active whenever a cleric is present:**

```yaml
id: cleric_aura
trigger: on_damage_received
condition:
  type: has_class
  subject: ally.any
  class: cleric
target: self
effect:
  - type: damage_reduction
    formula: "2"
```

The condition uses `"ally.any"` (existential quantifier over allies): the aura fires
if at least one ally is a cleric. If no cleric is present, the condition fails and the
aura does not apply. The engine evaluates this using `EvaluateCondition` — the same
function used for world edge gates.

### Resolution kernel

The formula evaluation engine is **not duplicated for combat**. The same
`EvaluateFormula(formula, subject, context)` function evaluates all formulas everywhere
in the system: world conditions, combat hit checks, damage rolls, and initiative rolls.

```
hit    = EvaluateFormula(hit_formula,        self,   ctx)
         op
         EvaluateFormula(hit_target_formula, target, ctx)

damage = EvaluateFormula(damage_formula, self, ctx)
```

This is structurally identical to `stat_check`. Combat adds only:
- The concept of rounds and initiative order
- The target iteration loop (for `enemy.all` / `ally.all` targets)
- Damage application and HP bookkeeping

## Consequences

### Positive
- One formula evaluator and one condition evaluator — used everywhere, tested once
- Combat behaviours are fully data-driven: actions, conditions, and targets are YAML
  objects; a new monster ability or party effect is a data change, not a code change
- Static analysis can inspect combat conditions using the same tools as world conditions
- The `has_class` + `ally.any` pattern captures party-composition effects without any
  special-case engine logic
- The berserker cleave and single-target attack are the same code path, parameterised
  by the target spec

### Negative
- The full condition vocabulary must be agreed before any combat implementation starts;
  changes to the formula syntax are a breaking schema change
- The engine must correctly propagate the current subject context through quantifier
  iteration — a bug in quantifier resolution affects both world conditions and combat
- Monster stat schema (needed for `{ATK}`, `{AC}`, etc.) depends on a future Monster ADR;
  `stat_check` and `CombatAction` cannot be fully validated until that ADR is written

### Neutral
- A future Monster ADR will define the full monster stat block; this ADR defines how
  formulas reference those stats via `{STAT}` placeholders
- A future NPC Combat ADR may add NPC-combat-specific subjects if hostile NPCs are implemented

## Alternatives Considered

1. **Separate condition system for combat**: Define a parallel condition/formula syntax
   specific to combat. Rejected — duplication without benefit; any fix or extension
   must be applied twice.

2. **Keep conditions as simple booleans, delegate dice logic to Lua entirely**: Simpler
   condition model, but pushes all probabilistic logic into Lua, making static analysis
   impossible for common cases. Rejected — the formula system is not significantly more
   complex than a boolean model, and the payoff (modding, static analysis) is high.

3. **Use an existing expression language (e.g. Godot Expression)**: Would cover
   formula evaluation but not the subject/quantifier system. Rejected — the quantifier
   is the essential new concept; a third-party library would still require a custom
   wrapper for stat resolution and quantification, adding a dependency without covering
   the full problem.

## Related ADRs

- [ADR-003][adr-003]: World Data Model — defines where `Condition` objects appear
  (edge gates, TraverseEntry selectors, node `on_enter` selectors)
- [ADR-005][adr-005]: Lua Scripting — Lua script evaluation used for Tier 2 `script` conditions
  and complex combat actions

## References

- Inspired by MM1-Remaster ADR-011 Condition Vocabulary and Combat System
- Dice notation: https://en.wikipedia.org/wiki/Dice_notation
- Boolean logic: https://en.wikipedia.org/wiki/Boolean_algebra

---

**Owner**: Fellhaven Team  
**Last Updated**: 2026-05-15
