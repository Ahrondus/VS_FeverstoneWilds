using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FeverstoneWilds;

public class GriffonAvoidanceSystem : ModSystem
{
    ICoreServerAPI server;
    static readonly string[] Species = { "sheep", "goat", "deer", "hare", "rabbit", "fox", "raccoon", "wolf" };

    public override void Start(ICoreAPI api)
    {
        if (api is ICoreServerAPI sapi) sapi.RegisterAiTask<AiTaskFleeGriffon>("fswfleegriffon");
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        server = api;
        api.Event.OnEntitySpawn += AddAvoidance;
        api.Event.OnEntityLoaded += AddAvoidance;
    }

    public static bool IsAffectedAnimal(Entity animal) => animal?.Code?.Domain == "game" &&
        Species.Any(species => animal.Code.Path.StartsWith(species + "-", StringComparison.Ordinal));

    public static bool ShouldFear(Entity animal, Entity griffon)
    {
        if (!IsAffectedAnimal(animal) || griffon == null || !griffon.Alive ||
            griffon.Code?.Domain != "feverstonewilds" || griffon.Pos.Dimension != animal.Pos.Dimension) return false;
        string[] parts = griffon.Code.Path.Split('-');
        if (parts.Length < 3 || parts[0] != "griffon") return false;
        if (parts.Contains("cub") || parts.Contains("juvenile")) return false;
        if (parts.Contains("adolescent") && animal.Code.Path.StartsWith("wolf-", StringComparison.Ordinal)) return false;
        // Living griffon stages use wild/tamed codes; egg and nest entities do not.
        if (parts.Contains("wild")) return true;
        return parts.Contains("tamed") && animal.WatchedAttributes.GetInt("generation", 0) < 1;
    }

    public static string SelectAnimation(Entity animal)
    {
        var animations = animal.Properties?.Client?.Animations;
        foreach (string name in new[] { "run", "sprint", "canter", "gallop", "hop", "walk" })
        {
            var animation = animations?.FirstOrDefault(a => string.Equals(a.Code, name, StringComparison.OrdinalIgnoreCase));
            if (animation != null) return animation.Code;
        }
        return null;
    }

    public static void AddAvoidance(Entity animal)
    {
        if (!IsAffectedAnimal(animal) || animal is not EntityAgent agent) return;
        var ai = animal.GetBehavior<EntityBehaviorTaskAI>();
        if (ai?.PathTraverser == null || ai.TaskManager.AllTasks.Any(task => task is AiTaskFleeGriffon)) return;
        var config = new JsonObject(new JObject {
            ["code"] = "fswfleegriffon", ["priority"] = 3.5, ["priorityForCancel"] = 3.5,
            ["movespeed"] = 0.05, ["seekingRange"] = 18, ["fleeingDistance"] = 28,
            ["fleeDurationMs"] = 9000, ["mincooldown"] = 1000, ["maxcooldown"] = 2000,
            ["entityCodes"] = new JArray("griffon-*"), ["retaliateAttacks"] = false,
            ["animation"] = SelectAnimation(animal), ["animationSpeed"] = 1.2
        });
        var task = new AiTaskFleeGriffon(agent, config, JsonObject.FromJson("{}"));
        task.AfterInitialize();
        ai.TaskManager.AddTask(task);
    }

    public override void Dispose()
    {
        if (server != null)
        {
            server.Event.OnEntitySpawn -= AddAvoidance;
            server.Event.OnEntityLoaded -= AddAvoidance;
        }
        base.Dispose();
    }
}

public class AiTaskFleeGriffon : AiTaskFleeEntity
{
    public AiTaskFleeGriffon(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
        : base(entity, taskConfig, aiConfig)
    {
        // Vanilla progressively reduces sensing range with generation. Wild griffons
        // must remain frightening at every generation; filter tame griffons separately.
        tamingGenerations = float.MaxValue;
    }

    public override bool IsTargetableEntity(Entity candidate, float range) =>
        GriffonAvoidanceSystem.ShouldFear(entity, candidate) && base.IsTargetableEntity(candidate, range);
    public override bool IsTargetableEntityNoTagsAll(Entity candidate, float range) =>
        GriffonAvoidanceSystem.ShouldFear(entity, candidate) && base.IsTargetableEntityNoTagsAll(candidate, range);
    public override bool IsTargetableEntityNoTagsNoAll(Entity candidate, float range) =>
        GriffonAvoidanceSystem.ShouldFear(entity, candidate) && base.IsTargetableEntityNoTagsNoAll(candidate, range);
    public override bool ContinueExecute(float dt) =>
        GriffonAvoidanceSystem.ShouldFear(entity, targetEntity) && base.ContinueExecute(dt);
}
