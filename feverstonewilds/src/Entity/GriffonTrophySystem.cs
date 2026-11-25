using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace FeverstoneWilds;

// Mounting and item persistence are handled entirely by vanilla antler mounts.
public class GriffonTrophySystem : ModSystem
{
    const string PatchId = "feverstonewilds.griffontrophies";
    Harmony patches;

    public override void StartServerSide(Vintagestory.API.Server.ICoreServerAPI api)
    {
        patches = new Harmony(PatchId);
        patches.Patch(AccessTools.Method(typeof(EntityBehaviorHarvestable), "GenerateDrops"),
            prefix: new HarmonyMethod(typeof(GriffonTrophySystem), nameof(BeforeDrops)),
            postfix: new HarmonyMethod(typeof(GriffonTrophySystem), nameof(AfterDrops)));
    }

    static void BeforeDrops(EntityBehaviorHarvestable __instance, out bool __state)
        => __state = __instance.DropsGenerated;

    static void AfterDrops(EntityBehaviorHarvestable __instance, bool __state)
    {
        var entity = __instance.entity;
        if (__state || !__instance.DropsGenerated || entity.Alive || entity.Code.Domain != "feverstonewilds") return;
        string code = TrophyCode(entity.Code.Path, entity.WatchedAttributes.GetInt("textureIndex"),
            entity.WatchedAttributes.GetInt(BehaviorGriffonMarkings.EyeKey),
            entity.WatchedAttributes.GetBool(BehaviorGriffonMarkings.SpotsKey));
        if (code == null) return;
        var item = entity.World.GetItem(new AssetLocation("feverstonewilds", code));
        if (item == null)
        {
            entity.World.Logger.Error("Unable to resolve head trophy {0} for {1}", code, entity.Code);
            return;
        }
        var trophy = new ItemStack(item);
        foreach (var slot in __instance.Inventory)
        {
            if (!slot.Empty) continue;
            slot.Itemstack = trophy;
            slot.MarkDirty();
            return;
        }
        // Preserve the head even if another mod fills every harvest slot.
        entity.World.SpawnItemEntity(trophy, entity.Pos.XYZ);
    }

    public static string TrophyCode(string entityCode, int skin, int eye = 0, bool spots = false)
    {
        switch (entityCode)
        {
            case "griffon-wild-male":
            case "griffon-wild-female":
            case "griffon-tamed-male":
            case "griffon-tamed-female":
                return $"griffon-head-{Math.Clamp(skin, 0, 8)}-{Math.Clamp(eye, 0, 6)}-{(spots ? "spotted" : "plain")}";
            case "direwolf-male":
            case "direwolf-female":
            case "tame-direwolf-male":
            case "tame-direwolf-female":
                return $"direwolf-head-{Math.Clamp(skin, 0, 13)}";
            case "hellboar-male":
                return $"hellboar-male-head-{Math.Clamp(skin, 0, 7)}";
            default:
                return null;
        }
    }

    public override void Dispose()
    {
        patches?.UnpatchAll(PatchId);
        base.Dispose();
    }
}
