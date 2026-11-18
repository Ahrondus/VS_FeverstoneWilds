using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting;

public static class EggHeat
{
    static readonly int[] Layers = { BlockLayersAccess.Solid, BlockLayersAccess.Fluid };
    public static bool HasNearbyHeat(IWorldAccessor world, BlockPos receiver, int radius = 3)
    {
        var pos = receiver.Copy();
        for (int x = receiver.X - radius; x <= receiver.X + radius; x++)
        for (int y = receiver.Y - radius; y <= receiver.Y + radius; y++)
        for (int z = receiver.Z - radius; z <= receiver.Z + radius; z++)
        {
            pos.Set(x, y, z);
            foreach (int layer in Layers)
            {
                var block = world.BlockAccessor.GetBlock(pos, layer);
                var heat = block?.GetInterface<IHeatSource>(world, pos);
                if (heat != null && heat.GetHeatStrength(world, pos, receiver) > 0) return true;
            }
        }
        return false;
    }

    public static double Advance(double progress, double elapsedHours, bool wasWarm, bool isWarm)
        => System.Math.Max(0, progress) + (wasWarm && isWarm ? System.Math.Max(0, elapsedHours) : 0);
}
