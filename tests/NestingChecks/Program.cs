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
var griffon = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "entities/land/griffon.json")))!;
var reproductionJson = griffon["server"]!["behaviors"]!.AsArray().Single(x => (string?)x?["code"] == "fsweggreproduction")!;
Check((bool)reproductionJson["enabledByType"]!["*-tamed-female"]! && !(bool)reproductionJson["enabledByType"]!["*"]!,
    "new reproduction enabled only on tamed female griffons");
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
Console.WriteLine($"Passed {passed} nesting checks.");

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
