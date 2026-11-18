using System;

namespace FeverstoneWilds.Nesting
{
    public static class NestingRules
    {
        public static int RemainingAfterLay(int remaining, bool success) => Math.Max(0, remaining - (success ? 1 : 0));
        public static bool CanMate(bool alive, int remaining, float food, float requiredFood, double now, double cooldown)
            => alive && remaining <= 0 && food >= requiredFood && now >= cooldown;
        public static bool WithinReach(double dx, double dy, double dz, double radius)
            => dx * dx + dz * dz <= radius * radius && Math.Abs(dy) <= 1.5;
    }
}
