using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace FeverstoneWilds.Nesting;

public static class GriffonBrooding
{
    public static Vec3d SurfacePosition(BlockEntity nest)
    {
        double top = 0;
        var boxes = nest.Block.GetCollisionBoxes(nest.Api.World.BlockAccessor, nest.Pos);
        if (boxes != null)
            foreach (var box in boxes) top = Math.Max(top, box.Y2);
        return new Vec3d(nest.Pos.X + 0.5, nest.Pos.Y + top + 0.001, nest.Pos.Z + 0.5);
    }

    public static bool OnNest(Vec3d feet, Vec3d surface, double heightTolerance = 0.03) =>
        feet.HorizontalSquareDistanceTo(surface.X, surface.Z) <= 0.01 && Math.Abs(feet.Y - surface.Y) <= heightTolerance;

    public static bool TrySettle(EntityAgent mother, Vec3d surface)
    {
        // Only the last few centimetres of a completed approach are aligned; never pull her across a pen.
        if (!mother.OnGround || !OnNest(mother.Pos.XYZ, surface, 0.15)) return false;
        if (mother.World.CollisionTester?.IsColliding(mother.World.BlockAccessor, mother.CollisionBox, surface, false) == true)
            return false;
        mother.Pos.SetPos(surface);
        mother.Pos.Motion.X = mother.Pos.Motion.Z = 0;
        mother.ServerControls.StopAllMovement();
        mother.Controls.StopAllMovement();
        return true;
    }
}
