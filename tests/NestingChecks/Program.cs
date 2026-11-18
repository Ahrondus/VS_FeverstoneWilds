using System.Reflection;
using System.Text.Json.Nodes;
using FeverstoneWilds;
using FeverstoneWilds.Nesting;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    passed++;
}
Check(typeof(IAnimalNest).IsAssignableFrom(typeof(BlockEntityAnimalNestLarge)), "large nest implements installed interface");
Check(typeof(BlockHenbox).IsAssignableFrom(typeof(BlockAnimalNest)), "large nest supports current egg collection");
Check(!NestingRules.CanMate(true, 0, 11, 12, 20, 10), "underfed mother cannot mate");
Check(!NestingRules.CanMate(true, 1, 12, 12, 20, 10), "unfinished clutch cannot be reserved again");
Check(!NestingRules.CanMate(true, 0, 12, 12, 5, 10), "breeding cooldown respected");
Check(NestingRules.RemainingAfterLay(2, false) == 2, "failed laying preserves pending eggs");
Check(!NestingRules.WithinReach(0, 2, 0, 1.55), "brooding from another floor rejected");

double days = 20;
long milliseconds = 10000;
ICoreAPI? api = null;
IWorldAccessor? world = null;
Item? resolvedItem = null;
var spawned = new List<Entity>();
var calendar = Fake.Make<IGameCalendar>((m, a) => m.Name switch
{
    "get_TotalDays" => days,
    "get_TotalHours" => days * 24,
    _ => Fake.Default(m.ReturnType)
});
var accessor = Fake.Make<IBlockAccessor>((m, a) => Fake.Default(m.ReturnType));
var registry = Fake.Make<IClassRegistryAPI>((m, a) => m.Name == "CreateEntity"
    ? new EntityAgent { World = world!, Code = ((EntityProperties)a[0]!).Code } : Fake.Default(m.ReturnType));
world = Fake.Make<IWorldAccessor>((m, a) => m.Name switch
{
    "get_Side" => EnumAppSide.Server,
    "get_Calendar" => calendar,
    "get_ElapsedMilliseconds" => milliseconds,
    "get_Api" => api,
    "get_BlockAccessor" => accessor,
    "get_Rand" => new Random(1),
    "get_ClassRegistry" => registry,
    "GetItem" => resolvedItem,
    "GetEntityType" => new EntityProperties { Code = (AssetLocation)a[0]! },
    "SpawnEntity" => Capture((Entity)a[0]!),
    _ => Fake.Default(m.ReturnType)
});
api = Fake.Make<ICoreAPI>((m, a) => m.Name switch
{
    "get_Side" => EnumAppSide.Server,
    "get_World" => world,
    "get_ClassRegistry" => registry,
    _ => Fake.Default(m.ReturnType)
});
object? Capture(Entity entity) { spawned.Add(entity); return null; }
EntityAgent Mother(long id)
{
    var mother = new EntityAgent { World = world, EntityId = id, Code = new AssetLocation("test:mother") };
    mother.Pos.SetPos(0.5, 0, 0.5);
    return mother;
}
var mother = Mother(1);
var secondMother = Mother(2);
var block = new BlockAnimalNest { Code = new AssetLocation("test:nest-ready"),
    Attributes = Vintagestory.API.Datastructures.JsonObject.FromJson("{\"constructionStage\":0,\"quantitySlots\":3,\"nestType\":\"test\"}") };
var nest = new TestNest();
nest.Setup(api, block);
Check(nest.Claim(mother), "first female claims nest");
Check(!nest.Claim(secondMother), "second female cannot steal active claim");
milliseconds += 5001;
Check(nest.Claim(secondMother), "expired claim can be recovered");
nest.Release(secondMother);
Check(nest.Claim(mother), "released claim is available");

var item = new Item { Code = new AssetLocation("test:egg"), ItemId = 1 };
resolvedItem = item;
ItemStack Egg()
{
    var egg = new ItemStack(item);
    var chick = new TreeAttribute();
    chick.SetString("code", "test:chick");
    chick.SetInt("generation", 4);
    chick.SetDouble("incubationDays", 12);
    egg.Attributes["chick"] = chick;
    return egg;
}
Check(nest.TryAddEgg(Egg()), "first egg accepted");
nest.Brood(mother);
days += 1;
nest.Tick();
Check(nest.RemainingDays == 12, "partial clutch does not incubate");
Check(nest.TryAddEgg(Egg()) && nest.TryAddEgg(Egg()) && nest.Full, "full three egg clutch accepted");
Check(!nest.TryAddEgg(Egg()), "full nest refuses fourth egg");
nest.Brood(mother);
days += 0.5;
nest.Tick();
Check(Math.Abs(nest.RemainingDays - 11.5) < 0.00001, "present mother advances incubation");
nest.Release(mother);
days += 3;
nest.Tick();
Check(Math.Abs(nest.RemainingDays - 11.5) < 0.00001, "absence pauses incubation");
nest.Brood(secondMother);
days += 0.5;
nest.Tick();
Check(Math.Abs(nest.RemainingDays - 11) < 0.00001, "another mother resumes the same nest progress");
secondMother.Pos.Y = 3;
days += 1;
nest.Tick();
Check(Math.Abs(nest.RemainingDays - 11) < 0.00001, "mother on another floor cannot incubate");
secondMother.Pos.Y = 0;
milliseconds += 5001;
days += 1;
nest.Tick();
Check(Math.Abs(nest.RemainingDays - 11) < 0.00001, "expired claim pauses incubation");

var savedNest = new TreeAttribute();
nest.ToTreeAttributes(savedNest);
var reloadedNest = new TestNest();
reloadedNest.Setup(api, block);
reloadedNest.FromTreeAttributes(savedNest, world);
Check(reloadedNest.CountEggs() == 3 && Math.Abs(reloadedNest.RemainingDays - 11) < 0.00001,
    "nest inventory and incubation survive serialization");
days += 10;
reloadedNest.Tick();
Check(reloadedNest.CountEggs() == 3 && Math.Abs(reloadedNest.RemainingDays - 11) < 0.00001,
    "offline interval is not counted as brooding");
reloadedNest.Brood(mother);
days += 11.1;
reloadedNest.Tick();
Check(spawned.Count == 3 && reloadedNest.CountEggs() == 0, "completed clutch hatches exactly three cubs");
Check(spawned.All(e => e.WatchedAttributes.GetInt("generation") == 4 && e.Attributes.GetString("origin") == "reproduction"),
    "hatchlings retain generation and reproduction origin");

var reproduction = new TestReproduction(mother);
reproduction.Setup();
mother.WatchedAttributes.SetInt(EntityBehaviorFswReproduction.RemainingKey, 3);
reproduction.EggLaid();
Check(reproduction.Remaining == 2, "interrupted laying retains two pending eggs");
var saved = mother.WatchedAttributes.Clone() as ITreeAttribute;
Check(saved!.GetInt(EntityBehaviorFswReproduction.RemainingKey) == 2, "saved clutch records partial progress");
reproduction.EggLaid();
reproduction.EggLaid();
Check(reproduction.Remaining == 0 && reproduction.TotalDaysCooldownUntil >= days + 5,
    "last egg completes clutch and starts cooldown");

string assets = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../feverstonewilds/resources/assets/feverstonewilds"));
if (args.Contains("--nest-guard"))
{
    var guardNest = new EntityAgent { EntityId = 4001, Alive = true, Code = new AssetLocation("feverstonewilds:wild-griffon-nest-eggs") };
    var griffonGuard = new EntityAgent();
    foreach (string code in new[] { "griffon-wild-male", "griffon-wild-female" })
    {
        griffonGuard.Code = new AssetLocation("feverstonewilds", code);
        Check(AiTaskGriffonGuardNest.IsWildAdult(griffonGuard), code + " guards nests");
    }
    foreach (string code in new[] { "griffon-tamed-male", "griffon-tamed-female", "griffon-cub-wild", "griffon-adolescent-wild-male" })
    {
        griffonGuard.Code = new AssetLocation("feverstonewilds", code);
        Check(!AiTaskGriffonGuardNest.IsWildAdult(griffonGuard), code + " does not use wild guarding");
    }
    Check(AiTaskGriffonGuardNest.IsWildNest(guardNest), "living wild guardNest is guardable");
    guardNest.Alive = false;
    Check(!AiTaskGriffonGuardNest.IsWildNest(guardNest), "destroyed guardNest stops guarding");
    guardNest.Alive = true;
    var mode = EnumGameMode.Survival;
    var intruder = new EntityPlayer { EntityId = 4002, Alive = true };
    intruder.Pos.SetPos(0, 0, 11);
    Check(AiTaskGriffonGuardNest.IsIntruder(intruder, guardNest, 12, mode), "survival player approaching guardNest triggers defense");
    intruder.Pos.Z = 13;
    Check(!AiTaskGriffonGuardNest.IsIntruder(intruder, guardNest, 12, mode) && AiTaskGriffonGuardNest.IsIntruder(intruder, guardNest, 30, mode),
        "alert radius and chase leash are distinct");
    intruder.Pos.Z = 31;
    Check(!AiTaskGriffonGuardNest.IsIntruder(intruder, guardNest, 30, mode), "player outside leash loses guard target");
    intruder.Pos.Z = 5;
    mode = EnumGameMode.Creative;
    Check(!AiTaskGriffonGuardNest.IsIntruder(intruder, guardNest, 12, mode), "creative players do not provoke guardNest guards");
    mode = EnumGameMode.Spectator;
    Check(!AiTaskGriffonGuardNest.IsIntruder(intruder, guardNest, 12, mode), "spectators do not provoke guardNest guards");
    Check(!AiTaskGriffonGuardNest.IsIntruder(griffonGuard, guardNest, 12, mode), "other griffons are not intruders");
    var guardSaved = new TreeAttribute(); guardSaved.SetLong("fswGuardedWildNestId", guardNest.EntityId);
    var restored = new TreeAttribute(); restored.FromBytes(guardSaved.ToBytes());
    Check(restored.GetLong("fswGuardedWildNestId") == guardNest.EntityId, "guardNest association persists across saves");
    var config = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/griffon.json")))!;
    var task = config["server"]!["behaviors"]!.AsArray().Single(b => (string?)b?["code"] == "taskai")!["aitasks"]!
        .AsArray().Single(t => (string?)t?["code"] == "griffonguardnest")!;
    Check((bool)task["enabledByType"]!["*-wild-*"]! && !(bool)task["enabledByType"]!["*"]! && (double)task["priority"]! > 3.5,
        "guard task is wild-only and overrides ordinary pursuit to return to guardNest");
    Console.WriteLine("Passed " + passed + " checks including wild griffon guardNest guarding.");
    return;
}
if (args.Contains("--direwolf-bridle"))
{
    var log = Fake.Make<ILogger>((m,a) => Fake.Default(m.ReturnType));
    Shape Load(string path) => Vintagestory.API.Datastructures.JsonObject.FromJson(File.ReadAllText(Path.Combine(assets, path))).AsObject<Shape>()!;
    var wolf = Load("shapes/entity/land/mammal/direwolf-adult.json");
    var bridle = Load("shapes/item/wearable/head/bridle-direwolf.json");
    wolf.InitForAnimations(log, "wolf");
    bridle.ResolveReferences(log, "bridle");
    wolf.StepParentShape(bridle, null, "bridle", "wolf", log, (_,_) => { });
    wolf.InitForAnimations(log, "equipped wolf");
    IEnumerable<ShapeElement> Walk(ShapeElement[] elements) => elements.SelectMany(e => new[] { e }.Concat(Walk(e.Children ?? [])));
    Check(bridle.Elements[0].StepParentName == "NoseMuzzle", "bridle retains authored snout attachment");
    int headJoint = wolf.GetElementByName("Head").JointId;
    Check(headJoint != 0 && Walk(bridle.Elements).All(e => e.JointId == headJoint),
        "entire equipped bridle inherits head animation joint");
    Check(wolf.JointsById[wolf.GetElementByName("Origin").JointId].Element == wolf.GetElementByName("Origin"),
        "body Origin animation still resolves to the direwolf root");
    Check(Walk(bridle.Elements).Select(e => e.Name).Distinct().Count() == Walk(bridle.Elements).Count(),
        "bridle element names are unique");
    foreach (string pose in new[] { "lie", "sleep", "sit", "walk", "run", "die" })
        Check(wolf.Animations.Single(a => a.Code == pose).KeyFrames.SelectMany(k => k.Elements.Keys)
            .All(key => !key.StartsWith("DirewolfBridle_")), "bridle receives no unintended body animation in " + pose);
    Console.WriteLine("Passed " + passed + " checks including direwolf bridle joint binding against installed VS API.");
    return;
}
var griffon = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/griffon.json")))!;
if (args.Contains("--mounted-attacks"))
{
    var testWorld = Fake.Make<IWorldAccessor>((m,a) => Fake.Default(m.ReturnType));
    var mount = new EntityAgent { World = testWorld, EntityId = 3001, Alive = true };
    var rider = new EntityPlayer { World = testWorld, EntityId = 3002, Alive = true };
    bool driver = true;
    var seat = Fake.Make<IMountableSeat>((m,a) => m.Name switch {
        "get_Entity" => mount,
        "get_CanControl" => driver,
        _ => Fake.Default(m.ReturnType)
    });
    typeof(EntityAgent).GetProperty("MountedOn")!.SetValue(rider, seat);
    foreach (var (code, damage) in new[] { ("bison-tamed-male", 7.5f), ("bison-tamed-female", 7.5f),
        ("tame-ostrich-male", 8f), ("tame-ostrich-female", 8f) })
    {
        mount.Code = new AssetLocation("feverstonewilds", code);
        Check(FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(mount, rider), code + " accepts driver F attack");
        Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanGrabWithMount(mount), code + " has no R pickup");
        Check(FeverstoneWilds.Commands.GriffonMountedAbilities.MountedAttackDamage(mount) == damage, code + " uses species damage");
        driver = false;
        Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(mount, rider), code + " rejects passengers");
        driver = true;
    }
    foreach (string code in new[] { "bison-wild-male", "bison-calf-tamed", "tame-ostrich-chick", "ostrich-male" })
    {
        mount.Code = new AssetLocation("feverstonewilds", code);
        Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(mount, rider), code + " remains excluded");
    }
    foreach (string species in new[] { "bison", "tame-ostrich" })
    {
        var config = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/" + species + ".json")))!;
        var attack = config["client"]!["animations"]!.AsArray().Single(a => (string?)a?["code"] == "attack")!;
        Check((string?)attack["animation"] == "attack" && (bool)attack["supressDefaultAnimation"]!, species + " registers attack animation");
    }
    foreach (string shape in new[] { "mammal/bison-male", "mammal/bison-female", "bird/ostrich" })
    {
        var model = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "shapes/entity/land/" + shape + ".json")))!;
        Check(model["animations"]!.AsArray().Any(a => (string?)a?["code"] == "attack" && a["keyframes"]!.AsArray().Count > 0),
            shape + " has authored attack keyframes");
    }
    Console.WriteLine("Passed " + passed + " checks including bison and ostrich mounted attacks.");
    return;
}
var reproductionJson = griffon["server"]!["behaviors"]!.AsArray().Single(x => (string?)x?["code"] == "multiplybase")!;
Check((bool)reproductionJson["enabledByType"]!["*-tamed-female"]! && !(bool)reproductionJson["enabledByType"]!["*"]!,
    "vanilla egg feeding enabled only on tamed female griffons");
Check(!griffon["server"]!["behaviors"]!.AsArray().Any(x => (string?)x?["code"] == "multiply"), "no live-birth behavior alongside eggs");
foreach (string bird in new[] { "ostrich", "tame-ostrich", "cockatrice", "tame-cockatrice" })
{
    var json = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/" + bird + ".json")))!;
    Check(json["server"]!["behaviors"]!.AsArray().Any(x => (string?)x?["code"] == "taskai" &&
        x!["aitasks"]!.AsArray().Any(t => (string?)t?["code"] == "seekblockandlay")), bird + " retains poultry laying");
}
foreach (string stage in new[] { "griffon-egg", "griffon-egg-hatching-1", "griffon-egg-hatching-2" })
{
    var json = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/" + stage + ".json")))!;
    var behaviors = json["server"]!["behaviors"]!.AsArray();
    Check(behaviors.Count(x => (string?)x?["code"] == "fswegg") == 1 &&
        !behaviors.Any(x => (string?)x?["code"] is "grow" or "griffoneggpickup"), stage + " has one incubation authority");
}
var carriedEgg = new ItemStack(resolvedItem!);
carriedEgg.Attributes.SetDouble(EntityBehaviorFswEgg.ProgressKey, 175.25);
var carriedChick = new TreeAttribute();
carriedChick.SetString("code", "feverstonewilds:griffon-cub-tamed");
carriedChick.SetInt("generation", 4);
carriedEgg.Attributes["chick"] = carriedChick;
// Simulate saving the inventory, waiting, and placing at a later calendar time.
var savedEggAttributes = new TreeAttribute();
using (var eggBytes = new MemoryStream())
{
    using (var writer = new BinaryWriter(eggBytes, System.Text.Encoding.UTF8, true))
        carriedEgg.Attributes.ToBytes(writer);
    eggBytes.Position = 0;
    using var reader = new BinaryReader(eggBytes);
    savedEggAttributes.FromBytes(reader);
}
carriedEgg.Attributes = savedEggAttributes;
var placedEgg = new EntityAgent();
ItemFswEgg.RestoreProgress(carriedEgg, placedEgg, 2000);
Check(Math.Abs(2000 - placedEgg.WatchedAttributes.GetDouble(EntityBehaviorFswEgg.StartKey) - 175.25) < 0.00001,
    "carried egg preserves exact progress and pauses in inventory across save/load");
Check(placedEgg.WatchedAttributes.GetTreeAttribute(EntityBehaviorFswEgg.ChickKey).GetInt("generation") == 4,
    "placed egg preserves chick generation");
placedEgg.WatchedAttributes.GetTreeAttribute(EntityBehaviorFswEgg.ChickKey).SetInt("generation", 9);
Check(carriedEgg.Attributes.GetTreeAttribute("chick").GetInt("generation") == 4,
    "placement clones chick metadata without mutating stored eggs");
var variantData = new TreeAttribute();
BehaviorGriffonFeathers.EnsureVariant(variantData, new Random(1), 1);
BehaviorGriffonFeathers.EnsureVariant(variantData, new Random(1), 0);
Check(variantData.GetBool(BehaviorGriffonFeathers.VariantKey), "feather choice never rerolls once saved");
var noFeathers = new TreeAttribute();
BehaviorGriffonFeathers.EnsureVariant(noFeathers, new Random(1), 0);
BehaviorGriffonFeathers.EnsureVariant(noFeathers, new Random(1), 1);
Check(!noFeathers.GetBool(BehaviorGriffonFeathers.VariantKey), "saved normal arms remain normal");
var featherShape = Vintagestory.API.Datastructures.JsonObject.FromJson(File.ReadAllText(Path.Combine(assets, "shapes/entity/land/mammal/griffon-armfeathers.json"))).AsObject<Shape>();
var parentShape = Vintagestory.API.Datastructures.JsonObject.FromJson(File.ReadAllText(Path.Combine(assets, "shapes/entity/land/mammal/griffon-adult.json"))).AsObject<Shape>();
BehaviorGriffonFeathers.RemapTextures(featherShape, parentShape);
Check(AllFaces(featherShape.Elements).All(f => f.Texture == "griffonskin8"),
    "extra feathers reuse main skin texture code");
Check(Math.Abs(featherShape.Elements[0].FacesResolved![0].Uv[0] - 1.25) < 0.00001,
    "feather UVs preserve authored texture proportions");
var unassignedDetail = new ShapeElement { FacesResolved = new[] { new ShapeElementFace { Texture = "null", Uv = new float[] { 2, 4, 6, 8 } } } };
BehaviorGriffonFeathers.RemapTextures(new[] { new ShapeElement { Children = new[] { unassignedDetail } } }, 0.5f, 0.25f);
Check(unassignedDetail.FacesResolved[0].Texture == "griffonskin8" &&
    unassignedDetail.FacesResolved[0].Uv.SequenceEqual(new float[] { 1, 1, 3, 2 }),
    "unassigned nested feather faces inherit body skin and convert both UV axes");
var shapeLogger = Fake.Make<ILogger>((m, a) => Fake.Default(m.ReturnType));
featherShape.Textures.Clear();
featherShape.TextureSizes.Clear();
featherShape.TextureWidth = parentShape.TextureWidth;
featherShape.TextureHeight = parentShape.TextureHeight;
Check(parentShape.StepParentShape(featherShape, null, "feathers", "griffon", shapeLogger, null),
    "feather shape attaches using saved VSMC step parents");
Check(parentShape.GetElementByName("ForearmL").Children.Any(e => e.Name == "ArmFluff_L") &&
    parentShape.GetElementByName("ForearmR").Children.Any(e => e.Name == "ArmFluff_R"),
    "both feather groups follow their respective forearms");
Check(parentShape.Textures.Count == 2, "overlay does not introduce a separate texture selection");
Check(typeof(AiTaskSeekBlockAndLay).IsAssignableFrom(typeof(AiTaskGriffonSeekNestAndLay)),
    "griffons use vanilla nest seeking, laying and brooding");
Check(!griffon["server"]!["behaviors"]!.AsArray().Any(x => (string?)x?["code"] == "fsweggreproduction"),
    "griffons no longer run custom clutch reproduction");
var domesticNest = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "blocktypes/wood/griffonnest.json")))!;
Check((string?)domesticNest["class"] == "BlockHenbox" && (string?)domesticNest["entityClass"] == "HenBox",
    "domestic griffon nest uses vanilla henbox classes");
Check(domesticNest["variantgroups"]![0]!["states"]!.AsArray().Count == 1 &&
    (string?)domesticNest["variantgroups"]![0]!["states"]![0] == "ready", "construction blocks no longer generated");
var nestRecipe = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "recipes/grid/griffonnest.json")))!;
Check((string?)nestRecipe["ingredientPattern"] == "SFS,SSS" &&
    (int)nestRecipe["ingredients"]!["S"]!["quantity"]! == 3 &&
    (int)nestRecipe["ingredients"]!["F"]!["quantity"]! == 30 &&
    (string?)nestRecipe["output"]!["code"] == "feverstonewilds:griffonnest-ready",
    "recipe produces domestic nest from fifteen sticks and thirty feathers");
foreach (var (stage, next) in new[] { ("cub", "juvenile"), ("juvenile", "adolescent"), ("adolescent", "adult") })
{
    var stageJson = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/griffon-" + stage + ".json")))!;
    var grow = stageJson["server"]!["behaviors"]!.AsArray().Single(b => (string?)b?["code"] == "griffongrow")!;
    Check((int)grow["hoursToGrow"]! == 216, stage + " lasts nine game days");
    Check(grow["fedAdultEntityCodes"] == null && grow["fedAdultEntityCodesByType"] == null &&
        grow["orPortionsEatenForGrowing"] == null, stage + " cannot grow through feeding");
    foreach (string tame in new[] { "wild", "tamed" })
    foreach (string sex in new[] { "male", "female" })
    {
        string pattern = stage == "cub" ? "*-" + tame : "*-" + tame + "-" + sex;
        var codes = grow["adultEntityCodesByType"]![pattern]!.AsArray().Select(c => (string)c!).ToArray();
        string expected = "feverstonewilds:griffon-" + (next == "adult" ? "" : next + "-") + tame + "-" + sex;
        Check(codes.Contains(expected) && codes.All(c => c.Contains("-" + tame + "-")) &&
            (stage == "cub" || codes.Length == 1), stage + " preserves " + tame + " " + sex + " growth target");
    }
}
Check(BehaviorGriffonMarkings.SelectWing(0) == 1 && BehaviorGriffonMarkings.SelectWing(0.04285) == 1 &&
    BehaviorGriffonMarkings.SelectWing(0.3) == 8 && BehaviorGriffonMarkings.SelectWing(0.59999) == 14 &&
    BehaviorGriffonMarkings.SelectWing(0.6) == 0 && BehaviorGriffonMarkings.SelectWing(0.99999) == 0,
    "fourteen exclusive wingtip options share sixty percent and plain tips retain forty percent");
var markingsData = new TreeAttribute();
BehaviorGriffonMarkings.EnsureVariants(markingsData, new Random(42));
var chosenWing = markingsData.GetInt(BehaviorGriffonMarkings.WingKey);
var chosenSpots = markingsData.GetBool(BehaviorGriffonMarkings.SpotsKey);
var chosenEye = markingsData.GetInt(BehaviorGriffonMarkings.EyeKey);
BehaviorGriffonMarkings.EnsureVariants(markingsData, new Random(500));
Check(markingsData.GetInt(BehaviorGriffonMarkings.WingKey) == chosenWing &&
    markingsData.GetBool(BehaviorGriffonMarkings.SpotsKey) == chosenSpots &&
    markingsData.GetInt(BehaviorGriffonMarkings.EyeKey) == chosenEye, "markings and eyes never reroll");
var growingGriffon = new EntityAgent();
growingGriffon.WatchedAttributes.SetInt(BehaviorGriffonMarkings.WingKey, 2);
growingGriffon.WatchedAttributes.SetBool(BehaviorGriffonMarkings.SpotsKey, true);
growingGriffon.WatchedAttributes.SetInt(BehaviorGriffonMarkings.EyeKey, 6);
var grownGriffon = new EntityAgent();
BehaviorGriffonMarkings.CopyVariants(growingGriffon, grownGriffon);
Check(grownGriffon.WatchedAttributes.GetInt(BehaviorGriffonMarkings.WingKey) == 2 &&
    grownGriffon.WatchedAttributes.GetBool(BehaviorGriffonMarkings.SpotsKey) &&
    grownGriffon.WatchedAttributes.GetInt(BehaviorGriffonMarkings.EyeKey) == 6, "growth preserves markings and eye color");
var unmarkedShape = Vintagestory.API.Datastructures.JsonObject.FromJson(File.ReadAllText(Path.Combine(assets,
    "shapes/entity/land/mammal/griffon-adult.json"))).AsObject<Shape>()!;
var markedShape = unmarkedShape.Clone();
BehaviorGriffonMarkings.ApplyMarkings(markedShape.Elements, 2, true);
IEnumerable<ShapeElementFace> AllFaces(ShapeElement[] elements) => elements.SelectMany(e =>
    (e.FacesResolved ?? []).Where(f => f != null).Concat(AllFaces(e.Children ?? [])));
Check(AllFaces(markedShape.Elements).Any(f => f.Texture == "fswgriffonspots") &&
    AllFaces(markedShape.Elements).Any(f => f.Texture == "fswgriffonwingtip2"), "body and wing markings combine");
Check(!AllFaces(unmarkedShape.Elements).Any(f => f.Texture.StartsWith("fswgriffon")),
    "markings never alter another griffon's shared base model");
for (int eye = 1; eye <= BehaviorGriffonMarkings.EyeOverlays.Length; eye++)
foreach (bool spots in new[] { false, true })
{
    var eyeShape = unmarkedShape.Clone();
    BehaviorGriffonMarkings.ApplyMarkings(eyeShape.Elements, 12, spots, eye);
    string expectedBody = (spots ? "fswgriffonspotseye" : "fswgriffoneye") + eye;
    Check(AllFaces(eyeShape.Elements).Any(f => f.Texture == expectedBody) &&
        AllFaces(eyeShape.Elements).Any(f => f.Texture == "fswgriffonwingtip12"),
        $"eye {eye}, spots {spots} and extended wingtip combine without replacing one another");
}
var legacyMarkings = new TreeAttribute();
legacyMarkings.SetInt(BehaviorGriffonMarkings.WingKey, 2);
legacyMarkings.SetBool(BehaviorGriffonMarkings.SpotsKey, true);
BehaviorGriffonMarkings.EnsureVariants(legacyMarkings, new Random(1));
Check(legacyMarkings.GetInt(BehaviorGriffonMarkings.WingKey) == 2 && legacyMarkings.GetBool(BehaviorGriffonMarkings.SpotsKey) &&
    legacyMarkings.GetInt(BehaviorGriffonMarkings.EyeKey) is >= 1 and <= 6,
    "existing dark tips and spots survive adding a saved eye color");
var markingRandom = new Random(5);
int[] wingCounts = new int[BehaviorGriffonMarkings.WingOverlays.Length + 1]; int[] eyeCounts = new int[6]; int spotCount = 0;
for (int i = 0; i < 10000; i++)
{
    var data = new TreeAttribute(); BehaviorGriffonMarkings.EnsureVariants(data, markingRandom);
    wingCounts[data.GetInt(BehaviorGriffonMarkings.WingKey)]++;
    eyeCounts[data.GetInt(BehaviorGriffonMarkings.EyeKey) - 1]++;
    if (data.GetBool(BehaviorGriffonMarkings.SpotsKey)) spotCount++;
}
Check(Math.Abs(wingCounts[0] - 4000) < 250 && wingCounts.Skip(1).All(count => Math.Abs(count - 6000.0 / BehaviorGriffonMarkings.WingOverlays.Length) < 100) &&
    eyeCounts.All(count => Math.Abs(count - 10000.0 / 6) < 200) && Math.Abs(spotCount - 3000) < 200,
    "plain wings, fourteen tips, six eyes and independent spots follow selected probabilities");
foreach (string stage in new[] { "griffon", "griffon-adolescent" })
{
    var stageJson = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/" + stage + ".json")))!;
    var textureSets = stageJson["client"]!["textures"]!.AsObject();
    Check(textureSets.Count == 29, stage + " includes every base and combined variant texture set");
    for (int tip = 0; tip < BehaviorGriffonMarkings.WingOverlays.Length; tip++)
        Check((string?)textureSets["fswgriffonwingtip" + (tip + 1)]!["blendedOverlays"]![0]!["base"] ==
            "feverstonewilds:entity/land/griffon/griffon-feathertip-" + BehaviorGriffonMarkings.WingOverlays[tip],
            stage + " maps saved wingtip ID to its registered overlay " + (tip + 1));
    foreach (string code in textureSets.Select(kv => kv.Key).Where(key => key.StartsWith("fswgriffon")))
    {
        var composite = stageJson["client"]!["textures"]![code]!;
        var normal = stageJson["client"]!["textures"]![code.StartsWith("fswgriffonwingtip") ? "griffonskin8Wing" : "griffonskin8"]!;
        var normalSkins = new[] { normal }.Concat(normal["alternates"]!.AsArray().Select(x => x!)).ToArray();
        var markedSkins = new[] { composite }.Concat(composite["alternates"]!.AsArray().Select(x => x!)).ToArray();
        Check(normalSkins.Length == 9 && markedSkins.Length == normalSkins.Length,
            stage + " overlay supports every base skin: " + code);
        for (int skin = 0; skin < normalSkins.Length; skin++)
        {
            Check((string?)markedSkins[skin]["base"] == (string?)normalSkins[skin]["base"],
                $"{stage} {code} retains matching base texture at skin {skin + 1}");
            Check(markedSkins[skin]["blendedOverlays"]!.ToJsonString() == composite["blendedOverlays"]!.ToJsonString(),
                $"{stage} {code} retains the same marking at skin {skin + 1}");
        }
        foreach (var layer in composite["blendedOverlays"]!.AsArray())
            Check(File.Exists(Path.Combine(assets, "textures", ((string)layer!["base"]!).Split(':').Last() + ".png")),
                stage + " overlay file exists for " + code);
        if (code.StartsWith("fswgriffonspotseye"))
            Check(composite["blendedOverlays"]!.AsArray().Count == 2,
                stage + " composites preserve spots beneath the eye overlay: " + code);
    }
}
var commandFiles = new[] { "bison", "bison-calf", "horse", "foal", "griffon", "griffon-cub", "griffon-juvenile", "griffon-adolescent", "tame-cockatrice", "tame-cockatrice-chick", "tame-direwolf", "tame-direwolf-pup", "tame-ostrich", "tame-ostrich-chick" };
foreach (string file in commandFiles)
{
    var json = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/" + file + ".json")))!;
    foreach (string side in new[] { "client", "server" })
    {
        var behavior = json[side]!["behaviors"]!.AsArray().Single(x => (string?)x?["code"] == "fswildsbehavior")!;
        Check(file.StartsWith("tame-") || (bool)behavior["enabledByType"]!["*-tamed-*"]! && !(bool)behavior["enabledByType"]!["*"]!,
            file + " commands enabled only for tame variants on " + side);
    }
    var ai = json["server"]!["behaviors"]!.AsArray().Single(x => (string?)x?["code"] == "taskai")!;
    Check(ai["aitasks"]!.AsArray().Any(x => (string?)x?["code"] == "fswfollowowner") &&
        ai["aitasks"]!.AsArray().Any(x => (string?)x?["code"] == "fswcompanionassist"), file + " has companion movement and combat");
}
EntityAgent Companion(long id)
{
    var e = Mother(id);
    typeof(Entity).GetProperty("Properties")!.SetValue(e, new EntityProperties { Server = new EntityServerProperties([], new()) });
    e.SidedProperties.Behaviors.Add(new FeverstoneWilds.Commands.FSWildsBehavior(e));
    return e;
}
var commandYoung = Companion(801);
var commandAdult = Mother(802);
commandYoung.WatchedAttributes.SetString("fswCompanionOwnerUid", "owner-a");
commandYoung.WatchedAttributes.SetString("fswCompanionName", "Velria");
commandYoung.WatchedAttributes.SetInt("fswCompanionMoveMode", 1);
commandYoung.WatchedAttributes.SetInt("fswCompanionStance", 0);
var commandOwned = new TreeAttribute(); commandOwned.SetString("uid", "owner-a");
commandYoung.WatchedAttributes.SetAttribute("ownedby", commandOwned);
FeverstoneWilds.Commands.FSWildsBehavior.CopyState(commandYoung, commandAdult);
Check(commandAdult.WatchedAttributes.GetString("fswCompanionOwnerUid") == "owner-a" &&
    commandAdult.WatchedAttributes.GetString("fswCompanionName") == "Velria" &&
    commandAdult.WatchedAttributes.GetInt("fswCompanionMoveMode") == 1 &&
    commandAdult.WatchedAttributes.GetInt("fswCompanionStance") == 0, "growth preserves companion commands and ownership");
commandOwned.SetString("uid", "changed");
Check(commandAdult.WatchedAttributes.GetTreeAttribute("ownedby").GetString("uid") == "owner-a", "growth clones vanilla ownership");
Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanAttack(commandYoung, commandAdult), "companions cannot damage another tame of the same owner");
var strangerTame = Companion(803);
strangerTame.WatchedAttributes.SetString("fswCompanionOwnerUid", "owner-b");
Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanAttack(commandYoung, strangerTame), "other tames protected from random hunting");
commandYoung.WatchedAttributes.SetLong("fswCompanionCombatTargetId", strangerTame.EntityId);
commandYoung.WatchedAttributes.SetLong("fswCompanionCombatTargetUntil", milliseconds + 1000);
Check(FeverstoneWilds.Commands.FSWildsBehavior.CanAttack(commandYoung, strangerTame), "explicit defensive target may be another owner's tame");
milliseconds += 1001;
Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanAttack(commandYoung, strangerTame), "defensive target expires");
Check(!FeverstoneWilds.Commands.FSWildsBehavior.IsOwner(commandYoung, "intruder") && !FeverstoneWilds.Commands.FSWildsBehavior.IsOwner(commandYoung, "") && FeverstoneWilds.Commands.FSWildsBehavior.IsOwner(commandYoung, "owner-a"), "command authorization accepts only the saved owner");
var stanceBehavior = commandYoung.GetBehavior<FeverstoneWilds.Commands.FSWildsBehavior>()!;
foreach (int stance in new[] { 0, 1, 2, 3 })
{
    foreach (bool ownerAttacked in new[] { false, true })
    {
        commandYoung.WatchedAttributes.SetInt("fswCompanionStance", stance);
        commandYoung.WatchedAttributes.SetLong("fswCompanionCombatTargetId", 0);
        stanceBehavior.SetDefensiveCombatTarget(strangerTame, ownerAttacked);
        bool expected = stance == 1 || stance == 3 && ownerAttacked;
        Check((commandYoung.WatchedAttributes.GetLong("fswCompanionCombatTargetId", 0) == strangerTame.EntityId) == expected,
            $"stance {stance} routes owner combat correctly, owner attacked={ownerAttacked}");
    }
}
commandYoung.WatchedAttributes.SetInt("fswCompanionStance", 3);
commandYoung.WatchedAttributes.SetLong("fswCompanionCombatTargetId", 0);
float protectiveDamage = 1;
stanceBehavior.OnEntityReceiveDamage(new DamageSource { SourceEntity = strangerTame }, ref protectiveDamage);
Check(commandYoung.WatchedAttributes.GetLong("fswCompanionCombatTargetId", 0) == 0,
    "Protective does not retaliate for damage to the companion alone");
stanceBehavior.SetDefensiveCombatTarget(strangerTame, true);
typeof(FeverstoneWilds.Commands.FSWildsBehavior).GetMethod("SetStance", BindingFlags.Instance | BindingFlags.NonPublic)!
    .Invoke(stanceBehavior, new object[] { 3 });
Check(commandYoung.WatchedAttributes.GetLong("fswCompanionCombatTargetId", 0) == 0 &&
    commandYoung.WatchedAttributes.GetLong("fswCompanionCombatTargetUntil", 0) == 0,
    "changing stance clears targets acquired under the previous stance");
var savedStance = new TreeAttribute();
savedStance.FromBytes(commandYoung.WatchedAttributes.ToBytes());
Check(savedStance.GetInt("fswCompanionStance") == 3, "Protective survives watched attribute serialization");
FeverstoneWilds.Commands.FSWildsBehavior.CopyState(commandYoung, commandAdult);
Check(commandAdult.WatchedAttributes.GetInt("fswCompanionStance") == 3, "Protective survives companion growth state transfer");
var releasedCompanion = Companion(850);
releasedCompanion.Code = new AssetLocation("feverstonewilds:griffon-tamed-female");
releasedCompanion.WatchedAttributes.SetString("fswCompanionOwnerUid", "owner-a");
releasedCompanion.WatchedAttributes.SetString("fswCompanionOwnerName", "Owner A");
releasedCompanion.WatchedAttributes.SetString("fswCompanionName", "Peak");
releasedCompanion.WatchedAttributes.SetInt("fswCompanionStance", 2);
releasedCompanion.WatchedAttributes.SetInt("fswCompanionMoveMode", 0);
releasedCompanion.WatchedAttributes.SetInt("fswCompanionPvpMode", 1);
releasedCompanion.WatchedAttributes.SetLong("fswCompanionCombatTargetId", strangerTame.EntityId);
releasedCompanion.WatchedAttributes.SetLong("fswCompanionCombatTargetUntil", milliseconds + 20000);
var releasedVanillaOwner = new TreeAttribute();
releasedVanillaOwner.SetString("uid", "owner-a");
releasedCompanion.WatchedAttributes.SetAttribute("ownedby", releasedVanillaOwner);
var releaseBehavior = releasedCompanion.GetBehavior<FeverstoneWilds.Commands.FSWildsBehavior>()!;
Check(!releaseBehavior.TryReleaseOwnership("intruder") &&
    FeverstoneWilds.Commands.FSWildsBehavior.IsOwner(releasedCompanion, "owner-a"),
    "another player cannot release a companion");
Check(releaseBehavior.TryReleaseOwnership("owner-a"), "owner can release their companion");
Check(!releasedCompanion.WatchedAttributes.HasAttribute("ownedby") &&
    !releasedCompanion.WatchedAttributes.HasAttribute("fswCompanionOwnerUid") &&
    !releasedCompanion.WatchedAttributes.HasAttribute("fswCompanionOwnerName"),
    "release clears both vanilla and companion ownership");
typeof(FeverstoneWilds.Commands.FSWildsBehavior).GetMethod("SyncVanillaOwner", BindingFlags.Instance | BindingFlags.NonPublic)!
    .Invoke(releaseBehavior, null);
Check(!releasedCompanion.WatchedAttributes.HasAttribute("fswCompanionOwnerUid"),
    "vanilla synchronization cannot restore the released claim");
Check(releasedCompanion.Code.Path == "griffon-tamed-female" &&
    releasedCompanion.WatchedAttributes.GetString("fswCompanionName") == "Peak",
    "release preserves tame entity and name");
Check(releasedCompanion.WatchedAttributes.GetInt("fswCompanionMoveMode") == 2 &&
    releasedCompanion.WatchedAttributes.GetInt("fswCompanionStance") == 0 &&
    releasedCompanion.WatchedAttributes.GetInt("fswCompanionPvpMode") == 0 &&
    releasedCompanion.WatchedAttributes.GetLong("fswCompanionCombatTargetId") == 0 &&
    releasedCompanion.WatchedAttributes.GetLong("fswCompanionCombatTargetUntil") == 0,
    "release stops owner follow and clears combat targets");
Check(!releaseBehavior.TryReleaseOwnership("owner-a"), "old owner cannot command another release");
var releasedSave = new TreeAttribute();
releasedSave.FromBytes(releasedCompanion.WatchedAttributes.ToBytes());
Check(!releasedSave.HasAttribute("ownedby") && !releasedSave.HasAttribute("fswCompanionOwnerUid"),
    "released ownership remains cleared after serialization");
var commandTitleBar = (Vintagestory.API.Client.GuiElementDialogTitleBar)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
    typeof(FeverstoneWilds.Commands.CompanionTitleBar));
var vanillaTitleBar = (Vintagestory.API.Client.GuiElementDialogTitleBar)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
    typeof(Vintagestory.API.Client.GuiElementDialogTitleBar));
Check(FeverstoneWilds.Commands.CompanionTitleBarSystem.UsesPanelBackground(commandTitleBar),
    "companion command header uses the panel background");
Check(!FeverstoneWilds.Commands.CompanionTitleBarSystem.UsesPanelBackground(vanillaTitleBar),
    "normal title bars retain their native backgrounds");
var titleBarPatches = FeverstoneWilds.Commands.CompanionTitleBarSystem.Install();
Check(HarmonyLib.Harmony.GetPatchInfo(typeof(Vintagestory.API.Client.GuiElementDialogTitleBar).GetMethod("ComposeTextElements"))!
    .Owners.Contains("feverstonewilds.companiontitlebar"), "header composition patch installs against VS 1.22.7");
titleBarPatches.UnpatchAll("feverstonewilds.companiontitlebar");
foreach (bool releaseChoice in new[] { false, true })
{
    var pendingUi = new Queue<Action>();
    int releasePackets = 0, releaseConfirmed = 0, releaseClosed = 0;
    var uiEvents = Fake.Make<Vintagestory.API.Client.IClientEventAPI>((m, a) => {
        if (m.Name == "EnqueueMainThreadTask") pendingUi.Enqueue((Action)a[0]!);
        return Fake.Default(m.ReturnType);
    });
    var uiNetwork = Fake.Make<Vintagestory.API.Client.IClientNetworkAPI>((m, a) => {
        if (m.Name == "SendEntityPacket") releasePackets++;
        return Fake.Default(m.ReturnType);
    });
    var uiGui = Fake.Make<Vintagestory.API.Client.IGuiAPI>((m, a) => Fake.Default(m.ReturnType));
    var uiApi = Fake.Make<Vintagestory.API.Client.ICoreClientAPI>((m, a) => m.Name switch {
        "get_Event" => uiEvents, "get_Network" => uiNetwork, "get_Gui" => uiGui,
        _ => Fake.Default(m.ReturnType)
    });
    var dialog = (FeverstoneWilds.Commands.FswCompanionReleaseConfirm)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
        typeof(FeverstoneWilds.Commands.FswCompanionReleaseConfirm));
    HarmonyLib.AccessTools.Field(typeof(Vintagestory.API.Client.GuiDialog), "capi").SetValue(dialog, uiApi);
    HarmonyLib.AccessTools.Field(typeof(Vintagestory.API.Client.GuiDialog), "opened").SetValue(dialog, true);
    HarmonyLib.AccessTools.Field(dialog.GetType(), "companion").SetValue(dialog, releasedCompanion);
    HarmonyLib.AccessTools.Field(dialog.GetType(), "onConfirmed").SetValue(dialog, (Action)(() => releaseConfirmed++));
    HarmonyLib.AccessTools.Field(dialog.GetType(), "onClosed").SetValue(dialog, (Action)(() => releaseClosed++));
    var choose = HarmonyLib.AccessTools.Method(dialog.GetType(), releaseChoice ? "OnYes" : "OnNo");
    choose.Invoke(dialog, null);
    choose.Invoke(dialog, null);
    Check(dialog.IsOpened() && releasePackets == 0 && pendingUi.Count == 1,
        $"release choice {releaseChoice} waits for input completion and ignores duplicate clicks");
    while (pendingUi.Count > 0) pendingUi.Dequeue()();
    Check(!dialog.IsOpened() && releaseClosed == 1,
        $"release choice {releaseChoice} closes the dialog and completes cleanup");
    Check(releasePackets == (releaseChoice ? 1 : 0) && releaseConfirmed == (releaseChoice ? 1 : 0),
        $"release choice {releaseChoice} only confirms ownership release for Yes");
}
var accessCompanion = Companion(860);
accessCompanion.Code = new AssetLocation("feverstonewilds:griffon-tamed-female");
accessCompanion.WatchedAttributes.SetString("fswCompanionOwnerUid", "owner-a");
var accessBehavior = accessCompanion.GetBehavior<FeverstoneWilds.Commands.FSWildsBehavior>()!;
Check(FeverstoneWilds.Commands.FSWildsBehavior.CanUseCompanion(accessCompanion, "visitor"),
    "existing claimed companions default to Public access");
foreach (int command in new[] { 0, 1, 2, 10, 11, 12, 13, 20, 30, 31 })
    Check(FeverstoneWilds.Commands.FSWildsBehavior.CanIssueCompanionCommand(accessCompanion, "visitor", command),
        $"Public permits visitor command {command}");
foreach (int command in new[] { 40, 41, 50, 51 })
    Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanIssueCompanionCommand(accessCompanion, "visitor", command),
        $"Public does not grant visitor ownership management {command}");
Check(!accessBehavior.TrySetAccessMode("visitor", 1) && !accessBehavior.TrySetAccessMode("owner-a", 2),
    "only owner can set a valid access mode");
Check(accessBehavior.TrySetAccessMode("owner-a", 1), "owner can confirm Private access");
Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanUseCompanion(accessCompanion, "visitor") &&
    FeverstoneWilds.Commands.FSWildsBehavior.CanUseCompanion(accessCompanion, "owner-a"),
    "Private restricts command access to owner");
Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanRideCompanion(accessCompanion, new EntityPlayer { World = world }),
    "Private rejects nonowner riders");
foreach (int command in new[] { 0, 13, 20, 40, 41, 50, 51 })
    Check(!FeverstoneWilds.Commands.FSWildsBehavior.CanIssueCompanionCommand(accessCompanion, "visitor", command) &&
        FeverstoneWilds.Commands.FSWildsBehavior.CanIssueCompanionCommand(accessCompanion, "owner-a", command),
        $"Private authorizes only owner for command {command}");
var accessSave = new TreeAttribute();
accessSave.FromBytes(accessCompanion.WatchedAttributes.ToBytes());
Check(accessSave.GetInt(FeverstoneWilds.Commands.FSWildsBehavior.AccessModeKey) == 1,
    "Private access persists through serialization");
var accessAdult = Mother(861);
FeverstoneWilds.Commands.FSWildsBehavior.CopyState(accessCompanion, accessAdult);
Check(accessAdult.WatchedAttributes.GetInt(FeverstoneWilds.Commands.FSWildsBehavior.AccessModeKey) == 1,
    "growth retains selected access mode");
Check(accessBehavior.TrySetAccessMode("owner-a", 0) &&
    FeverstoneWilds.Commands.FSWildsBehavior.CanUseCompanion(accessCompanion, "visitor"),
    "owner can restore Public access");
Check(accessBehavior.TryReleaseOwnership("owner-a") &&
    accessCompanion.WatchedAttributes.GetInt(FeverstoneWilds.Commands.FSWildsBehavior.AccessModeKey, -1) == 0 &&
    FeverstoneWilds.Commands.FSWildsBehavior.CanRideCompanion(accessCompanion, new EntityPlayer { World = world }) &&
    !FeverstoneWilds.Commands.FSWildsBehavior.CanUseCompanion(accessCompanion, "visitor"),
    "release clears Private restrictions while unclaimed commands require a claim");
var commandPatches = FeverstoneWilds.Commands.FSWildsCommandSystem.InstallCombatHooks();
Check(HarmonyLib.Harmony.GetPatchInfo(typeof(EntityPlayer).GetMethod("OnHurt", new[] { typeof(DamageSource), typeof(float) }))!.Owners.Contains("feverstonewilds.commands"), "companion combat hooks install against the game API");
commandPatches.UnpatchAll("feverstonewilds.commands");
var abilityMount = Mother(901); abilityMount.Code = new AssetLocation("feverstonewilds:griffon-tamed-female");
var abilityRider = new EntityPlayer { World = world, EntityId = 902 };
var abilityPrey = Mother(903); abilityPrey.Code = new AssetLocation("game:chicken-hen");
abilityMount.Pos.SetPos(0, 0, 0); abilityMount.Pos.Yaw = 0;
abilityPrey.Pos.SetPos(0, 0, 3);
Check(FeverstoneWilds.Commands.GriffonMountedAbilities.IsTamedGriffon(abilityMount), "adult tame griffons have mounted abilities");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(abilityMount, abilityRider), "unmounted players cannot activate abilities");
abilityMount.Code = new AssetLocation("feverstonewilds:griffon-wild-male");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsTamedGriffon(abilityMount), "wild griffons have no mounted abilities");
abilityMount.Code = new AssetLocation("feverstonewilds:griffon-adolescent-tamed-male");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsTamedGriffon(abilityMount), "young griffons have no mounted abilities");
abilityMount.Code = new AssetLocation("feverstonewilds:griffon-tamed-male");
Check(FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, false), "animals can be grabbed in PvE");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityRider, true), "driver cannot grab themselves");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityMount, true), "griffon cannot grab itself");
var abilityPlayer = new EntityPlayer { World = world, EntityId = 904 };
abilityPrey.Alive = false;
Check(FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, false), "eligible animal corpses can be grabbed in PvE");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanMountedAttack(abilityMount, abilityRider, abilityPrey), "corpse pickup does not enable attacking dead animals");
abilityPrey.Code = new AssetLocation("game:bear-brown");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, true), "ineligible species remain excluded when dead");
abilityPrey.Code = new AssetLocation("game:chicken-hen");
abilityPrey.Alive = true;
foreach (string cockatriceCode in new[] { "cockatrice", "tame-cockatrice", "cockatrice-chick", "tame-cockatrice-chick" })
{
    abilityPrey.Code = new AssetLocation("feverstonewilds", cockatriceCode);
    foreach (bool alive in new[] { true, false })
    {
        abilityPrey.Alive = alive;
        Check(FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, false),
            "griffons can pick up cockatrices: " + cockatriceCode + " alive=" + alive);
    }
}
abilityPrey.Alive = true;
foreach (var youngCode in new[] { "griffon-cub-wild", "griffon-cub-tamed",
    "griffon-juvenile-wild-male", "griffon-juvenile-wild-female",
    "griffon-juvenile-tamed-male", "griffon-juvenile-tamed-female" })
{
    abilityPrey.Code = new AssetLocation("feverstonewilds:" + youngCode);
    foreach (bool alive in new[] { true, false })
    {
        abilityPrey.Alive = alive;
        Check(FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, false),
            "griffon young can be picked up: " + youngCode + " alive=" + alive);
    }
}
foreach (var largeCode in new[] { "griffon-adolescent-tamed-male", "griffon-tamed-female" })
{
    abilityPrey.Code = new AssetLocation("feverstonewilds:" + largeCode);
    Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, false),
        "larger griffons remain excluded from pickup: " + largeCode);
}
abilityPrey.Code = new AssetLocation("game:chicken-hen");
abilityPrey.Alive = true;
abilityPlayer.Alive = false;
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPlayer, true), "dead players cannot be grabbed even with PvP enabled");
abilityPlayer.Alive = true;
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPlayer, false) &&
    FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPlayer, true), "player grabs require PvP");
abilityPrey.WatchedAttributes.SetBool("fswGriffonCarried", true);
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, true), "already carried animals cannot be stolen by another grab");
abilityPrey.WatchedAttributes.SetBool("fswGriffonCarried", false);
Check(double.IsFinite(FeverstoneWilds.Commands.GriffonMountedAbilities.TargetScore(abilityMount, abilityPrey, true)), "forward prey is within grab range");
abilityPrey.Pos.Z = -3;
Check(double.IsPositiveInfinity(FeverstoneWilds.Commands.GriffonMountedAbilities.TargetScore(abilityMount, abilityPrey, true)), "prey behind griffon is rejected");
abilityPrey.Pos.Z = 4.25;
Check(double.IsPositiveInfinity(FeverstoneWilds.Commands.GriffonMountedAbilities.TargetScore(abilityMount, abilityPrey, true)) &&
    double.IsFinite(FeverstoneWilds.Commands.GriffonMountedAbilities.TargetScore(abilityMount, abilityPrey, false)), "attack and grab use their distinct ranges");
abilityPrey.Pos.SetPos(0, 3, 3);
Check(double.IsPositiveInfinity(FeverstoneWilds.Commands.GriffonMountedAbilities.TargetScore(abilityMount, abilityPrey, false)), "targets outside vertical reach are rejected");
bool unmountResult = true;
abilityPlayer.WatchedAttributes.SetBool("fswGriffonGrabbedPlayer", true);
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.PreventGrabbedUnmount(abilityPlayer, ref unmountResult) && !unmountResult, "grabbed player cannot voluntarily detach");
abilityPlayer.WatchedAttributes.SetBool("fswGriffonGrabbedPlayer", false);
Check(FeverstoneWilds.Commands.GriffonMountedAbilities.PreventGrabbedUnmount(abilityPlayer, ref unmountResult), "normal dismount restored after release");
var abilitySeats = griffon["behaviorConfigs"]!["flightrideable"]!["seats"]!.AsArray();
Check(abilitySeats.Count == 2 && (bool)abilitySeats[0]!["controllable"]! &&
    !(bool)abilitySeats[1]!["controllable"]! && (string?)abilitySeats[1]!["apName"] == "FaceAP" &&
    (string?)abilitySeats[1]!["selectionBox"] == "GriffonCarry", "reserved carry seat preserves the normal driver seat");
var guardMethod = typeof(FeverstoneWilds.Commands.GriffonMountedAbilities).GetMethod("InstallUnmountGuard", BindingFlags.NonPublic | BindingFlags.Instance)!;
var abilitySystem = new FeverstoneWilds.Commands.GriffonMountedAbilities();
guardMethod.Invoke(abilitySystem, null);
Check(HarmonyLib.Harmony.GetPatchInfo(typeof(EntityAgent).GetMethod("TryUnmount", Type.EmptyTypes))!.Owners.Contains("feverstonewilds.griffon-grab"), "grab dismount guard installs against the game API");
abilitySystem.Dispose();
abilityPrey.WatchedAttributes.SetBool("fswGriffonCarried", true);
typeof(FeverstoneWilds.Commands.GriffonMountedAbilities).GetMethod("ResetLoadedCarry", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(abilitySystem, new object[] { abilityPrey });
Check(!abilityPrey.WatchedAttributes.GetBool("fswGriffonCarried"), "reload clears stale creature carry state");
var driverSeat = Fake.Make<IMountableSeat>((m, a) => m.Name switch
{
    "get_Entity" => abilityMount,
    "get_CanControl" => true,
    _ => Fake.Default(m.ReturnType)
});
typeof(EntityAgent).GetProperty("MountedOn")!.SetValue(abilityRider, driverSeat);
Check(FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(abilityMount, abilityRider), "actual driver can use an unclaimed griffon's abilities");
foreach (string code in new[] { "tame-direwolf-male", "tame-direwolf-female" })
{
    abilityMount.Code = new AssetLocation("feverstonewilds", code);
    Check(FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(abilityMount, abilityRider),
        code + " driver can use mounted attack and pickup");
    Check(FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPrey, false),
        code + " can pick up eligible animals without PvP");
    Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.IsGrabCandidate(abilityMount, abilityRider, abilityPlayer, false),
        code + " cannot pick up players with server PvP disabled");
}
foreach (string code in new[] { "direwolf-male", "tame-direwolf-pup", "tame-direwolf-pup-male" })
{
    abilityMount.Code = new AssetLocation("feverstonewilds", code);
    Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(abilityMount, abilityRider),
        code + " does not gain mounted abilities");
}
abilityMount.Code = new AssetLocation("feverstonewilds:griffon-tamed-male");
Check(FeverstoneWilds.Commands.MountedInfoHudSystem.ShouldHide(abilityRider, abilityMount),
    "rider does not see their own mount information at any attachment selection");
Check(!FeverstoneWilds.Commands.MountedInfoHudSystem.ShouldHide(abilityRider, abilityPrey),
    "rider can still inspect other entities");
Check(!FeverstoneWilds.Commands.MountedInfoHudSystem.ShouldHide(new EntityPlayer { World = world }, abilityMount),
    "other players can still inspect the mounted griffon");
Check(!FeverstoneWilds.Commands.MountedInfoHudSystem.ShouldHide(abilityRider, null!),
    "block and empty selections are not hidden");
var originalMountCode = abilityMount.Code;
abilityMount.Code = new AssetLocation("game:elk-tamed");
Check(!FeverstoneWilds.Commands.MountedInfoHudSystem.ShouldHide(abilityRider, abilityMount),
    "vanilla mount HUD is unaffected");
abilityMount.Code = originalMountCode;
var passengerSeat = Fake.Make<IMountableSeat>((m, a) => m.Name switch
{
    "get_Entity" => abilityMount,
    "get_CanControl" => false,
    _ => Fake.Default(m.ReturnType)
});
typeof(EntityAgent).GetProperty("MountedOn")!.SetValue(abilityRider, passengerSeat);
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(abilityMount, abilityRider), "noncontrolling passengers cannot use abilities");
typeof(EntityAgent).GetProperty("MountedOn")!.SetValue(abilityRider, null);
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(abilityMount, abilityRider), "remote unmounted ability requests remain rejected");
Check(!FeverstoneWilds.Commands.MountedInfoHudSystem.ShouldHide(abilityRider, abilityMount), "mount information returns immediately after dismounting");
Check(FeverstoneWilds.Commands.GriffonMountedAbilities.CanMountedAttack(abilityMount, abilityRider, abilityPrey), "unclaimed mounted griffon may attack wild prey");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanMountedAttack(abilityMount, abilityRider, abilityRider), "mounted attack cannot damage its driver");
Check(!FeverstoneWilds.Commands.GriffonMountedAbilities.CanMountedAttack(abilityMount, abilityRider, abilityPlayer), "unclaimed mounted griffon still respects PvE attack mode");
abilityMount.WatchedAttributes.SetInt(BehaviorGriffonMarkings.WingKey, 1);
abilityMount.WatchedAttributes.SetBool(BehaviorGriffonMarkings.SpotsKey, true);
Shape serverShape = new Shape(); bool serverClone = false; string[] serverDeletes = [];
new BehaviorGriffonMarkings(abilityMount).OnTesselation(ref serverShape, "griffon-adult", ref serverClone, ref serverDeletes);
abilityMount.WatchedAttributes.SetBool(BehaviorGriffonFeathers.VariantKey, true);
new BehaviorGriffonFeathers(abilityMount).OnTesselation(ref serverShape, "griffon-adult", ref serverClone, ref serverDeletes);
Check(!serverClone, "server griffon tesselation skips client-only markings and feathers without accessing missing client textures");
var mapShips = new Dictionary<string, EntityOwnership> { ["vanilla-mount"] = new EntityOwnership { EntityId = 991, Pos = new EntityPos() } };
var mapRecords = new Dictionary<long, FeverstoneWilds.Commands.FswCompanionMapPacket>
{
    [992] = new FeverstoneWilds.Commands.FswCompanionMapPacket { EntityId = 992, X = 20, Y = 110, Z = 40, Dimension = 0, Yaw = 1.5f, Name = "Verlia's griffon" }
};
Check(FeverstoneWilds.Commands.FswCompanionMapSystem.MergeRecords(mapShips, mapRecords), "new companion adds an owned-creature marker");
var mapEntry = mapShips["feverstonewilds-companion-992"];
Check(mapEntry.Name == "Verlia's griffon" && mapEntry.Pos.X == 20 && mapEntry.Pos.Yaw == 1.5f && mapEntry.Color == "#f5a000", "marker carries the companion name, position, heading and yellow-orange color");
mapRecords[992].X = 25;
Check(!FeverstoneWilds.Commands.FswCompanionMapSystem.MergeRecords(mapShips, mapRecords) &&
    ReferenceEquals(mapEntry, mapShips["feverstonewilds-companion-992"]) && mapEntry.Pos.X == 25, "movement updates existing marker references without rebuilding the map layer");
mapRecords.Clear();
Check(FeverstoneWilds.Commands.FswCompanionMapSystem.MergeRecords(mapShips, mapRecords) &&
    mapShips.Count == 1 && mapShips.ContainsKey("vanilla-mount"), "companion removal preserves unrelated vanilla markers");
FeverstoneWilds.Commands.FswCompanionMapSystem.PrepareGrowth(commandYoung);
Check(commandYoung.Attributes.GetBool("fswCompanionGrowing"), "growth is marked as replacement rather than companion death");
foreach (string side in new[] { "client", "server" })
{
    var woundedConfig = griffon[side]!["behaviors"]!.AsArray().Single(b => (string?)b?["code"] == "mortallywoundable")!;
    Check((bool)woundedConfig["enabledByType"]!["*-tamed-*"]! && !(bool)woundedConfig["enabledByType"]!["*"]! &&
        (int)woundedConfig["whenBelowHealth"]! == 6, "elk-style wounded behavior enabled only for tamed adults on " + side);
}
var woundedAnimationCodes = new[] { "wounded-idle", "wounded-resthead", "wounded-look", "wounded-spasm", "wounded-trystand", "wounded-stand", "dead" };
foreach (string code in woundedAnimationCodes)
{
    var metadata = griffon["client"]!["animations"]!.AsArray().Single(a => (string?)a?["code"] == code)!;
    var model = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "shapes/entity/land/mammal/griffon-adult.json")))!;
    Check(model["animations"]!.AsArray().Any(a => string.Equals((string?)a?["code"], (string?)metadata["animation"], StringComparison.OrdinalIgnoreCase) &&
        a!["keyframes"]!.AsArray().Count > 0), "wounded animation resolves to an existing nonempty griffon animation: " + code);
}
var downedGriffon = Companion(1101); downedGriffon.Code = new AssetLocation("feverstonewilds:griffon-tamed-female");
var woundedBehavior = new EntityBehaviorMortallyWoundable(downedGriffon);
downedGriffon.SidedProperties.Behaviors.Add(woundedBehavior);
var downedSeat = Fake.Make<IMountableSeat>((m,a) => m.Name switch { "get_Entity" => downedGriffon, "get_CanControl" => true, _ => Fake.Default(m.ReturnType) });
typeof(EntityAgent).GetProperty("MountedOn")!.SetValue(abilityRider, downedSeat);
woundedBehavior.HealthState = EnumEntityHealthState.MortallyWounded;
Check(GriffonHealthState.IsIncapacitated(downedGriffon) && !FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(downedGriffon, abilityRider), "downed griffon cannot attack or grab");
woundedBehavior.HealthState = EnumEntityHealthState.Recovering;
Check(GriffonHealthState.IsIncapacitated(downedGriffon) && !FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(downedGriffon, abilityRider), "recovering griffon remains incapacitated");
woundedBehavior.HealthState = EnumEntityHealthState.Normal;
Check(!GriffonHealthState.IsIncapacitated(downedGriffon) && FeverstoneWilds.Commands.GriffonMountedAbilities.CanUseAbilities(downedGriffon, abilityRider), "normal controls restored after recovery");
typeof(EntityAgent).GetProperty("MountedOn")!.SetValue(abilityRider, null);
var healingLimit = (int)griffon["attributes"]!["minGenerationToAllowHealingByType"]!["*-tamed-*"]!;
downedGriffon.Properties.Attributes = Vintagestory.API.Datastructures.JsonObject.FromJson("{\"minGenerationToAllowHealing\":" + healingLimit + "}");
var poultice = new ItemPoultice();
downedGriffon.WatchedAttributes.SetInt("generation", 0);
Check(poultice.CanHeal(downedGriffon), "vanilla poultice can heal a first-generation tame griffon");
downedGriffon.WatchedAttributes.SetInt("generation", 7);
Check(poultice.CanHeal(downedGriffon), "vanilla poultice can also heal bred descendants");
Check((int)griffon["attributes"]!["minGenerationToAllowHealingByType"]!["*"]! == -1, "wild griffons are not made poultice-healable");
Check(EggHeat.Advance(12, 24, false, false) == 12, "cold eggs preserve progress without incubating");
Check(EggHeat.Advance(12, 24, false, true) == 12, "new heat does not credit the preceding cold interval");
Check(EggHeat.Advance(12, 24, true, false) == 12, "heat loss pauses incubation");
Check(EggHeat.Advance(12, 24, true, true) == 36, "continuous heat advances incubation");
Check(EggHeat.Advance(12, -24, true, true) == 12, "clock reversal cannot erase progress");
var heatBlock = new TestHeatBlock { Strength = 1 };
int sourceX = 3, sourceLayer = BlockLayersAccess.Solid;
var heatAccessor = Fake.Make<IBlockAccessor>((m,a) => m.Name == "GetBlock" && a.Length == 2 &&
    a[0] is BlockPos p && p.X == sourceX && p.Y == 0 && p.Z == 0 && (int)a[1]! == sourceLayer
    ? heatBlock : Fake.Default(m.ReturnType));
var heatWorld = Fake.Make<IWorldAccessor>((m,a) => m.Name == "get_BlockAccessor" ? heatAccessor : Fake.Default(m.ReturnType));
Check(EggHeat.HasNearbyHeat(heatWorld, new BlockPos(0,0,0)), "active heat at three blocks is detected");
sourceX = 4;
Check(!EggHeat.HasNearbyHeat(heatWorld, new BlockPos(0,0,0)), "heat beyond three blocks does not count");
sourceX = 1; sourceLayer = BlockLayersAccess.Fluid;
Check(EggHeat.HasNearbyHeat(heatWorld, new BlockPos(0,0,0)), "fluid heat sources are detected");
heatBlock.Strength = 0;
Check(!EggHeat.HasNearbyHeat(heatWorld, new BlockPos(0,0,0)), "inactive heat sources do not count");
foreach (var eggCode in new[] { "griffon-egg", "griffon-egg-hatching-1", "griffon-egg-hatching-2" })
{
    var eggConfig = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/" + eggCode + ".json")))!;
    foreach (var side in new[] { "client", "server" })
        Check((bool)eggConfig[side]!["behaviors"]!.AsArray().Single(b => (string?)b?["code"] == "fswegg")!["requiresHeat"]!, "heat requirement and tooltip configured for " + eggCode + " " + side);
}
var seatGriffon = Companion(1201);
seatGriffon.Code = new AssetLocation("feverstonewilds:griffon-tamed-male");
var selectBoxes = new EntityBehaviorSelectionBoxes(seatGriffon);
selectBoxes.selectionBoxes = new[] {
    new AttachmentPointAndPose { AttachPoint = new AttachmentPoint { Code = "MidAP" } },
    new AttachmentPointAndPose { AttachPoint = new AttachmentPoint { Code = "FaceAP" } }
};
seatGriffon.SidedProperties.Behaviors.Add(selectBoxes);
var rideable = new FeverstoneWilds.Flight.Behavior.BehaviorFlightRideable(seatGriffon);
rideable.Seats = new[] {
    Fake.Make<IMountableSeat>((m,a) => m.Name == "get_Config" ? new SeatConfig { APName = "Rider", SelectionBox = "MidAP", Controllable = true } : Fake.Default(m.ReturnType)),
    Fake.Make<IMountableSeat>((m,a) => m.Name == "get_Config" ? new SeatConfig { APName = "FaceAP", SelectionBox = "GriffonCarry", Controllable = false } : Fake.Default(m.ReturnType))
};
var selectionPolicy = rideable.GetType().GetMethod("IsRiderSelection", BindingFlags.Instance | BindingFlags.NonPublic)!;
Check((bool)selectionPolicy.Invoke(rideable, new object[] { 1 })!, "back saddle remains available for manual mounting");
Check(!(bool)selectionPolicy.Invoke(rideable, new object[] { 2 })!, "face pickup seat is excluded from manual mounting and its prompt");
Check(!(bool)selectionPolicy.Invoke(rideable, new object[] { 0 })!, "body clicks cannot fall back to the pickup seat");
var direwolfRideable = new FeverstoneWilds.Commands.DirewolfRideable(seatGriffon) { Seats = rideable.Seats };
Check(direwolfRideable.IsDriverSelection(1) && !direwolfRideable.IsDriverSelection(2) && !direwolfRideable.IsDriverSelection(0),
    "direwolf mouth seat is reserved for abilities while saddle remains selectable");
direwolfRideable.Seats = new[] {
    Fake.Make<IMountableSeat>((m,a) => m.Name switch {
        "get_Config" => new SeatConfig { Controllable = false, APName = "FaceAP" },
        "get_Passenger" => abilityPlayer,
        _ => Fake.Default(m.ReturnType)
    }),
    Fake.Make<IMountableSeat>((m,a) => m.Name switch {
        "get_Config" => new SeatConfig { Controllable = true, APName = "Rider" },
        "get_Passenger" => abilityRider,
        _ => Fake.Default(m.ReturnType)
    })
};
seatGriffon.SidedProperties.Behaviors.Add(direwolfRideable);
Check(ReferenceEquals(FeverstoneWilds.Commands.GriffonMountedAbilities.GetDriver(seatGriffon), abilityRider),
    "carry lifecycle finds native direwolf driver and ignores the mouth passenger");
var direwolfConfig = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/tame-direwolf.json")))!;
foreach (string side in new[] { "client", "server" })
    Check(direwolfConfig[side]!["behaviors"]!.AsArray().Count(b => (string?)b?["code"] == "direwolfrideable") == 1 &&
        !direwolfConfig[side]!["behaviors"]!.AsArray().Any(b => (string?)b?["code"] == "rideable"),
        "direwolf uses one ability-aware riding behavior on " + side);
var wolfSeats = direwolfConfig["behaviorConfigs"]!["direwolfrideable"]!["seats"]!.AsArray();
Check(wolfSeats.Count == 2 && (string?)wolfSeats[0]!["apName"] == "Rider" && (bool)wolfSeats[0]!["controllable"]! &&
    (string?)wolfSeats[1]!["apName"] == "FaceAP" && !(bool)wolfSeats[1]!["controllable"]!,
    "direwolf retains driver seat and adds noncontrolling mouth carry seat");
Check(BehaviorGriffonGrow.GrowthProgress(100, 100, 216) == 0, "new growth stage starts at its base size");
Check(BehaviorGriffonGrow.GrowthProgress(208, 100, 216) == 0.5f, "saved stage start restores halfway size");
Check(BehaviorGriffonGrow.GrowthProgress(316, 100, 216) == 1, "nine-day stage reaches full growth");
Check(BehaviorGriffonGrow.GrowthProgress(400, 100, 216) == 1, "delayed transformation cannot grow past stage maximum");
Check(BehaviorGriffonGrow.GrowthProgress(90, 100, 216) == 0, "clock reversal cannot shrink below starting size");
foreach (var (stage, size, factor) in new[] { ("cub", 1.0, 0.45), ("juvenile", 1.45, 1.940692), ("adolescent", 0.8, 0.2) })
{
    var config = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/griffon-" + stage + ".json")))!;
    double actualSize = (double?)config["client"]!["size"] ?? 1;
    double actualFactor = (double)(config["client"]!["sizeGrowthFactor"] ?? config["client"]!["sizeGrowthFactorByType"]!["*"])!;
    Check(Math.Abs(actualSize - size) < 0.0001 && Math.Abs(actualFactor - factor) < 0.0001,
        stage + " has its configured starting size and gradual growth");
}
var genderCub = Mother(1301);
genderCub.Api = api;
genderCub.Code = new AssetLocation("feverstonewilds:griffon-cub-tamed");
genderCub.WatchedAttributes.SetString("fswGriffonGender", "female");
var genderGrowth = new BehaviorGriffonGrow(genderCub);
genderGrowth.Initialize(new EntityProperties(), Vintagestory.API.Datastructures.JsonObject.FromJson(
    "{\"hoursToGrow\":216,\"adultEntityCodes\":[\"feverstonewilds:griffon-juvenile-tamed-male\",\"feverstonewilds:griffon-juvenile-tamed-female\"]}"));
Check(genderGrowth.AdultEntityCodes.Length == 1 && genderGrowth.AdultEntityCodes[0].Path == "griffon-juvenile-tamed-female", "displayed cub gender determines juvenile gender");
Check(genderCub.WatchedAttributes.GetString("fswGriffonGender") == "female", "saved cub gender survives growth behavior initialization");
var growthClientWorld = Fake.Make<IWorldAccessor>((m,a) => m.Name switch {
    "get_Side" => EnumAppSide.Client, "get_Calendar" => calendar, _ => Fake.Default(m.ReturnType)
});
var visibleYoung = new EntityAgent { World = growthClientWorld, Code = new AssetLocation("feverstonewilds:griffon-juvenile-tamed-female") };
var visualProperties = new EntityProperties { Client = new EntityClientProperties([], new()) { Size = 1.45f } };
typeof(Entity).GetProperty("Properties")!.SetValue(visibleYoung, visualProperties);
var visibleGrowTree = new TreeAttribute();
visibleGrowTree.SetDouble("timeSpawned", days * 24);
visibleYoung.WatchedAttributes.SetAttribute("grow", visibleGrowTree);
var visualGrowth = new BehaviorGriffonGrowthSize(visibleYoung);
visualGrowth.Initialize(visualProperties, Vintagestory.API.Datastructures.JsonObject.FromJson("{\"baseSize\":1.45,\"growthSize\":1.940692,\"hoursToGrow\":216}"));
visualGrowth.OnEntitySpawn();
Check(Math.Abs(visualProperties.Client.Size - 1.45f) < 0.0001, "client renders initial juvenile size");
days += 4.5;
visualGrowth.OnGameTick(0.1f);
Check(Math.Abs(visualProperties.Client.Size - 2.420346f) < 0.0001, "client visibly grows halfway through the saved stage even without age-listener events");
visualProperties.Client.Size = 1;
visualGrowth.OnEntityLoaded();
Check(Math.Abs(visualProperties.Client.Size - 2.420346f) < 0.0001, "client restores intermediate size on reload");
days += 4.5;
visualGrowth.OnGameTick(0.1f);
Check(Math.Abs(visualProperties.Client.Size * 7.964001 - 0.8 * 33.754357) < 0.001, "juvenile end height matches adolescent starting height across different models");
var clutchBlock = new BlockAnimalNest { Code = new AssetLocation("feverstonewilds:griffonnest-ready"),
    Attributes = Vintagestory.API.Datastructures.JsonObject.FromJson("{\"constructionStage\":0,\"quantitySlots\":3}") };
var clutchNest = new TestNest(); clutchNest.Setup(api, clutchBlock);
var clutch = new GriffonNestClutch(clutchNest);
var surfaceBlock = new Block { CollisionBoxes = new[] { new Cuboidf(0.1f, 0, 0.1f, 0.9f, 0.12f, 0.9f) } };
var surfaceNest = new TestNest(); surfaceNest.Setup(api, surfaceBlock);
var broodSurface = GriffonBrooding.SurfacePosition(surfaceNest);
Check(Math.Abs(broodSurface.X - 0.5) < 0.00001 && Math.Abs(broodSurface.Z - 0.5) < 0.00001 &&
    Math.Abs(broodSurface.Y - 0.121) < 0.00001, "griffon approach targets nest center at collision surface height");
Check(!GriffonBrooding.OnNest(new Vec3d(1.2, broodSurface.Y, 0.5), broodSurface),
    "griffon beside nest cannot brood");
Check(!GriffonBrooding.OnNest(new Vec3d(0.5, 0, 0.5), broodSurface),
    "brooding requires standing on the nest surface rather than the ground below it");
Check(!GriffonBrooding.OnNest(new Vec3d(0.5, broodSurface.Y - 1, 0.5), broodSurface) &&
    !GriffonBrooding.OnNest(new Vec3d(0.5, broodSurface.Y + 1, 0.5), broodSurface),
    "griffon on another floor cannot brood through nest");
var settlingMother = Mother(55);
settlingMother.OnGround = true;
settlingMother.Pos.SetPos(0.55, 0, 0.45);
settlingMother.Pos.Motion.Set(1, 0, 1);
Check(GriffonBrooding.TrySettle(settlingMother, broodSurface) &&
    settlingMother.Pos.XYZ.SquareDistanceTo(broodSurface) < 0.000001 &&
    settlingMother.Pos.Motion.X == 0 && settlingMother.Pos.Motion.Z == 0,
    "arrived brooder settles centrally on nest surface and stops walking");
settlingMother.Pos.SetPos(2, 0, 0.5);
Check(!GriffonBrooding.TrySettle(settlingMother, broodSurface) && settlingMother.Pos.X == 2,
    "distant griffon is not pulled onto nest");
settlingMother.Pos.SetPos(0.5, broodSurface.Y, 0.5);
settlingMother.OnGround = false;
Check(!GriffonBrooding.TrySettle(settlingMother, broodSurface), "airborne griffon cannot begin brooding");
var motherOneNest = new GriffonClutchNest(clutchNest, clutch, mother);
var motherTwoNest = new GriffonClutchNest(clutchNest, clutch, secondMother);
Check(motherOneNest.TryAddEgg(Egg()), "first egg establishes clutch owner");
Check(clutch.MotherId == mother.EntityId && !motherTwoNest.TryAddEgg(Egg()), "second female cannot mix eggs into a partial clutch");
motherOneNest.SetOccupier(mother);
motherOneNest.SetOccupier(null);
Check(!clutch.CanUse(secondMother), "leaving the nest does not release clutch ownership");
Check(motherOneNest.TryAddEgg(Egg()) && motherOneNest.TryAddEgg(Egg()), "same mother can complete her three-egg clutch");
var clutchSave = new TreeAttribute(); clutch.ToTreeAttributes(clutchSave);
var restoredClutch = new GriffonNestClutch(clutchNest); restoredClutch.FromTreeAttributes(clutchSave, world);
Check(restoredClutch.MotherId == mother.EntityId && !restoredClutch.CanUse(secondMother), "clutch ownership persists through reload");
clutchNest.Inventory[0].Itemstack = null;
Check(!clutch.CanLay(mother) && !clutch.CanLay(secondMother), "partly hatched or collected full clutch cannot be topped up by either female");
clutchNest.Inventory[1].Itemstack = null; clutchNest.Inventory[2].Itemstack = null;
Check(motherTwoNest.TryAddEgg(Egg()) && clutch.MotherId == secondMother.EntityId, "empty nest admits a fresh clutch from another female");
clutchNest.Inventory[0].Itemstack = null;
Check(motherTwoNest.TryAddEgg(Egg()) && motherTwoNest.TryAddEgg(Egg()), "new owner can add the next clutch's eggs");
clutchNest.Inventory[0].Itemstack = null;
Check(!clutch.CanLay(secondMother), "removing an egg from an unfinished clutch also prevents refilling that clutch");
var legacyClutch = new GriffonNestClutch(clutchNest);
Check(!legacyClutch.CanLay(mother), "existing eggs without owner data are protected against new additions");
legacyClutch.ClaimBrooding(mother);
Check(legacyClutch.CanUse(mother) && !legacyClutch.CanUse(secondMother), "first brooder adopts an existing clutch exclusively");
var domesticConfig = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "blocktypes/wood/griffonnest.json")))!;
Check(domesticConfig["entityBehaviors"]!.AsArray().Any(b => (string?)b?["name"] == "GriffonNestClutch"), "clutch behavior attaches to existing vanilla henbox nests");
var fallbackConfig = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "blocktypes/food/egg-griffon.json")))!;
var fallbackTask = griffon["server"]!["behaviors"]!.AsArray().Single(b => (string?)b?["code"] == "taskai")!["aitasks"]!.AsArray().Single(t => (string?)t?["code"] == "griffonseeknestandlay")!;
Check((string?)fallbackTask["failBlockCode"] == "feverstonewilds:" + (string?)fallbackConfig["code"], "ground fallback resolves to the griffon egg block");
Check((string?)fallbackConfig["drops"]![0]!["code"] == "feverstonewilds:egg-griffon-infertile", "ground egg collection returns the separate infertile food egg");
Check(fallbackConfig["behaviors"]!.AsArray().Any(b => (string?)b?["name"] == "RightClickPickup"), "ground eggs support vanilla right-click collection");
Check((string?)fallbackConfig["attributes"]!["transientProps"]!["inGameHours"] == "72", "ground fallback uses vanilla three-day expiration");
var vanillaGroundFallback = (Delegate)typeof(AiTaskGriffonSeekNestAndLay).GetField("GroundFallback", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
Check(vanillaGroundFallback.Method.DeclaringType == typeof(AiTaskSeekBlockAndLay), "clutch-locked nest fallback binds to the installed vanilla ground-laying method");
var infertileEggConfig = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "itemtypes/food/egg-griffon-infertile.json")))!;
Check((int)infertileEggConfig["maxStackSize"]! == 32, "infertile food eggs stack to 32");
Check((string?)infertileEggConfig["class"] == "Item" && infertileEggConfig["entityCode"] == null &&
    infertileEggConfig["attributes"]!["entityCode"] == null && infertileEggConfig["attributes"]!["incubationDays"] == null,
    "infertile food eggs have no egg placement or hatching behavior");
Check((int)infertileEggConfig["nutritionProps"]!["satiety"]! > 0 &&
    (string?)infertileEggConfig["nutritionProps"]!["foodcategory"] == "Protein", "infertile eggs are edible protein food");
var nestEggConfig = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "itemtypes/creature/egg-griffon.json")))!;
Check((string?)nestEggConfig["class"] == "FswEgg" && (int)nestEggConfig["maxStackSize"]! == 1,
    "nest egg item retains separate incubation placement and individual progress");
Check(typeof(AiTaskFleeEntity).IsAssignableFrom(typeof(AiTaskFleeGriffon)),
    "griffon avoidance uses vanilla fleeing and pathfinding");
var scaryGriffon = Mother(9901);
foreach (string species in new[] { "sheep", "goat", "deer", "hare", "rabbit", "fox", "raccoon", "wolf" })
{
    foreach (string stage in new[] { "adult", "baby" })
    {
        var preyAnimal = Mother(9902);
        preyAnimal.Code = new AssetLocation("game:" + species + "-" + stage);
        Check(GriffonAvoidanceSystem.IsAffectedAnimal(preyAnimal), $"{species} {stage} gets griffon avoidance");
        foreach (int generation in new[] { 0, 1, 10, 100 })
        {
            preyAnimal.WatchedAttributes.SetInt("generation", generation);
            foreach (string griffonStage in new[] { "griffon-wild-female", "griffon-cub-wild", "griffon-juvenile-wild-female", "griffon-adolescent-wild-male" })
            {
                scaryGriffon.Code = new AssetLocation("feverstonewilds:" + griffonStage);
                bool expectedFear = !griffonStage.Contains("-cub-") && !griffonStage.Contains("-juvenile-") &&
                    !(species == "wolf" && griffonStage.Contains("-adolescent-"));
                Check(GriffonAvoidanceSystem.ShouldFear(preyAnimal, scaryGriffon) == expectedFear,
                    $"{species} {stage} generation {generation} applies wild griffon age rules for {griffonStage}");
            }
            foreach (string griffonStage in new[] { "griffon-tamed-female", "griffon-cub-tamed", "griffon-juvenile-tamed-male", "griffon-adolescent-tamed-female" })
            {
                scaryGriffon.Code = new AssetLocation("feverstonewilds:" + griffonStage);
                bool expectedFear = generation == 0 && !griffonStage.Contains("-cub-") && !griffonStage.Contains("-juvenile-") && !(species == "wolf" && griffonStage.Contains("-adolescent-"));
                Check(GriffonAvoidanceSystem.ShouldFear(preyAnimal, scaryGriffon) == expectedFear,
                    $"{species} {stage} generation {generation} applies tame griffon exception");
            }
        }
    }
}
var fearPrey = Mother(9903); fearPrey.Code = new AssetLocation("game:sheep-bighorn-adult-male");
foreach (string harmless in new[] { "feverstonewilds:griffon-egg", "feverstonewilds:griffon-egg-hatching-1",
    "feverstonewilds:wild-griffon-nest-eggs", "game:griffon-wild-male", "game:wolf-male" })
{
    scaryGriffon.Code = new AssetLocation(harmless);
    Check(!GriffonAvoidanceSystem.ShouldFear(fearPrey, scaryGriffon), "griffon avoidance ignores " + harmless);
}
scaryGriffon.Code = new AssetLocation("feverstonewilds:griffon-wild-male");
scaryGriffon.Alive = false;
Check(!GriffonAvoidanceSystem.ShouldFear(fearPrey, scaryGriffon), "animals do not flee griffon corpses");
scaryGriffon.Alive = true;
fearPrey.Code = new AssetLocation("game:pig-wild-male");
Check(!GriffonAvoidanceSystem.ShouldFear(fearPrey, scaryGriffon), "unrequested species retain existing behavior");
var animationAnimal = Mother(9904);
typeof(Entity).GetProperty("Properties")!.SetValue(animationAnimal, new EntityProperties {
    Client = new EntityClientProperties([], new()) {
        Animations = new[] { new AnimationMetaData { Code = "Canter", Animation = "Canter" } }
    }
});
Check(GriffonAvoidanceSystem.SelectAnimation(animationAnimal) == "Canter", "fleeing uses available native locomotion animation");
Console.WriteLine($"Passed {passed} nesting, companion, ability, tracking and wounded-state checks.");

public class Fake : DispatchProxy
{
    public System.Func<MethodInfo, object?[], object?> Handler = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args ?? []);
    public static T Make<T>(System.Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T result = Create<T, Fake>();
        ((Fake)(object)result).Handler = handler;
        return result;
    }
    public static object? Default(Type type) => type == typeof(void) ? null : type.IsValueType ? Activator.CreateInstance(type) : null;
}
public class TestHeatBlock : Block, IHeatSource
{
    public float Strength;
    public float GetHeatStrength(IWorldAccessor world, BlockPos source, BlockPos receiver) => Strength;
}
public class TestNest : BlockEntityFswBreedingNest
{
    public void Setup(ICoreAPI api, Block block)
    {
        Api = api; Block = block; Pos = new BlockPos(0, 0, 0);
        inventory = new InventoryGeneric(3, "testnest", "1", null); inventory.Api = api;
    }
    public double RemainingDays => timeToIncubate;
    public void Tick() => On1500msTick(1.5f);
}
public class TestReproduction(Entity entity) : EntityBehaviorFswReproduction(entity)
{
    public void Setup()
    {
        multiplyTree = new TreeAttribute();
        MultiplyCooldownDaysMin = 5; MultiplyCooldownDaysMax = 12;
    }
}
