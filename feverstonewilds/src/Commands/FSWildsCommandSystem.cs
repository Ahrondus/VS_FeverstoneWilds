using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Commands;

public class FSWildsCommandSystem : ModSystem
{
    const string PatchId = "feverstonewilds.commands";
    Harmony harmony;
    public override void Start(ICoreAPI api)
    {
        api.RegisterEntityBehaviorClass("fswildsbehavior", typeof(FSWildsBehavior));
        api.RegisterEntityBehaviorClass("fswcommandgrow", typeof(FswCommandGrow));
        if (api is ICoreServerAPI server)
        {
            server.RegisterAiTask<AiTaskFswFollowOwner>("fswfollowowner");
            server.RegisterAiTask<AiTaskFswCompanionAssist>("fswcompanionassist");
            harmony = InstallCombatHooks();
        }
    }
    public static Harmony InstallCombatHooks()
    {
        var patches = new Harmony(PatchId);
        try
        {
            patches.Patch(AccessTools.Method(typeof(EntityAgent), "DidAttack", new[] { typeof(DamageSource), typeof(EntityAgent) }), postfix: new HarmonyMethod(typeof(FswCompanionCombatHooks), nameof(FswCompanionCombatHooks.DidAttackPostfix)));
            patches.Patch(AccessTools.Method(typeof(EntityPlayer), "OnHurt", new[] { typeof(DamageSource), typeof(float) }), postfix: new HarmonyMethod(typeof(FswCompanionCombatHooks), nameof(FswCompanionCombatHooks.PlayerHurtPostfix)));
            patches.Patch(AccessTools.Method(typeof(Entity), "ReceiveDamage", new[] { typeof(DamageSource), typeof(float) }), prefix: new HarmonyMethod(typeof(FSWildsCommandSystem), nameof(ProtectTargets)));
            return patches;
        }
        catch { patches.UnpatchAll(PatchId); throw; }
    }
    static bool ProtectTargets(Entity __instance, DamageSource damageSource, float damage, ref bool __result)
    {
        if (damage <= 0 || damageSource?.Type == EnumDamageType.Heal) return true;
        Entity attacker = damageSource?.GetCauseEntity() ?? damageSource?.SourceEntity;
        if (attacker?.GetBehavior<FSWildsBehavior>() == null || string.IsNullOrEmpty(attacker.WatchedAttributes.GetString("fswCompanionOwnerUid"))) return true;
        if (FSWildsBehavior.CanAttack(attacker, __instance)) return true;
        __result = false;
        return false;
    }
    public override void Dispose() { harmony?.UnpatchAll(PatchId); base.Dispose(); }
}

public class FswCommandGrow : EntityBehaviorGrow
{
    public FswCommandGrow(Entity entity) : base(entity) { }
    protected override void BecomeAdult(Entity adult, bool keepTextureIndex)
    {
        FSWildsBehavior.CopyState(entity, adult);
        FswCompanionMapSystem.PrepareGrowth(entity);
        base.BecomeAdult(adult, keepTextureIndex);
    }
}
