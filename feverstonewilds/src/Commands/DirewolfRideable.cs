using System;
using System.Linq;
using FeverstoneWilds.Flight;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Commands;

// Keep vanilla riding and gait controls; reserve the second seat for R pickup.
public class DirewolfRideable : EntityBehaviorRideable
{
    bool attackHeld, grabHeld;
    public DirewolfRideable(Entity entity) : base(entity) { }

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);
        if (entity.Api is not ICoreClientAPI client) return;
        bool Down(string code, GlKeys fallback)
        {
            int key = client.Input.GetHotKeyByCode(code)?.CurrentMapping?.KeyCode ?? (int)fallback;
            return key >= 0 && key < client.Input.KeyboardKeyStateRaw.Length && client.Input.KeyboardKeyStateRaw[key];
        }
        bool attack = Down("fsw-griffon-attack", GlKeys.F), grab = Down("fsw-griffon-grab", GlKeys.R);
        if (client.World.Player?.Entity?.MountedOn?.Entity?.EntityId == entity.EntityId)
        {
            var abilities = client.ModLoader.GetModSystem<GriffonMountedAbilities>();
            if (attack && !attackHeld) abilities?.RequestAbility(false);
            if (grab && !grabHeld) abilities?.RequestAbility(true);
        }
        attackHeld = attack;
        grabHeld = grab;
    }

    protected override IMountableSeat CreateSeat(string seatId, SeatConfig config) =>
        new FlightRideableSeat(this, seatId, config);

    public bool IsDriverSelection(int index)
    {
        var boxes = entity.GetBehavior<EntityBehaviorSelectionBoxes>()?.selectionBoxes;
        if (index <= 0 || boxes == null || index > boxes.Length) return false;
        string point = boxes[index - 1].AttachPoint.Code;
        return Array.Exists(Seats ?? Array.Empty<IMountableSeat>(), seat => seat.Config.Controllable &&
            (seat.Config.SelectionBox == point || seat.Config.APName == point));
    }

    public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition,
        EnumInteractMode mode, ref EnumHandling handled)
    {
        if (!FSWildsBehavior.CanRideCompanion(entity, byEntity))
        {
            handled = EnumHandling.PreventSubsequent;
            return;
        }
        if (!IsDriverSelection((byEntity as EntityPlayer)?.EntitySelection?.SelectionBoxIndex ?? -1) ||
            Seats.Any(seat => seat.Config.Controllable && seat.Passenger != null)) return;
        base.OnInteract(byEntity, slot, hitPosition, mode, ref handled);
    }

    public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection selection,
        IClientPlayer player, ref EnumHandling handled)
    {
        if (!IsDriverSelection(selection.SelectionBoxIndex) || !FSWildsBehavior.CanRideCompanion(entity, player.Entity) ||
            Seats.Any(seat => seat.Config.Controllable && seat.Passenger != null)) return Array.Empty<WorldInteraction>();
        return base.GetInteractionHelp(world, selection, player, ref handled);
    }
}
