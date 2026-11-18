using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace FeverstoneWilds;

public class BehaviorEntityLight : EntityBehavior
{
    byte[] lightHsv;
    public BehaviorEntityLight(Entity entity) : base(entity) { }
    public override string PropertyName() => "fswentitylight";
    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);
        lightHsv = attributes["lightHsv"].AsObject<byte[]>(new byte[] { 0, 0, 1 });
        entity.LightHsv = (byte[])lightHsv.Clone();
    }
    public override void OnGameTick(float dt)
    {
        if (entity.LightHsv == null) entity.LightHsv = (byte[])lightHsv.Clone();
    }
}
