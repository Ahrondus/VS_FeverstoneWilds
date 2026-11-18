using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace FeverstoneWilds.Commands
{
public class AiTaskFswCompanionAssist : AiTaskBase
    {
        const string TargetIdKey = "fswCompanionCombatTargetId";
        const string TargetUntilKey = "fswCompanionCombatTargetUntil";
        const string StanceKey = "fswCompanionStance";
        const string OwnerUidKey = "fswCompanionOwnerUid";
        const string PvpModeKey = "fswCompanionPvpMode";

        Entity target;
        float moveSpeed = 0.055f;
        float attackRange = 2.2f;
        float damage = 10f;
        float repathTimer;
        float attackTimer;
        bool pathFailed;

        public AiTaskFswCompanionAssist(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            moveSpeed = taskConfig["movespeed"].AsFloat(0.055f);
            attackRange = taskConfig["attackRange"].AsFloat(2.2f);
            damage = taskConfig["damage"].AsFloat(10f);
        }

        public override bool ShouldExecute()
        {
            if (FSWildsBehavior.IsMounted(entity) || entity.WatchedAttributes.GetInt(StanceKey, 1) == 0) return false;
            if ((entity.WatchedAttributes.GetInt(StanceKey, 1) == 1 || entity.WatchedAttributes.GetInt(StanceKey, 1) == 3) && entity.World.ElapsedMilliseconds > entity.WatchedAttributes.GetLong(TargetUntilKey, 0)) return false;
            target = FindTarget();
            return IsValidTarget(target);
        }

        public override void StartExecute()
        {
            base.StartExecute();
            repathTimer = 0;
            attackTimer = 0;
            pathFailed = false;
            Navigate();
        }

        public override bool ContinueExecute(float dt)
        {
            if (FSWildsBehavior.IsMounted(entity) || entity.WatchedAttributes.GetInt(StanceKey, 1) == 0) return false;
            if ((entity.WatchedAttributes.GetInt(StanceKey, 1) == 1 || entity.WatchedAttributes.GetInt(StanceKey, 1) == 3) && entity.World.ElapsedMilliseconds > entity.WatchedAttributes.GetLong(TargetUntilKey, 0)) return false;
            target = FindTarget();
            if (!IsValidTarget(target)) return false;

            double distance = DistanceTo(target);
            FaceTarget();
            attackTimer -= dt;

            if (distance <= attackRange)
            {
                entity.ServerControls.StopAllMovement(); entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>()?.StopFlightMotion();
                if (attackTimer <= 0)
                {
                    attackTimer = 1.05f;
                    entity.StartAnimation("attack");
                    target.ReceiveDamage(new DamageSource()
                    {
                        Source = EnumDamageSource.Entity,
                        SourceEntity = entity,
                        Type = EnumDamageType.SlashingAttack,
                        DamageTier = 2
                    }, damage);
                }
            }
            else
            {
                repathTimer += dt;
                if (pathFailed || repathTimer >= 0.65f)
                {
                    repathTimer = 0;
                    pathFailed = false;
                    Navigate();
                }
            }

            return true;
        }

        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled); entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>()?.ClearFlightTarget();
            entity.ServerControls.StopAllMovement(); entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>()?.StopFlightMotion();
            entity.StopAnimation("attack");
            target = null;
        }

        Entity FindTarget()
        {
            if (entity.WatchedAttributes.GetInt(StanceKey, 1) == 2) {
                if (IsValidTarget(target) && DistanceTo(target) < 20) return target;
                return entity.World.GetNearestEntity(entity.Pos.XYZ, 16, 8, e => e is EntityAgent && IsValidTarget(e) && !FswCompanionUtil.IsTamedEntity(e));
            }
            long id = entity.WatchedAttributes.GetLong(TargetIdKey, 0);
            return id == 0 ? null : entity.World.GetEntityById(id);
        }

        bool IsValidTarget(Entity candidate)
        {
            if (!FSWildsBehavior.CanAttack(entity, candidate)) return false;
            string ownerUid = entity.WatchedAttributes.GetString(OwnerUidKey);
            Entity owner = string.IsNullOrEmpty(ownerUid) ? null : entity.World.PlayerByUid(ownerUid)?.Entity;
            if (owner != null && candidate.EntityId == owner.EntityId) return false;
            if (candidate is EntityPlayer && entity.WatchedAttributes.GetInt(PvpModeKey, 0) == 0) return false;
            return candidate.Pos.Dimension == entity.Pos.Dimension;
        }

        double DistanceTo(Entity candidate)
        {
            double dx = candidate.Pos.X - entity.Pos.X;
            double dy = candidate.Pos.Y - entity.Pos.Y;
            double dz = candidate.Pos.Z - entity.Pos.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        void Navigate()
        {
            if (target == null) return;
            var flight = entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>();
            if (flight?.IsFlying == true) { flight.SetFlightTarget(target.Pos.XYZ); return; }
            pathTraverser.NavigateTo_Async(target.Pos.XYZ, moveSpeed, attackRange - 0.2f, () => { }, () => { pathFailed = true; });
        }

        void FaceTarget()
        {
            if (target == null) return;
            double dx = target.Pos.X - entity.Pos.X;
            double dz = target.Pos.Z - entity.Pos.Z;
            entity.Pos.Yaw = (float)Math.Atan2(dx, dz);
        }
    }
}
