using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace FeverstoneWilds
{
    // The poultry system keeps the current vanilla IAnimalNest implementation.
    public class BlockEntityAnimalNestLarge : BlockEntityHenBox
    {
        public override bool IsSuitableFor(Entity entity, string[] nestTypes)
        {
            string nestType = Block.Attributes?["nestType"].AsString("ground-large") ?? "ground-large";
            return entity is EntityAgent && nestTypes != null && System.Array.IndexOf(nestTypes, nestType) >= 0;
        }

        public override void Initialize(ICoreAPI api)
        {
            // Normalize this mod's legacy empty block before vanilla's henbox
            // migration tries to resolve an unqualified replacement in game:.
            if (api.Side == EnumAppSide.Server && Block.Code.Path == "largenest-empty")
            {
                Block current = api.World.GetBlock(new AssetLocation(Block.Code.Domain, "largenest"));
                if (current != null)
                {
                    api.World.BlockAccessor.ExchangeBlock(current.Id, Pos);
                    Block = current;
                }
            }
            base.Initialize(api);
        }
    }
}
