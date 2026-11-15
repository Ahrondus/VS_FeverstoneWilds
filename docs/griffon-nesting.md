# Configurable egg nesting

This ports the tamed Utahraptor construction/laying/brooding sequence from the
provided PaleoWilds source into Feverstone Wilds without a dependency on PaleoWilds.

## Griffon behavior

- Only `griffon-tamed-female` receives `fsweggreproduction` and `fswnesting`.
- Twelve portions and a living tamed male within ten blocks reserve one clutch
  of three fertile eggs. Food alone permits building but never fertile laying.
- As with vanilla MultiplyBase, a newly initialized female has a randomized
  initial breeding cooldown. The configured cooldown is five to twelve game days.
- A female builds a nearby nest in two stages, taking a four-to-six block material
  searching excursion and a three-second construction action for each stage.
- At most three compatible nests, including unfinished/full nests, are built in
  the local sixteen-block area. A removed nest can be replaced.
- She prefers her own nest, then a compatible communal nest. Each egg takes five
  real seconds to lay. If construction/nest space is unavailable, she lays the
  canonical collectible egg entities on the ground.
- Pending clutch size is saved after each successfully laid egg. Interrupting or
  reloading cannot restart a fresh three-egg clutch from the same mating.
- A full nest incubates for twelve game days of actual nearby brooding. Time away,
  expired claims, another floor/dimension, and unloaded time do not advance it.
- Inventory and incubation belong to the nest, so another female can resume the
  same clutch. A claim prevents simultaneous construction, laying or brooding.
- Vanilla egg inventory handling renders one, two or three eggs and preserves
  chick type, generation and herd data when hatching.
- Loose eggs progress through the existing three visual stages over twelve game
  days, support collection, and become the existing broken-egg entity when hit.
  Stage replacements preserve the original incubation timestamp and chick data.
- The ordinary flight system lands the female and pauses automatic takeoff while
  nesting. Riding and higher-priority combat/owner tasks can interrupt nesting.

The adult model has no sit, search or dig animations. The defaults use `walk`
for searching, `eat` for construction and `idle` for brooding. Set the corresponding
animation names when species-specific animations become available.

Ostrich and cockatrice entity definitions and `seekblockandlay` configuration are
unchanged. Their large nest now derives from the current `BlockHenbox` and
`BlockEntityHenBox`, restoring current `IAnimalNest` signatures, inventory-based
egg collection and incubation. The existing `largenest.json` asset is retained. Both core registrations are enabled.

This change does not alter wild griffon spawning or the existing wild nest entity.
Collected egg items use the existing `ItemCreature` placement behavior; placing
one starts a new loose-egg incubation, matching the reference's collect/place cycle.

## Adding another creature

1. Add the `fsweggreproduction` server behavior to the intended adult female
   variants. Use the griffon's behavior as the complete JSON example. Set
   `portionsEatenForMultiply`, cooldowns and all values inside `nesting`.
2. Add a `fswnesting` movement-slot AI task. Choose a priority below any combat
   or owner orders that should interrupt it. Other entity variants need neither
   the behavior nor the task. Do not also enable live-birth `multiply` for these
   variants.
3. Supply a unique `nestType`, a ready block and ordered `constructionBlocks`.
   Blocks use class/entityClass `FswBreedingNest`. Every variant must have the
   same inventory size, `inventoryClassName` and `displayTransforms`. The ready
   variant has `constructionStage: 0`; construction variants use positive stages.
   Keep the nest's `quantitySlots` equal to `clutchSize`.
4. Supply fully qualified `eggItem`, `eggEntity`, `mateCode`, and `chickCodes`.
   Asset validation disables breeding and logs the species if required assets
   are missing. Nest construction itself uses no griffon-specific identifiers.
5. Add `fswegg` to all loose egg stages, replacing `grow` and other incubation or
   pickup behaviors. Set the same `eggItem`, `brokenEntity`, ordered
   `stageEntities`, fallback `chickCodes`, and `incubationDays` on every stage.
   Retain the broken entity's cleanup behavior. The number of visual stages is
   independent of clutch size.
6. Add models, textures, item/entity definitions and language entries for that
   creature. Add a separate nest family rather than sharing an incompatible
   species' ready block.

## Validation

With `VINTAGE_STORY` pointing at the game install:

```powershell
dotnet build feverstonewilds/feverstonewilds.csproj
dotnet run --project tests/NestingChecks
```

The test executable uses the installed game implementation with minimal world
stubs for clutch accounting, claims, incubation, serialization and hatching, plus
checks of species activation and existing poultry tasks. It needs no test package.

An in-game smoke test is still required: use a test world, feed a male/female
pair, allow the initial breeding cooldown, observe both construction stages and
three eggs, interrupt/resume laying, leave/reload while brooding, collect eggs,
test an area at the three-nest cap, and verify an ostrich and cockatrice still
lay and hatch in their large nest. Inspect server and client logs for asset or
animation errors. A successful build is not proof of in-game pathfinding or mesh
placement.
