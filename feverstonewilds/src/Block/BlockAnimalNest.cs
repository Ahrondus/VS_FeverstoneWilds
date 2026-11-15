using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace FeverstoneWilds
{
    public class BlockAnimalNest : Block
    {
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (Variant["eggCount"] == "empty") return new WorldInteraction[0];

            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-collect-eggs",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
    }
}
