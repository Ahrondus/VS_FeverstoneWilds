using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace FeverstoneWilds
{
    public class GriffonEggPickupRegistration : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("griffoneggpickup", typeof(GriffonEggPickupBehavior));
        }
    }

    public class GriffonEggPickupBehavior : EntityBehavior
    {
        private bool collecting;
        public GriffonEggPickupBehavior(Entity entity) : base(entity) { }
        public override string PropertyName() { return "griffoneggpickup"; }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode != EnumInteractMode.Interact || collecting)
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
                return;
            }
            EntityPlayer player = byEntity as EntityPlayer;
            if (player == null || player.Player == null)
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
                return;
            }
            handled = EnumHandling.PreventDefault;
            if (entity.World.Side != EnumAppSide.Server) return;
            Item egg = entity.World.GetItem(new AssetLocation("feverstonewilds:creature-griffon-egg"));
            if (egg == null) return;
            collecting = true;
            ItemStack stack = new ItemStack(egg, 1);
            bool given = player.Player.InventoryManager.TryGiveItemstack(stack, true);
            if (!given && stack.StackSize > 0)
            {
                entity.World.SpawnItemEntity(stack, entity.Pos.XYZ.Add(0, 0.2, 0));
            }
            entity.Die(EnumDespawnReason.Removed);
        }
    }
}
