# Griffon nesting

Tamed female griffons use vanilla `AiTaskSeekBlockAndLay` through a small flight-aware subclass. Nest seeking, laying, egg fertility data, brooding, and incubation use the installed vanilla implementation. The subclass requests landing for an available nest, prevents laying while mounted, and retains vanilla ground-laying fallback. Griffons never construct nests.

## Domestic nest

`feverstonewilds:griffonnest-ready` uses vanilla `BlockHenbox` and `HenBox`, with three egg slots and the domestic empty-nest model. Only the ready variant is generated. Breaking the empty nest returns the domestic nest block. Wild griffon nest entities remain separate.

The grid recipe is `SFS,SSS`, width 3, height 2 (bottom crafting row empty). Each S takes three sticks; F takes thirty feathers. Total: fifteen sticks and thirty feathers for one domestic nest.

## Laying and incubation

Only tamed adult females receive vanilla `multiplybase` feeding and the `griffonseeknestandlay` task. Vanilla laying consumes food; a living tamed male within ten blocks allows fertile eggs. Griffon `eggTypes` selects the griffon egg item, and hatchlings are tamed griffon cubs. The nest type is `feverstonewilds:griffon`. Laying takes five seconds, incubation is twelve game days, and the vanilla sitting cycle is 0.9 days with one-to-two-hour task cooldowns. No custom three-egg clutch reservation or construction task runs on griffons.

Loose griffon eggs require twenty game days of heat to hatch. Active vanilla heat sources within three blocks on each axis count, including fluid heat sources. Incubation pauses when cold, picked up, or unloaded; accumulated progress survives pickup and hatching-stage changes. Cold eggs show “Needs heat to hatch.” in their hover panel. Nest incubation remains twelve days of brooding. Ostrich and cockatrice definitions are unchanged.

## Validation

Build and run `tests/NestingChecks` with `VINTAGE_STORY` set to the installed game directory. In-game: craft and place the domestic nest, feed a tamed female, keep a tamed male nearby, then observe laying and brooding. Verify that no construction blocks appear, and that eggs hatch into tamed cubs. Build checks cannot establish in-game navigation or placement.

The older reusable custom construction classes remain available for other creatures, but griffons no longer reference them.

## Growth stages

Cub -> juvenile -> adolescent -> adult. Each transition takes 216 in-game hours (nine days), for 27 days from hatching to adulthood. Feeding never accelerates these transitions: no fed-adult growth targets are configured. Juveniles use the cub shape at 1.45 scale; adolescents use griffon-adolescent at 0.8 scale. Existing gender-neutral cub entity codes remain compatible, with sex selected on entering the juvenile stage and preserved thereafter. Wild/tamed status is retained at every transition. Non-adult stages have no breeding or riding tasks.

Visual size grows gradually over each nine-day stage: cub scale 1.0–1.45, juvenile scale 1.45–3.390692, and adolescent scale 0.8–1.0 (males) or 0.8–0.95 (females) on its separate model. Stage age is saved by vanilla grow data; no feeding acceleration is added.

Young griffons apply size explicitly on the client using saved stage start time. Juvenile end scale matches the adolescent model's starting rest-pose height (cub shape height 7.964001 model units; adolescent height 33.754357 at scale 0.8). Different body proportions still change when the model switches.

## One mother per clutch

Domestic griffon nests retain vanilla HenBox inventory and incubation, with a persistent GriffonNestClutch block-entity behavior. The first successful egg records its mother's entity ID. Other females cannot use that nest until every egg is gone. The same mother can finish filling three slots; once full, or once any eggs are removed or hatch, additions stop for the remainder of that clutch. Brooding and leaving do not release the lock. Empty nests reset it. Existing eggs without mother metadata are sealed against additions and assigned to the first brooding female; their contents and incubation progress are preserved. Remaining infertile eggs must be collected before the nest becomes available.

## Ground fallback

Fed females use vanilla's 30% ground-laying fallback when no available nest is found, including nests locked to another clutch. Vanilla also controls delayed fallback after unsuccessful seeking. The configured fallback block is egg-griffon-1, collected as one infertile griffon food egg; it expires after 72 game hours like vanilla ground chicken eggs. Ground-laid blocks do not incubate in place. Player-placed egg entities retain their existing heat requirement and twenty-day incubation.

Ground fallback eggs drop egg-griffon-infertile, a separate ordinary food item stacking to 32, providing 200 protein satiety and spoiling after 120 fresh hours plus 24 transition hours. They have no entity placement or incubation behavior. Nest eggs remain creature-griffon-egg with individual saved incubation progress.
