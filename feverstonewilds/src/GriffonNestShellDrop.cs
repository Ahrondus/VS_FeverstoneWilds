using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace FeverstoneWilds
{
    // Shell loot is a temporary physical remnant, not a collectible item.
    public class GriffonNestShellDrop : EntityBehavior
    {
        private bool dropped;
        public GriffonNestShellDrop(Entity entity) : base(entity) { }
        public override string PropertyName() => "griffonnestshelldrop";

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            if (dropped || entity.World.Side != EnumAppSide.Server) return;
            dropped = true;
            if (entity.World.Rand.NextDouble() >= 0.5) return;
            var type = entity.World.GetEntityType(new AssetLocation("feverstonewilds:griffon-egg-broken"));
            if (type == null) return;
            var shell = entity.World.ClassRegistry.CreateEntity(type);
            shell.Pos.SetFrom(entity.Pos);
            shell.Pos.Y += 0.25;
            shell.Pos.Motion.Set((entity.World.Rand.NextDouble() - 0.5) * 0.04,
                0.04, (entity.World.Rand.NextDouble() - 0.5) * 0.04);
            entity.World.SpawnEntity(shell);
        }
    }
}
