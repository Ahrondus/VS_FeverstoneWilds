using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace FeverstoneWilds.Compatibility;

// No reference to Primitive Survival: discover and patch its methods only when installed.
public class PrimitiveSurvivalFishingCompat : ModSystem
{
    private const string PatchId = "feverstonewilds.primitivesurvival.fishing";
    private static readonly object Sync = new object();
    private static Harmony harmony;
    private static int users;
    private static ICoreClientAPI clientApi;
    private bool attached;
    private bool clientSide;

    internal static readonly Dictionary<string, string> FishItems = new Dictionary<string, string>
    {
        ["feverstonewilds:discus"] = "feverstonewilds:fsw-fishraw-reef-discus",
        ["feverstonewilds:buromenfish-freshwater"] = "feverstonewilds:fsw-fishraw-freshwater-buromenfish",
        ["feverstonewilds:buromenfish-saltwater"] = "feverstonewilds:fsw-fishraw-saltwater-buromenfish",
        ["feverstonewilds:ghostfish-freshwater"] = "feverstonewilds:fsw-fishraw-freshwater-ghostfish",
        ["feverstonewilds:ghostfish-saltwater"] = "feverstonewilds:fsw-fishraw-saltwater-ghostfish",
        ["feverstonewilds:eel-freshwater"] = "feverstonewilds:fsw-fishraw-freshwater-eel",
        ["feverstonewilds:eel-saltwater"] = "feverstonewilds:fsw-fishraw-saltwater-eel"
    };

    public override void Start(ICoreAPI api)
    {
        if (!api.ModLoader.IsModEnabled("primitivesurvival")) return;
        lock (Sync)
        {
            if (harmony == null)
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "primitivesurvival");
                var spear = assembly?.GetType("PrimitiveSurvival.ModSystem.ItemFishingSpear");
                var rendered = assembly?.GetType("PrimitiveSurvival.ModSystem.ItemSpearFromAttributes");
                var hit = spear == null ? null : AccessTools.DeclaredMethod(spear, "ItemSpear_OnBeginHitEntity",
                    new[] { typeof(EntityAgent), typeof(EnumHandling).MakeByRefType() });
                var remove = spear == null ? null : AccessTools.DeclaredMethod(spear, "OnHeldInteractStop",
                    new[] { typeof(float), typeof(ItemSlot), typeof(EntityAgent), typeof(BlockSelection), typeof(EntitySelection) });
                var mesh = rendered == null ? null : AccessTools.DeclaredMethod(rendered, "GenMesh",
                    new[] { typeof(ItemStack), typeof(ITextureAtlasAPI) });
                if (hit == null || remove == null || mesh == null || mesh.ReturnType != typeof(MeshData))
                {
                    api.Logger.Warning("[FSW] Primitive Survival fishing compatibility disabled: unsupported spear methods.");
                    return;
                }
                var patches = new Harmony(PatchId);
                try
                {
                    patches.Patch(hit, prefix: new HarmonyMethod(typeof(PrimitiveSurvivalFishingCompat), nameof(CatchFish)));
                    patches.Patch(remove, prefix: new HarmonyMethod(typeof(PrimitiveSurvivalFishingCompat), nameof(RemoveFish)));
                    patches.Patch(mesh, prefix: new HarmonyMethod(typeof(PrimitiveSurvivalFishingCompat), nameof(RenderFish)));
                    harmony = patches;
                }
                catch (Exception ex)
                {
                    patches.UnpatchAll(PatchId);
                    api.Logger.Warning("[FSW] Primitive Survival fishing compatibility disabled: {0}", ex.Message);
                    return;
                }
            }
            users++;
            attached = true;
        }
        api.Logger.Notification("[FSW] Optional Primitive Survival fishing compatibility enabled.");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        if (!attached) return;
        clientApi = api;
        clientSide = true;
    }

    public override void Dispose()
    {
        lock (Sync)
        {
            if (clientSide) clientApi = null;
            if (!attached) return;
            attached = false;
            if (--users == 0)
            {
                harmony?.UnpatchAll(PatchId);
                harmony = null;
            }
        }
        base.Dispose();
    }

    private static bool CatchFish(EntityAgent byEntity)
    {
        if (byEntity is not EntityPlayer player || byEntity.World.Side != EnumAppSide.Server) return true;
        var slot = player.ActiveHandItemSlot;
        if (slot?.Itemstack == null || slot.Itemstack.Attributes.GetString("fish", "none") != "none") return true;
        var target = player.EntitySelection?.Entity;
        if (target == null)
        {
            target = byEntity.World.GetNearestEntity(byEntity.Pos.XYZ.AddCopy(0, 1, 0), 1, 1,
                e => e.Code != null && FishItems.ContainsKey(e.Code.ToString()));
        }
        if (target?.Code == null || !FishItems.TryGetValue(target.Code.ToString(), out var itemCode)) return true;
        // All other fish continue through PS's original handler.
        if (byEntity.Attributes.GetInt("didattack") != 0) return false;
        var item = byEntity.World.GetItem(new AssetLocation(itemCode));
        if (item == null) return false; // Never remove an entity without a resolvable catch.
        byEntity.Attributes.SetInt("didattack", 1);
        slot.Itemstack.Attributes.SetString("fish", target.Code.ToString());
        slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot, 1);
        if (slot.Itemstack == null)
        {
            // A spear breaking on the catch must not destroy the fish item.
            byEntity.World.SpawnItemEntity(new ItemStack(item), byEntity.Pos.XYZ.AddCopy(0, 0.5, 0));
        }
        slot.MarkDirty();
        ((IServerWorldAccessor)byEntity.World).DespawnEntity(target,
            new EntityDespawnData { Reason = EnumDespawnReason.Removed });
        byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/arrow-impact"), byEntity);
        return false;
    }

    private static bool RemoveFish(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var fish = slot?.Itemstack?.Attributes.GetString("fish", "none");
        if (fish == null || !FishItems.TryGetValue(fish, out var itemCode)) return true;
        if (secondsUsed < 0.35f) return false;
        if (byEntity.World.Side == EnumAppSide.Server)
        {
            var item = byEntity.World.GetItem(new AssetLocation(itemCode));
            if (item == null) return false; // Preserve the stored catch if an asset is missing.
            byEntity.World.SpawnItemEntity(new ItemStack(item), byEntity.Pos.XYZ.AddCopy(0, 0.5, 0));
            slot.Itemstack.Attributes.SetString("fish", "none");
            slot.MarkDirty();
        }
        // The server synchronizes the cleared attribute; PS invalidates its mesh on change.
        return false;
    }

    private static bool RenderFish(object __instance, MethodBase __originalMethod,
        ItemStack itemstack, ITextureAtlasAPI targetAtlas, ref MeshData __result)
    {
        var fish = itemstack.Attributes.GetString("fish", "none");
        if (!FishItems.TryGetValue(fish, out var itemCode) || clientApi == null) return true;
        // Re-enter with an empty clone: the prefix falls through and PS builds the original spear.
        var empty = itemstack.Clone();
        empty.Attributes.SetString("fish", "none");
        __result = (MeshData)__originalMethod.Invoke(__instance, new object[] { empty, targetAtlas });
        var item = clientApi.World.GetItem(new AssetLocation(itemCode));
        if (item == null) return false;
        var textures = new Dictionary<string, AssetLocation>();
        foreach (var texture in item.Textures)
            textures[texture.Key] = texture.Value.Baked?.BakedName ?? texture.Value.Base;
        var source = new ContainedTextureSource(clientApi, targetAtlas, textures, "fsw-fish-on-spear");
        clientApi.Tesselator.TesselateItem(item, out var fishMesh, source);
        if (fishMesh != null)
        {
            var transform = Mat4f.Create();
            Mat4f.Translate(transform, transform, 0.8f, 0.6f, 0.65f);
            Mat4f.RotateX(transform, transform, -1.5708f);
            Mat4f.RotateY(transform, transform, 1.5708f);
            fishMesh.MatrixTransform(transform);
            __result.AddMeshData(fishMesh);
        }
        return false;
    }
}
