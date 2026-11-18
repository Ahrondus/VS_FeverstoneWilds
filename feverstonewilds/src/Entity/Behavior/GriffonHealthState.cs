using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace FeverstoneWilds;

public static class GriffonHealthState
{
    public static bool IsIncapacitated(Entity entity)
    {
        if (entity?.SidedProperties?.Behaviors == null) return false;
        var wounded = entity?.GetBehavior<EntityBehaviorMortallyWoundable>();
        return wounded != null && wounded.HealthState != EnumEntityHealthState.Normal;
    }
}
