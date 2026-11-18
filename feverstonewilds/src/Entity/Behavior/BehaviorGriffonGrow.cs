using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace FeverstoneWilds
{
    public class BehaviorGriffonGrow : EntityBehaviorGrow
    {
        public BehaviorGriffonGrow(Entity entity) : base(entity) { }
        float sizeUpdateTimer;

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            if (entity.World.Side != EnumAppSide.Server || !entity.Code.Path.StartsWith("griffon-cub-")) return;
            string gender = entity.WatchedAttributes.GetString("fswGriffonGender");
            if (gender != "male" && gender != "female")
            {
                gender = entity.World.Rand.Next(2) == 0 ? "male" : "female";
                entity.WatchedAttributes.SetString("fswGriffonGender", gender);
                entity.WatchedAttributes.MarkPathDirty("fswGriffonGender");
            }
            // Select once while still a cub so its label and eventual juvenile agree.
            AdultEntityCodes = Array.FindAll(AdultEntityCodes, code => code.Path.EndsWith("-" + gender));
        }

        public static float GrowthProgress(double now, double spawned, float duration)
            => (float)Math.Clamp((now - spawned) / Math.Max(0.001, duration), 0, 1);

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (entity.World.Side != EnumAppSide.Server || !entity.Alive) return;
            sizeUpdateTimer += dt;
            if (sizeUpdateTimer < 1) return;
            sizeUpdateTimer = 0;
            var tree = entity.WatchedAttributes.GetTreeAttribute("grow");
            if (tree == null) return;
            // Reuse vanilla's saved stage age and client sizeGrowthFactor, but grow
            // throughout the entire stage instead of skipping its first ten percent.
            float progress = GrowthProgress(entity.World.Calendar.TotalHours, tree.GetDouble("timeSpawned"), HoursToGrow);
            if (Math.Abs(progress - tree.GetFloat("age")) < 0.001f) return;
            tree.SetFloat("age", progress);
            entity.WatchedAttributes.MarkPathDirty("grow");
        }
        protected override void BecomeAdult(Entity adult, bool keepTextureIndex)
        {
            BehaviorGriffonMarkings.CopyVariants(entity, adult);
            Commands.FSWildsBehavior.CopyState(entity, adult);
            Commands.FswCompanionMapSystem.PrepareGrowth(entity);
            base.BecomeAdult(adult, keepTextureIndex);
        }
    }
}
