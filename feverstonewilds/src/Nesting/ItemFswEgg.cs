using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Nesting
{
    // Vanilla ItemCreature does not copy the egg's saved item attributes to its entity.
    public class ItemFswEgg : ItemCreature
    {
        public static void RestoreProgress(ItemStack stack, Entity egg, double nowHours)
        {
            double elapsed = Math.Max(0, stack.Attributes.GetDouble(EntityBehaviorFswEgg.ProgressKey));
            egg.WatchedAttributes.SetDouble(EntityBehaviorFswEgg.StartKey, nowHours - elapsed);
            egg.WatchedAttributes.SetDouble(EntityBehaviorFswEgg.ProgressKey, elapsed);
            var chick = stack.Attributes["chick"];
            if (chick != null) egg.WatchedAttributes[EntityBehaviorFswEgg.ChickKey] = chick.Clone();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity,
            BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (!firstEvent || blockSel == null || slot.Empty || !(byEntity is EntityPlayer player)) return;
            var world = byEntity.World;
            var pos = blockSel.DidOffset ? blockSel.Position.Copy() : blockSel.Position.AddCopy(blockSel.Face);
            if (!world.Claims.TryAccess(player.Player, pos, EnumBlockAccessFlags.BuildOrBreak)) return;
            if (world.BlockAccessor.GetBlock(pos).Replaceable < 6000) return;
            handHandling = EnumHandHandling.PreventDefault;
            if (world.Side != EnumAppSide.Server) return;

            double elapsed = Math.Max(0, slot.Itemstack.Attributes.GetDouble(EntityBehaviorFswEgg.ProgressKey));
            double hours = Math.Max(0.024, Attributes["incubationDays"].AsDouble(12) * 24);
            string[] stages = Attributes["stageEntities"].AsArray<string>();
            int stage = Math.Min(stages.Length - 1, (int)(elapsed / hours * stages.Length));
            var type = world.GetEntityType(new AssetLocation(stages[stage]));
            if (type == null) return;
            var egg = world.ClassRegistry.CreateEntity(type);
            if (egg == null) return;
            egg.Pos.SetPos(pos.X + 0.5, pos.Y, pos.Z + 0.5);
            egg.Pos.Dimension = pos.dimension;
            egg.Pos.Yaw = byEntity.Pos.Yaw + (float)Math.PI;
            egg.PositionBeforeFalling.Set(egg.Pos.X, egg.Pos.Y, egg.Pos.Z);
            egg.Attributes.SetString("origin", "playerplaced");
            RestoreProgress(slot.Itemstack, egg, world.Calendar.TotalHours);
            world.SpawnEntity(egg);
            if (player.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                slot.TakeOut(1);
                slot.MarkDirty();
            }
        }
    }
}
