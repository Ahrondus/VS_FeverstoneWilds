using System;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds;

public class AiTaskGriffonGuardNest : AiTaskBase
{
    const string NestKey = "fswGuardedWildNestId";
    const double AlertRange = 12, LeashRange = 30;
    Entity nest, target;
    long nextSearch;
    float repath, attackCooldown;
    bool walking;

    public AiTaskGriffonGuardNest(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
        : base(entity, taskConfig, aiConfig) { }

    public static bool IsWildAdult(Entity e) => e?.Code?.Domain == "feverstonewilds" &&
        (e.Code.Path == "griffon-wild-male" || e.Code.Path == "griffon-wild-female");

    public static bool IsWildNest(Entity e) => e?.Alive == true && e.Code?.Domain == "feverstonewilds" &&
        e.Code.Path == "wild-griffon-nest-eggs";

    public static bool WithinNestRange(Entity candidate, Entity nest, double range) => candidate != null && nest != null &&
        candidate.Pos.Dimension == nest.Pos.Dimension && candidate.Pos.XYZ.SquareDistanceTo(nest.Pos.XYZ) <= range * range;

    public static bool IsIntruder(Entity candidate, Entity nest, double range) => candidate is EntityPlayer player &&
        IsIntruder(candidate, nest, range, player.Player?.WorldData?.CurrentGameMode ?? EnumGameMode.Spectator);

    public static bool IsIntruder(Entity candidate, Entity nest, double range, EnumGameMode mode) =>
        candidate is EntityPlayer player && player.Alive && mode == EnumGameMode.Survival &&
        WithinNestRange(candidate, nest, range);

    bool FindNest()
    {
        if (!IsWildAdult(entity)) return false;
        if (IsWildNest(nest) && nest.Pos.Dimension == entity.Pos.Dimension && entity.World.GetEntityById(nest.EntityId) == nest) return true;
        nest = entity.World.GetEntityById(entity.WatchedAttributes.GetLong(NestKey));
        if (IsWildNest(nest) && nest.Pos.Dimension == entity.Pos.Dimension) return true;
        if (entity.World.ElapsedMilliseconds < nextSearch) return false;
        nextSearch = entity.World.ElapsedMilliseconds + 5000;
        nest = entity.World.GetNearestEntity(entity.Pos.XYZ, (float)LeashRange, 16,
            e => IsWildNest(e) && WithinNestRange(entity, e, LeashRange));
        entity.WatchedAttributes.SetLong(NestKey, nest?.EntityId ?? 0);
        entity.WatchedAttributes.MarkPathDirty(NestKey);
        target = null;
        return nest != null;
    }

    public override bool ShouldExecute()
    {
        if (!entity.Alive || entity.Swimming || !PreconditionsSatisfied() || !FindNest()) return false;
        if (!IsIntruder(target, nest, LeashRange))
            target = entity.World.GetNearestEntity(nest.Pos.XYZ, (float)AlertRange, (float)AlertRange,
                e => IsIntruder(e, nest, AlertRange));
        return target != null || !WithinNestRange(entity, nest, AlertRange);
    }

    public override void StartExecute()
    {
        repath = 1;
        attackCooldown = 0;
        pathTraverser.Stop();
    }

    public override bool ContinueExecute(float dt)
    {
        if (!entity.Alive || entity.Swimming || !IsWildAdult(entity) || !IsWildNest(nest) ||
            nest.Pos.Dimension != entity.Pos.Dimension || entity.World.GetEntityById(nest.EntityId) != nest) return false;
        var flight = entity.GetBehavior<BehaviorFlight>();
        flight?.HoldGroundForNesting();
        if (!IsIntruder(target, nest, LeashRange) || !WithinNestRange(entity, nest, LeashRange)) target = null;
        bool returning = target == null;
        Vec3d destination = returning ? nest.Pos.XYZ : target.Pos.XYZ;
        double distance = entity.Pos.XYZ.DistanceTo(destination);
        if (returning && distance < 6) return false;
        if (flight?.IsFlying == true)
        {
            flight.SetFlightTarget(destination);
            return true;
        }
        attackCooldown -= dt;
        if (!returning && distance <= 2.5)
        {
            pathTraverser.Stop();
            entity.ServerControls.StopAllMovement();
            if (walking) { entity.StopAnimation("walk"); walking = false; }
            entity.Pos.Yaw = (float)Math.Atan2(target.Pos.X - entity.Pos.X, target.Pos.Z - entity.Pos.Z);
            if (attackCooldown <= 0)
            {
                attackCooldown = 1.2f;
                entity.StopAnimation("attack");
                entity.StartAnimation("attack");
                target.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Entity, SourceEntity = entity,
                    Type = EnumDamageType.SlashingAttack, DamageTier = 2 }, 9.5f);
            }
            return true;
        }
        repath += dt;
        if (repath >= 0.75f)
        {
            repath = 0;
            if (!walking) { entity.StartAnimation("walk"); walking = true; }
            pathTraverser.NavigateTo_Async(destination, returning ? 0.025f : 0.045f, returning ? 5 : 2,
                () => { repath = 1; }, () => { repath = 1; });
        }
        return true;
    }

    public override void FinishExecute(bool cancelled)
    {
        pathTraverser.Stop();
        entity.ServerControls.StopAllMovement();
        if (walking) entity.StopAnimation("walk");
        entity.StopAnimation("attack");
        walking = false;
        entity.GetBehavior<BehaviorFlight>()?.ClearFlightTarget();
    }
}
