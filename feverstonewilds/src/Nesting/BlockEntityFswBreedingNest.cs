using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting
{
    // Contents and incubation belong to the nest, so changing mothers or
    // reloading cannot transfer or reset the clutch's progress.
    public class BlockEntityFswBreedingNest : BlockEntityHenBox
    {
        Entity claimant;
        long claimExpires;
        bool brooding;
        public bool Ready => Block.Attributes?["constructionStage"].AsInt() == 0;
        public bool Full => CountEggs() >= Inventory.Count;

        public override bool IsSuitableFor(Entity entity, string[] nestTypes)
            => Ready && base.IsSuitableFor(entity, nestTypes);

        public bool AvailableTo(Entity entity)
            => claimant == null || !claimant.Alive || Api.World.ElapsedMilliseconds >= claimExpires || claimant.EntityId == entity.EntityId;

        public bool Claim(Entity entity)
        {
            if (!AvailableTo(entity)) return false;
            claimant = entity;
            claimExpires = Api.World.ElapsedMilliseconds + 5000;
            return true;
        }

        public void Brood(Entity entity)
        {
            if (!Claim(entity)) return;
            if (!brooding) occupiedTimeLast = Api.World.Calendar.TotalDays;
            brooding = true;
            SetOccupier(entity);
        }

        public void Release(Entity entity)
        {
            if (claimant?.EntityId != entity.EntityId) return;
            claimant = null;
            brooding = false;
            SetOccupier(null);
            occupiedTimeLast = Api.World.Calendar.TotalDays;
        }

        public override bool TryAddEgg(ItemStack egg)
            => Ready && egg != null && base.TryAddEgg(egg);

        protected override void On1500msTick(float dt)
        {
            bool present = brooding && claimant != null && claimant.Alive &&
                Api.World.ElapsedMilliseconds < claimExpires && claimant.Pos.Dimension == Pos.dimension &&
                NestingRules.WithinReach(claimant.Pos.X - (Pos.X + 0.5), claimant.Pos.Y - Pos.Y,
                    claimant.Pos.Z - (Pos.Z + 0.5), 1.55);
            if (!Ready || !Full || !present)
            {
                occupiedTimeLast = Api.World.Calendar.TotalDays;
                return;
            }
            base.On1500msTick(dt);
        }
    }
}
