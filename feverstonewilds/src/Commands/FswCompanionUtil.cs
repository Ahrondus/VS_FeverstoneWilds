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
public static class FswCompanionUtil
    {
        public static bool IsFswCompanion(Entity entity)
        {
            if (entity?.Code == null || entity.Code.Domain != "feverstonewilds") return false;
            if (entity.GetBehavior<FSWildsBehavior>() != null) return true;
            return false;
        }

        public static void NotifyOwnerCombat(string ownerUid, Entity target, IWorldAccessor world, bool ownerAttacked = false)
        {
            if (string.IsNullOrEmpty(ownerUid) || target == null || world == null) return;
            IPlayer ownerPlayer = world.PlayerByUid(ownerUid);
            Entity owner = ownerPlayer?.Entity;
            if (owner == null) return;

            Entity[] nearby = world.GetEntitiesAround(owner.Pos.XYZ, 80f, 40f, e => e != null && IsFswCompanion(e));
            for (int i = 0; i < nearby.Length; i++)
            {
                FSWildsBehavior behavior = nearby[i].GetBehavior<FSWildsBehavior>();
                if (behavior == null) continue;
                if (nearby[i].WatchedAttributes.GetString("fswCompanionOwnerUid") != ownerUid) continue;
                behavior.SetDefensiveCombatTarget(target, ownerAttacked);
            }
        }

        public static bool IsTamedEntity(Entity entity)
        {
            if (entity == null) return false;

            string path = entity.Code?.Path ?? "";
            if (path.EndsWith("-tamed") || path.Contains("-tamed-") || path.StartsWith("tame-") || path.Contains("-tames-") || path.StartsWith("taming")) return true;

            // Vanilla mount ownership and many tame mods persist one of these attributes.
            if (entity.WatchedAttributes.GetTreeAttribute("ownedby") != null) return true;
            if (!string.IsNullOrEmpty(entity.WatchedAttributes.GetString("guardedPlayerUid"))) return true;
            if (!string.IsNullOrEmpty(entity.WatchedAttributes.GetString("fswCompanionOwnerUid"))) return true;

            // Vanilla rideable/owned creatures use this behavior once claimed.
            try
            {
                EntityBehaviorOwnable ownable = entity.GetBehavior<EntityBehaviorOwnable>();
                if (ownable != null && entity.WatchedAttributes.GetTreeAttribute("ownedby") != null) return true;
            }
            catch { }

            // Content tags are not exposed consistently by every mod/version, so use reflection
            // as an optional final check without creating a hard dependency on another mod.
            try
            {
                object props = entity.Properties;
                PropertyInfo tagsProp = props?.GetType().GetProperty("Tags", BindingFlags.Public | BindingFlags.Instance);
                object tagsObj = tagsProp?.GetValue(props);
                if (tagsObj is string[] tags)
                {
                    for (int i = 0; i < tags.Length; i++) if (tags[i] == "tamed") return true;
                }
            }
            catch { }

            return false;
        }
    }
}
