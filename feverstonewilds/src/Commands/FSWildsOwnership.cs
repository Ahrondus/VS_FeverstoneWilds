using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace FeverstoneWilds.Commands;

public partial class FSWildsBehavior
{
    public static bool IsOwner(Entity e, string uid) => !string.IsNullOrEmpty(uid)
        && e.WatchedAttributes.GetString(OwnerUidKey) == uid;
    public static bool IsMounted(Entity e) => e.GetInterface<IMountable>()?.IsBeingControlled() == true;
    public const string AccessModeKey = "fswCompanionAccessMode"; // 0 Public, 1 Private

    public static bool CanUseCompanion(Entity e, string uid)
    {
        string owner = e.WatchedAttributes.GetString(OwnerUidKey);
        return !string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(owner) &&
            (owner == uid || e.WatchedAttributes.GetInt(AccessModeKey, 0) == 0);
    }

    public static bool CanRideCompanion(Entity mount, EntityAgent rider)
    {
        if (string.IsNullOrEmpty(mount.WatchedAttributes.GetString(OwnerUidKey))) return true;
        return CanUseCompanion(mount, (rider as EntityPlayer)?.Player?.PlayerUID);
    }

    public static bool CanIssueCompanionCommand(Entity e, string uid, int command) =>
        CanUseCompanion(e, uid) &&
        ((command != 40 && command != 41 && command != 50 && command != 51) || IsOwner(e, uid));

    public bool TrySetAccessMode(string uid, int mode)
    {
        if (entity.World.Side != EnumAppSide.Server || !IsOwner(entity, uid) || mode < 0 || mode > 1) return false;
        entity.WatchedAttributes.SetInt(AccessModeKey, mode);
        entity.WatchedAttributes.MarkPathDirty(AccessModeKey);
        return true;
    }

    public bool TryReleaseOwnership(string ownerUid)
    {
        if (entity.World.Side != EnumAppSide.Server || !entity.Alive || IsMounted(entity)
            || GriffonHealthState.IsIncapacitated(entity) || !IsOwner(entity, ownerUid)) return false;

        // Remove the old owner's tracker before clearing the address it is sent to.
        FswCompanionMapSystem.SendUpdate(entity, true);
        foreach (string key in new[] { "ownedby", OwnerUidKey, OwnerNameKey })
        {
            entity.WatchedAttributes.RemoveAttribute(key);
            entity.WatchedAttributes.MarkPathDirty(key);
        }
        SetStance(0);
        SetMoveMode(2);
        entity.WatchedAttributes.SetInt(PvpModeKey, 0);
        entity.WatchedAttributes.MarkPathDirty(PvpModeKey);
        entity.WatchedAttributes.SetInt(AccessModeKey, 0);
        entity.WatchedAttributes.MarkPathDirty(AccessModeKey);
        return true;
    }

    void SyncVanillaOwner()
    {
        if (entity.World.Side != EnumAppSide.Server) return;
        var owned = entity.WatchedAttributes.GetTreeAttribute("ownedby");
        string uid = owned?.GetString("uid");
        if (string.IsNullOrEmpty(uid) || uid == entity.WatchedAttributes.GetString(OwnerUidKey)) return;
        FswCompanionMapSystem.SendUpdate(entity, true);
        entity.WatchedAttributes.SetString(OwnerUidKey, uid);
        entity.WatchedAttributes.SetString(OwnerNameKey, owned.GetString("name") ?? "Unknown");
        entity.WatchedAttributes.MarkPathDirty(OwnerUidKey);
        entity.WatchedAttributes.MarkPathDirty(OwnerNameKey);
        FswCompanionMapSystem.SendUpdate(entity);
    }

    public static bool CanAttack(Entity attacker, Entity target, bool defensiveTarget = false)
    {
        if (target == null || !target.Alive || target.EntityId == attacker.EntityId || target.Pos.Dimension != attacker.Pos.Dimension) return false;
        string owner = attacker.WatchedAttributes.GetString("fswCompanionOwnerUid");
        if (string.IsNullOrEmpty(owner)) return false;
        if (target is EntityPlayer player)
            return player.Player?.PlayerUID != owner && attacker.WatchedAttributes.GetInt("fswCompanionPvpMode", 0) == 1;
        string otherOwner = target.WatchedAttributes.GetString("fswCompanionOwnerUid") ?? target.WatchedAttributes.GetTreeAttribute("ownedby")?.GetString("uid");
        if (otherOwner == owner) return false;
        if (!FswCompanionUtil.IsTamedEntity(target)) return true;
        return defensiveTarget || attacker.WatchedAttributes.GetLong("fswCompanionCombatTargetId", 0) == target.EntityId
            && attacker.World.ElapsedMilliseconds <= attacker.WatchedAttributes.GetLong("fswCompanionCombatTargetUntil", 0);
    }

    public static void CopyState(Entity from, Entity to)
    {
        // Grow creates the next entity before its behaviors are initialized by SpawnEntity.
        if (from.GetBehavior<FSWildsBehavior>() == null) return;
        foreach (string key in new[] { "ownedby", OwnerUidKey, OwnerNameKey, MoveModeKey, StanceKey, PvpModeKey, AccessModeKey, "fswCompanionName" })
        {
            var value = from.WatchedAttributes.GetAttribute(key);
            if (value != null) { to.WatchedAttributes.SetAttribute(key, value.Clone()); to.WatchedAttributes.MarkPathDirty(key); }
        }
    }
}
