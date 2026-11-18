using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace FeverstoneWilds.Commands
{
public static class FswCompanionCombatHooks
    {
        // Called after an agent completes an attack. For player attackers, this is the most
        // direct and reliable signal for "I hit that - my Defensive companion should help".
        public static void DidAttackPostfix(EntityAgent __instance, DamageSource source, EntityAgent targetEntity)
        {
            if (!(__instance is EntityPlayer playerEntity) || playerEntity.Player == null || targetEntity == null) return;
            FswCompanionUtil.NotifyOwnerCombat(playerEntity.Player.PlayerUID, targetEntity, __instance.World);
        }

        // Called when a player is actually hurt. The source/cause entity becomes the temporary
        // Defensive or Protective target, including another tame if that tame attacked the owner.
        public static void PlayerHurtPostfix(EntityPlayer __instance, DamageSource damageSource, float damage)
        {
            if (__instance?.Player == null || damageSource == null || damage <= 0) return;
            Entity attacker = damageSource.GetCauseEntity() ?? damageSource.SourceEntity;
            if (attacker == null || attacker.EntityId == __instance.EntityId) return;
            FswCompanionUtil.NotifyOwnerCombat(__instance.Player.PlayerUID, attacker, __instance.World, ownerAttacked: true);
        }
    }
}
