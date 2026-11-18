using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace FeverstoneWilds;

// Render size is applied on the client as well as restored on initial spawn/load.
public class BehaviorGriffonGrowthSize(Entity entity) : EntityBehavior(entity)
{
    float baseSize, growthSize, hoursToGrow;
    public override string PropertyName() => "griffongrowthsize";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);
        baseSize = attributes["baseSize"].AsFloat(1);
        growthSize = attributes["growthSize"].AsFloat(0);
        hoursToGrow = attributes["hoursToGrow"].AsFloat(216);
    }

    public override void OnEntitySpawn() => ApplySize();
    public override void OnEntityLoaded() => ApplySize();
    public override void OnGameTick(float dt) => ApplySize();

    void ApplySize()
    {
        if (entity.World.Side != EnumAppSide.Client || entity.Properties?.Client == null) return;
        var grow = entity.WatchedAttributes.GetTreeAttribute("grow");
        if (grow == null) return;
        float progress = grow.HasAttribute("timeSpawned")
            ? BehaviorGriffonGrow.GrowthProgress(entity.World.Calendar.TotalHours, grow.GetDouble("timeSpawned"), hoursToGrow)
            : grow.GetFloat("age");
        entity.Properties.Client.Size = baseSize + System.Math.Clamp(progress, 0, 1) * growthSize;
    }
}
