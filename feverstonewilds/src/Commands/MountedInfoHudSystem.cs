using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace FeverstoneWilds.Commands;

public class MountedInfoHudSystem : ModSystem
{
    const string PatchId = "feverstonewilds.mountedinfohud";
    static ICoreClientAPI clientApi;
    Harmony patches;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        // Resolve the client-only HUD at runtime so dedicated servers need no client assembly.
        var hud = AccessTools.TypeByName("Vintagestory.Client.NoObf.HudElementBlockAndEntityInfo");
        var render = hud == null ? null : AccessTools.DeclaredMethod(hud, "ShouldReceiveRenderEvents");
        if (render == null) throw new InvalidOperationException("Cannot find the mounted information HUD render hook.");
        patches = new Harmony(PatchId);
        patches.Patch(render, postfix: new HarmonyMethod(typeof(MountedInfoHudSystem), nameof(FilterMountInfo)));
    }

    public static bool ShouldHide(EntityAgent rider, Entity viewed)
    {
        Entity mount = rider?.MountedOn?.Entity;
        return mount != null && viewed != null && mount.Code?.Domain == "feverstonewilds"
            && mount.EntityId == viewed.EntityId;
    }

    static void FilterMountInfo(Entity ___currentEntity, ref bool __result)
    {
        if (__result && ShouldHide(clientApi?.World.Player?.Entity, ___currentEntity)) __result = false;
    }

    public override void Dispose()
    {
        patches?.UnpatchAll(PatchId);
        clientApi = null;
        base.Dispose();
    }
}
