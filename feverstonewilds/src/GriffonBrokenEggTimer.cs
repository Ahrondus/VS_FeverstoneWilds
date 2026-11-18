using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace FeverstoneWilds
{
    public class GriffonBrokenEggTimerRegistration : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("griffonbrokeneggtimer", typeof(GriffonBrokenEggTimer));
        }
    }

    public class GriffonBrokenEggTimer : EntityBehavior
    {
        private float elapsedSeconds;
        private bool expired;
        public GriffonBrokenEggTimer(Entity entity) : base(entity) { }
        public override string PropertyName() { return "griffonbrokeneggtimer"; }

        public override void OnGameTick(float deltaTime)
        {
            if (expired || entity.World.Side != EnumAppSide.Server) return;
            elapsedSeconds += deltaTime;
            if (elapsedSeconds >= 30f)
            {
                expired = true;
                entity.Die(EnumDespawnReason.Removed);
            }
        }
    }
}
