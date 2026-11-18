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
public class AiTaskFswFollowOwner : AiTaskBase
    {
        const string OwnerUidKey = "fswCompanionOwnerUid";
        const string MoveModeKey = "fswCompanionMoveMode";

        float moveSpeed = 0.045f;
        float followStartDistance = 6f;
        float followStopDistance = 3.5f;
        float repathSeconds = 0.75f;

        Entity owner;
        float repathTimer;
        bool pathFailed;
        double lastTargetX;
        double lastTargetY;
        double lastTargetZ;

        public AiTaskFswFollowOwner(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
        {
            moveSpeed = taskConfig["movespeed"].AsFloat(0.045f);
            followStartDistance = taskConfig["followStartDistance"].AsFloat(6f);
            followStopDistance = taskConfig["followStopDistance"].AsFloat(3.5f);
            repathSeconds = taskConfig["repathSeconds"].AsFloat(0.75f);
        }

        public override bool ShouldExecute()
        {
            if (FSWildsBehavior.IsMounted(entity) || entity.WatchedAttributes.GetInt(MoveModeKey, 0) != 0) return false;
            owner = FindOwner();
            if (owner == null || !owner.Alive || owner.Pos.Dimension != entity.Pos.Dimension) return false;
            return HorizontalDistance(owner) > followStartDistance;
        }

        public override void StartExecute()
        {
            base.StartExecute();
            repathTimer = 0;
            pathFailed = false;
            NavigateToOwner();
        }

        public override bool ContinueExecute(float dt)
        {
            if (FSWildsBehavior.IsMounted(entity) || entity.WatchedAttributes.GetInt(MoveModeKey, 0) != 0) return false;
            owner = FindOwner();
            if (owner == null || !owner.Alive || owner.Pos.Dimension != entity.Pos.Dimension) return false;

            double distance = HorizontalDistance(owner);
            var flight = entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>();
            if (flight?.IsFlying == true) { flight.SetFlightTarget(owner.Pos.XYZ); return distance > followStopDistance; }
            if (distance <= followStopDistance) return false;

            repathTimer += dt;
            double dx = owner.Pos.X - lastTargetX;
            double dy = owner.Pos.Y - lastTargetY;
            double dz = owner.Pos.Z - lastTargetZ;
            bool ownerMoved = dx * dx + dy * dy + dz * dz >= 2.25;

            if (pathFailed || repathTimer >= repathSeconds || ownerMoved)
            {
                repathTimer = 0;
                pathFailed = false;
                NavigateToOwner();
            }

            return true;
        }

        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled); entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>()?.ClearFlightTarget();
            entity.ServerControls.StopAllMovement();
            entity.ServerControls.MovespeedMultiplier = 1f;
            owner = null;
            pathFailed = false;
        }

        Entity FindOwner()
        {
            string uid = entity.WatchedAttributes.GetString(OwnerUidKey);
            if (string.IsNullOrEmpty(uid)) return null;
            return entity.World.PlayerByUid(uid)?.Entity;
        }

        double HorizontalDistance(Entity target)
        {
            double dx = target.Pos.X - entity.Pos.X;
            double dz = target.Pos.Z - entity.Pos.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        void NavigateToOwner()
        {
            if (owner == null) return;
            var flight = entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>();
            if (flight?.IsFlying == true) { flight.SetFlightTarget(owner.Pos.XYZ); return; }

            lastTargetX = owner.Pos.X;
            lastTargetY = owner.Pos.Y;
            lastTargetZ = owner.Pos.Z;

            pathTraverser.NavigateTo_Async(
                new Vec3d(lastTargetX, lastTargetY, lastTargetZ),
                moveSpeed,
                followStopDistance,
                () => { },
                () => { pathFailed = true; }
            );
        }
    }
}
