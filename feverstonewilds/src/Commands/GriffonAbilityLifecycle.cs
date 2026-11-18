using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace FeverstoneWilds.Commands;

public partial class GriffonMountedAbilities
{
    const string GuardId = "feverstonewilds.griffon-grab";
    static readonly object GuardLock = new();
    static Harmony guard;
    static int guardUsers;
    bool guardAttached;
    long clientTickId, serverTickId;

    readonly Dictionary<long, long> nextGrabAtByMount = new();
    readonly Dictionary<long, string> driverByMount = new();

    void RegisterAbilityInput()
    {
        capi.Input.RegisterHotKeyFirst("fsw-griffon-attack", "FS Wilds mount: attack", GlKeys.F, HotkeyType.CharacterControls);
        capi.Input.RegisterHotKeyFirst("fsw-griffon-grab", "FS Wilds mount: grab/release", GlKeys.R, HotkeyType.CharacterControls);
        capi.Input.SetHotKeyHandler("fsw-griffon-attack", _ => RequestAbility(false, true));
        capi.Input.SetHotKeyHandler("fsw-griffon-grab", _ => RequestAbility(true, true));
    }

    long nextInputTrace;
    bool attackHeld, grabHeld;
    void UpdateReleasedAbilityKeys()
    {
        bool IsDown(string code, GlKeys fallback)
        {
            int key = capi.Input.GetHotKeyByCode(code)?.CurrentMapping?.KeyCode ?? (int)fallback;
            return key >= 0 && key < capi.Input.KeyboardKeyStateRaw.Length && capi.Input.KeyboardKeyStateRaw[key];
        }
        if (!IsDown("fsw-griffon-attack", GlKeys.F)) attackHeld = false;
        if (!IsDown("fsw-griffon-grab", GlKeys.R)) grabHeld = false;
    }
    public bool RequestAbility(bool grab, bool fromHotkey = false)
    {
        EntityAgent rider = capi.World.Player?.Entity;
        Entity mount = rider?.MountedOn?.Entity;
        if (capi.IsGamePaused || !CanUseAbilities(mount, rider) || (grab && !CanGrabWithMount(mount)))
        {
            if (capi.World.ElapsedMilliseconds >= nextInputTrace)
            {
                nextInputTrace = capi.World.ElapsedMilliseconds + 2000;
                capi.Logger.Notification("[FSW abilities] Input blocked: mount={0}, controlling={1}, paused={2}", mount?.Code, rider?.MountedOn?.CanControl, capi.IsGamePaused);
            }
            return false;
        }
        // The input dispatcher already handles focused dialogs for CharacterControls hotkeys.
        if (!fromHotkey && !CanRequestAbility()) return false;
        if (grab ? grabHeld : attackHeld) return true;
        if (grab) grabHeld = true; else attackHeld = true;
        capi.Logger.Notification("[FSW abilities] Sending {0} from driver {1} on mount {2}", grab ? "grab" : "attack", rider.EntityId, mount.EntityId);
        // Match PaleoWilds' immediate rider feedback; the server still validates the action.
        mount.StopAnimation("attack");
        mount.StartAnimation("attack");
        if (grab) clientChannel?.SendPacket(new GriffonGrabPacket { Pressed = true });
        else SendBitePacket();
        return true;
    }
    bool CanRequestAbility()
    {
        EntityAgent rider = capi.World.Player?.Entity;
        return !capi.IsGamePaused && !capi.Gui.OpenedGuis.Any(g => g.Focusable && g.ShouldReceiveKeyboardEvents())
            && CanUseAbilities(rider?.MountedOn?.Entity, rider);
    }

    public static EntityAgent GetDriver(Entity mount)
    {
        var rideable = mount?.GetBehavior<Vintagestory.GameContent.EntityBehaviorRideable>();
        return rideable?.Seats.FirstOrDefault(s => s.Config.Controllable && s.Passenger != null)?.Passenger as EntityAgent;
    }

    public static bool IsTamedDirewolf(Entity mount) => mount?.Code?.Domain == "feverstonewilds" &&
        (mount.Code.Path == "tame-direwolf-male" || mount.Code.Path == "tame-direwolf-female");

    public static bool IsAttackOnlyMount(Entity mount) => mount?.Code?.Domain == "feverstonewilds" &&
        (mount.Code.Path == "bison-tamed-male" || mount.Code.Path == "bison-tamed-female" ||
         mount.Code.Path == "tame-ostrich-male" || mount.Code.Path == "tame-ostrich-female");

    public static bool CanGrabWithMount(Entity mount) => IsTamedGriffon(mount) || IsTamedDirewolf(mount);
    public static bool IsAbilityMount(Entity mount) => CanGrabWithMount(mount) || IsAttackOnlyMount(mount);
    public static float MountedAttackDamage(Entity mount) => IsTamedDirewolf(mount) ? 10f :
        mount?.Code?.Path.StartsWith("bison-tamed-") == true ? 7.5f :
        mount?.Code?.Path.StartsWith("tame-ostrich-") == true ? 8f : BiteDamage;

    bool polledAttack, polledGrab;
    void PollMountedAbilityInput()
    {
        bool Down(string code, GlKeys fallback)
        {
            int key = capi.Input.GetHotKeyByCode(code)?.CurrentMapping?.KeyCode ?? (int)fallback;
            return key >= 0 && key < capi.Input.KeyboardKeyStateRaw.Length && capi.Input.KeyboardKeyStateRaw[key];
        }
        bool attack = Down("fsw-griffon-attack", GlKeys.F), grab = Down("fsw-griffon-grab", GlKeys.R);
        if (IsAttackOnlyMount(capi.World.Player?.Entity?.MountedOn?.Entity))
        {
            if (attack && !polledAttack) RequestAbility(false);
            if (grab && !polledGrab) RequestAbility(true);
        }
        polledAttack = attack;
        polledGrab = grab;
    }

    public static bool CanUseAbilities(Entity mount, EntityAgent rider) => IsAbilityMount(mount)
        && mount.Alive && !GriffonHealthState.IsIncapacitated(mount) && rider?.Alive == true && rider is EntityPlayer && rider.MountedOn?.Entity?.EntityId == mount.EntityId && rider.MountedOn.CanControl;

    public static bool IsGrabCandidate(Entity mount, EntityAgent rider, Entity candidate, bool pvp)
    {
        if (candidate is not EntityAgent agent || agent.MountedOn != null) return false;
        if (candidate.EntityId == mount.EntityId || candidate.EntityId == rider.EntityId || candidate.Pos.Dimension != mount.Pos.Dimension) return false;
        if (candidate.WatchedAttributes.GetBool("fswGriffonCarried") || candidate.WatchedAttributes.GetBool("fswGriffonGrabbedPlayer")) return false;
        return candidate is EntityPlayer ? candidate.Alive && pvp : IsGrabWhitelisted(candidate);
    }

    public static bool CanMountedAttack(Entity mount, EntityAgent rider, Entity candidate)
    {
        if (candidate is not EntityAgent || !candidate.Alive || candidate.EntityId == mount.EntityId
            || candidate.EntityId == rider.EntityId || candidate.Pos.Dimension != mount.Pos.Dimension) return false;
        if (!string.IsNullOrEmpty(mount.WatchedAttributes.GetString("fswCompanionOwnerUid")))
            return FSWildsBehavior.CanAttack(mount, candidate);
        // Riding an unclaimed tame does not require claiming it just to use its abilities.
        if (candidate is EntityPlayer) return mount.WatchedAttributes.GetInt("fswCompanionPvpMode", 0) == 1;
        return !FswCompanionUtil.IsTamedEntity(candidate);
    }

    public static double TargetScore(Entity mount, Entity candidate, bool grab)
    {
        double dx = candidate.Pos.X - mount.Pos.X, dz = candidate.Pos.Z - mount.Pos.Z;
        double dy = Math.Abs(candidate.Pos.Y - mount.Pos.Y);
        double horizontal = Math.Sqrt(dx * dx + dz * dz);
        if (horizontal < 0.001 || horizontal > (grab ? GrabRange : BiteRange) || dy > (grab ? GrabVerticalRange : BiteVerticalRange)) return double.PositiveInfinity;
        double dot = (dx * Math.Sin(mount.Pos.Yaw) + dz * Math.Cos(mount.Pos.Yaw)) / horizontal;
        if (dot < (grab ? GrabMinForwardDot : MinForwardDot)) return double.PositiveInfinity;
        return horizontal + (1 - dot) * (grab ? 1.5 : 1.25) + dy * (grab ? 0.2 : 0.15);
    }

    bool IsOnline(EntityPlayer player) => sapi.World.AllOnlinePlayers.Any(p => p.PlayerUID == player.Player?.PlayerUID);

    void ResetPlayerGrab(IServerPlayer player)
    {
        var entity = player?.Entity;
        if (entity == null || !entity.WatchedAttributes.GetBool("fswGriffonGrabbedPlayer")) return;
        entity.WatchedAttributes.SetBool("fswGriffonGrabbedPlayer", false);
        entity.TryUnmount();
        SendGrabbedPlayerState(entity, false);
    }

    void ResetLoadedCarry(Entity entity)
    {
        // A live carry is session state; never leave a creature restrained after a crash/reload.
        if (entity.WatchedAttributes.GetBool("fswGriffonCarried")) entity.WatchedAttributes.SetBool("fswGriffonCarried", false);
    }

    void InstallUnmountGuard()
    {
        lock (GuardLock)
        {
            if (guard == null)
            {
                guard = new Harmony(GuardId);
                guard.Patch(AccessTools.Method(typeof(EntityAgent), "TryUnmount", Type.EmptyTypes),
                    prefix: new HarmonyMethod(typeof(GriffonMountedAbilities), nameof(PreventGrabbedUnmount)));
            }
            guardUsers++;
            guardAttached = true;
        }
    }

    public static bool PreventGrabbedUnmount(EntityAgent __instance, ref bool __result)
    {
        if (!__instance.WatchedAttributes.GetBool("fswGriffonGrabbedPlayer")) return true;
        __result = false;
        return false;
    }

    public override void Dispose()
    {
        if (sapi != null)
        {
            foreach (var pair in grabbedEntityByMount.ToArray()) ReleaseGrab(pair.Key, pair.Value);
            sapi.Event.UnregisterGameTickListener(serverTickId);
            sapi.Event.PlayerNowPlaying -= ResetPlayerGrab;
            sapi.Event.OnEntityLoaded -= ResetLoadedCarry;
        }
        if (capi != null)
        {
            SetLocalGrabLock(false);
            capi.Event.UnregisterGameTickListener(clientTickId);
            capi.Input.SetHotKeyHandler("fsw-griffon-attack", _ => false);
            capi.Input.SetHotKeyHandler("fsw-griffon-grab", _ => false);
        }
        lock (GuardLock)
        {
            if (guardAttached && --guardUsers == 0) { guard.UnpatchAll(GuardId); guard = null; }
            guardAttached = false;
        }
        base.Dispose();
    }
}
