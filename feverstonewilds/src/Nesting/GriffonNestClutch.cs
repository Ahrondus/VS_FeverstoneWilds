using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting;

// Added as a behavior to the vanilla HenBox, including existing placed nests.
public class GriffonNestClutch(BlockEntity blockentity) : BlockEntityBehavior(blockentity)
{
    long motherId;
    bool sealedClutch;
    int previousEggCount;
    BlockEntityHenBox Nest => (BlockEntityHenBox)Blockentity;
    public long MotherId => motherId;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        if (api.Side == EnumAppSide.Server)
            Blockentity.RegisterGameTickListener(_ => { if (Refresh()) Blockentity.MarkDirty(true); }, 1500);
    }

    bool Refresh()
    {
        int count = Nest.CountEggs();
        long oldMother = motherId;
        bool oldSealed = sealedClutch;
        int oldCount = previousEggCount;
        if (count == 0) { motherId = 0; sealedClutch = false; }
        else if (count >= Nest.Inventory.Count || count < previousEggCount || motherId == 0)
            sealedClutch = true;
        previousEggCount = count;
        return oldMother != motherId || oldSealed != sealedClutch || oldCount != count;
    }

    public bool CanUse(Entity mother)
    {
        Refresh();
        return mother != null && (motherId == 0 || motherId == mother.EntityId);
    }

    public bool CanLay(Entity mother)
        => CanUse(mother) && !sealedClutch && Nest.CountEggs() < Nest.Inventory.Count;

    public void ClaimBrooding(Entity mother)
    {
        if (!CanUse(mother) || Nest.CountEggs() == 0 || motherId != 0) return;
        // Older eggs have no recorded mother. Preserve them, disallow additions,
        // and let the first brooder take care of this existing clutch.
        motherId = mother.EntityId;
        Blockentity.MarkDirty();
    }

    public void RecordEgg(Entity mother)
    {
        motherId = mother.EntityId;
        previousEggCount = Nest.CountEggs();
        sealedClutch = previousEggCount >= Nest.Inventory.Count;
        Blockentity.MarkDirty(true);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        Refresh();
        tree.SetLong("fswClutchMotherId", motherId);
        tree.SetBool("fswClutchSealed", sealedClutch);
        tree.SetInt("fswClutchEggCount", previousEggCount);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
    {
        motherId = tree.GetLong("fswClutchMotherId");
        sealedClutch = tree.GetBool("fswClutchSealed");
        previousEggCount = tree.GetInt("fswClutchEggCount");
    }

    public static GriffonNestClutch For(IAnimalNest nest)
        => nest is GriffonClutchNest wrapped ? wrapped.Clutch : (nest as BlockEntity)?.GetBehavior<GriffonNestClutch>();
}

// Keep vanilla laying, fertility, incubation, and inventory handling. This proxy
// only adds persistent clutch admission checks to the task's nest interface.
public class GriffonClutchNest(IAnimalNest nest, GriffonNestClutch clutch, Entity mother) : IAnimalNest
{
    public GriffonNestClutch Clutch => clutch;
    // Henboxes normally advertise the block midpoint; griffons stand on the low nest surface.
    public Vec3d Position => GriffonBrooding.SurfacePosition(clutch.Blockentity);
    public string Type => nest.Type;
    public float DistanceWeighting => nest.DistanceWeighting;
    public bool IsSuitableFor(Entity entity, string[] types) => clutch.CanUse(entity) && nest.IsSuitableFor(entity, types);
    public bool Occupied(Entity entity) => !clutch.CanUse(entity) || nest.Occupied(entity);
    public void SetOccupier(Entity entity)
    {
        if (entity != null)
        {
            if (!clutch.CanUse(entity)) return;
            clutch.ClaimBrooding(entity);
        }
        nest.SetOccupier(entity);
    }
    public bool TryAddEgg(ItemStack egg)
    {
        if (!clutch.CanLay(mother) || !nest.TryAddEgg(egg)) return false;
        clutch.RecordEgg(mother);
        return true;
    }
}
