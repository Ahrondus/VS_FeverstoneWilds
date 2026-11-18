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
public partial class FSWildsBehavior : EntityBehavior
    {
        const int CommandPacketId = 8610;
        const string OwnerUidKey = "fswCompanionOwnerUid";
        const string OwnerNameKey = "fswCompanionOwnerName";
        const string MoveModeKey = "fswCompanionMoveMode";
        const string StanceKey = "fswCompanionStance";
        const string PvpModeKey = "fswCompanionPvpMode";

        // Move modes: 0 Follow, 1 Stay, 2 Wander
        // Stances:    0 Passive, 1 Defensive, 2 Aggressive, 3 Protective
        // PvP mode:   0 PvE (default), 1 PvP
        float followStartDistance = 6f;
        float followStopDistance = 3.5f;
        float instantRecallDistance = 80f;
        float stuckRecallDistance = 14f;
        float stuckRecallSeconds = 12f;

        long defensiveUntilMs;
        long noProgressSinceMs;
        long nextProgressCheckMs;
        double lastProgressDistance = double.MaxValue;
        bool blockerInstalled;
        long nextMapUpdate;


        public FSWildsBehavior(Entity entity) : base(entity) { }

        public override string PropertyName() => "fswildsbehavior";

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            followStartDistance = attributes["followStartDistance"].AsFloat(6f);
            followStopDistance = attributes["followStopDistance"].AsFloat(3.5f);
            instantRecallDistance = attributes["instantRecallDistance"].AsFloat(80f);
            stuckRecallDistance = attributes["stuckRecallDistance"].AsFloat(14f);
            stuckRecallSeconds = attributes["stuckRecallSeconds"].AsFloat(12f);
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn(); SyncVanillaOwner();
            SetupTaskBlocker();
            SyncCompanionNameTag();
        }

        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded(); SyncVanillaOwner();
            SetupTaskBlocker();
            SyncCompanionNameTag();
        }

        void SyncCompanionNameTag()
        {
            if (entity.World.Side != EnumAppSide.Server) return;
            EntityBehaviorNameTag nameTag = entity.GetBehavior<EntityBehaviorNameTag>();
            if (nameTag == null) return;
            string savedName = entity.WatchedAttributes.GetString("fswCompanionName");
            string labelKey = entity.Code.Domain + ":item-creature-" + entity.Code.Path;
            string currentName = entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name");
            bool youngDefault = entity.Code.Domain == "feverstonewilds" &&
                (currentName == "Tamed Griffon Cub" || currentName == "Tamed Juvenile Griffon" ||
                 currentName == "Tamed Adolescent Griffon");
            if (entity.Code.Path == "griffon-cub-tamed")
            {
                string gender = entity.WatchedAttributes.GetString("fswGriffonGender");
                if (gender == "male" || gender == "female") labelKey += "-" + gender;
            }
            // A newly added vanilla nametag suppresses the normal heading when its name is empty.
            // Restore the original localized creature label, while preserving custom names.
            if (!string.IsNullOrWhiteSpace(savedName)) nameTag.SetName(savedName);
            else if (youngDefault || string.IsNullOrWhiteSpace(entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name")) ||
                (entity.Code.Domain == "feverstonewilds" &&
                 ((entity.Code.Path.StartsWith("griffon-juvenile-tamed-") && entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name") == "Griffon Juvenile") ||
                  (entity.Code.Path.StartsWith("griffon-adolescent-tamed-") && entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name") == "Griffon Adolescent"))))
                nameTag.SetName(Lang.GetMatching(labelKey));
        }

        void SetupTaskBlocker()
        {
            if (blockerInstalled || entity.World.Side != EnumAppSide.Server) return;
            EntityBehaviorTaskAI taskAi = entity.GetBehavior<EntityBehaviorTaskAI>();
            if (taskAi == null) return;
            taskAi.TaskManager.OnShouldExecuteTask += ShouldAllowTask;
            blockerInstalled = true;
        }

        bool ShouldAllowTask(IAiTask task)
        {
            if (GriffonHealthState.IsIncapacitated(entity)) return false;
            if (entity.WatchedAttributes.GetBool("fswGriffonCarried")) return false;
            if (task == null) return true;
            if (IsMounted(entity)) return false;
            if (string.IsNullOrEmpty(entity.WatchedAttributes.GetString(OwnerUidKey))) return true;
            if (task is AiTaskFswFollowOwner) return entity.WatchedAttributes.GetInt(MoveModeKey, 0) == 0;
            if (task.GetType().Name.ToLowerInvariant().Contains("cometoowner")) return false;

            int moveMode = entity.WatchedAttributes.GetInt(MoveModeKey, 0);
            int stance = entity.WatchedAttributes.GetInt(StanceKey, 1);
            string name = task.GetType().Name.ToLowerInvariant();

            bool isCombat = name.Contains("seekentity") || name.Contains("meleeattack") || name.Contains("petseek") || name.Contains("petmelee") || name.Contains("companionassist");
            bool isWander = name.Contains("wander") || name.Contains("seekfood");
            bool isIdle = name.Contains("idle") || name.Contains("lookaround");
            bool isSafety = name.Contains("getoutofwater") || name.Contains("flee") || name.Contains("swim");

            if (isCombat)
            {
                if (!(task is AiTaskFswCompanionAssist)) return false; if (stance == 0) return false;
                if ((stance == 1 || stance == 3) && entity.World.ElapsedMilliseconds > entity.WatchedAttributes.GetLong("fswCompanionCombatTargetUntil", 0)) return false;
            }

            if (moveMode == 1) // Stay
            {
                return isIdle || isSafety || isCombat;
            }

            if (moveMode == 0) // Follow
            {
                Entity owner = GetOwnerEntity();
                if (owner != null && HorizontalDistanceTo(owner) > followStartDistance)
                {
                    // Catching the owner takes precedence over grazing, wandering, or hunting.
                    if (isWander || isIdle || isCombat) return false;
                }
            }

            return true;
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode != EnumInteractMode.Interact || !entity.Alive || !(byEntity is EntityPlayer playerEntity) || playerEntity.Player == null) return;
            if (itemslot != null && !itemslot.Empty) return;

            SyncVanillaOwner(); string uid = playerEntity.Player.PlayerUID;
            string currentOwner = entity.WatchedAttributes.GetString(OwnerUidKey);

            // Companion commands are deliberately menu-only: Shift + right-click.
            // A normal empty-hand right-click no longer cycles Follow/Stay/Wander.
            if (byEntity.Controls.ShiftKey)
            {
                handled = EnumHandling.PreventSubsequent;

                if (entity.World.Side == EnumAppSide.Client)
                {
                    if (string.IsNullOrEmpty(currentOwner))
                    {
                        (entity.Api as ICoreClientAPI)?.TriggerIngameError(this, "fswildsbehavior-unclaimed", "Claim this companion first with an empty-hand right-click.");
                    }
                    else if (CanUseCompanion(entity, uid))
                    {
                        OpenCommandDialog();
                    }
                    else
                    {
                        (entity.Api as ICoreClientAPI)?.TriggerIngameError(this, "fswildsbehavior-notowner", "This FS Wilds companion belongs to another player.");
                    }
                }
                return;
            }

            // Empty-hand right-click is retained only for the one-time ownership claim.
            // Once claimed, it is not a command input and is left alone for other interactions.
            if (!string.IsNullOrEmpty(currentOwner)) return;

            handled = EnumHandling.PreventSubsequent;
            if (entity.World.Side != EnumAppSide.Server) return;

            Claim(playerEntity);
            Notify(playerEntity, "This tame is now your FS Wilds companion. Shift + right-click it to choose commands.");
        }

        void OpenCommandDialog()
        {
            ICoreClientAPI capi = entity.Api as ICoreClientAPI;
            if (capi == null) return;

            FSWildsCommands dialog = new FSWildsCommands(
                capi,
                entity,
                entity.WatchedAttributes.GetInt(MoveModeKey, 0),
                entity.WatchedAttributes.GetInt(StanceKey, 1),
                entity.WatchedAttributes.GetInt(PvpModeKey, 0)
            );
            dialog.TryOpen();
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            base.OnReceivedClientPacket(player, packetid, data, ref handled);
            if (packetid != CommandPacketId || player == null || data == null || data.Length == 0) return;
            handled = EnumHandling.PreventSubsequent;

            SyncVanillaOwner();
            if (!entity.Alive || IsMounted(entity) || GriffonHealthState.IsIncapacitated(entity) || !CanIssueCompanionCommand(entity, player.PlayerUID, data[0])) return;

            int command = data[0];
            if (command == 50 || command == 51)
            {
                if (TrySetAccessMode(player.PlayerUID, command - 50))
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion access: " + (command == 50 ? "Public" : "Private") + ".", EnumChatType.Notification);
                return;
            }
            if (command == 41)
            {
                if (TryReleaseOwnership(player.PlayerUID))
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion ownership released. This tame can be claimed again.", EnumChatType.Notification);
                return;
            }
            if (command == 40)
            {
                string name = data.Length > 1 ? Encoding.UTF8.GetString(data, 1, data.Length - 1).Trim() : "";
                if (name.Length == 0) name = entity.GetName();
                if (name.Length > 32) name = name.Substring(0, 32);
                entity.WatchedAttributes.SetString("fswCompanionName", name);
                entity.WatchedAttributes.MarkPathDirty("fswCompanionName");
                EntityBehaviorNameTag nameTag = entity.GetBehavior<EntityBehaviorNameTag>();
                if (nameTag != null) nameTag.SetName(name);
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion renamed to " + name + ".", EnumChatType.Notification);
                return;
            }

            if (command >= 0 && command <= 2)
            {
                SetMoveMode(command);
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion command: " + MoveModeName(command) + ".", EnumChatType.Notification);
                return;
            }

            if (command >= 10 && command <= 13)
            {
                int stance = command - 10;
                SetStance(stance);
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion stance: " + StanceName(stance) + ".", EnumChatType.Notification);
                return;
            }

            if (command == 30 || command == 31)
            {
                int pvpMode = command == 31 ? 1 : 0;
                entity.WatchedAttributes.SetInt(PvpModeKey, pvpMode);
                entity.WatchedAttributes.MarkPathDirty(PvpModeKey);
                if (pvpMode == 0)
                {
                    Entity current = entity.World.GetEntityById(entity.WatchedAttributes.GetLong("fswCompanionCombatTargetId", 0));
                    if (current is EntityPlayer)
                    {
                        entity.WatchedAttributes.SetLong("fswCompanionCombatTargetId", 0);
                        entity.WatchedAttributes.SetLong("fswCompanionCombatTargetUntil", 0);
                        entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
                    }
                }
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion combat mode: " + (pvpMode == 1 ? "PVP" : "PVE") + ".", EnumChatType.Notification);
                return;
            }

            if (command == 20)
            {
                Entity owner = player.Entity;
                if (owner != null && owner.Alive)
                {
                    RecallToOwner(owner);
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Companion recalled.", EnumChatType.Notification);
                }
            }
        }

        void SetMoveMode(int moveMode)
        {
            entity.WatchedAttributes.SetInt(MoveModeKey, moveMode);
            entity.WatchedAttributes.MarkPathDirty(MoveModeKey);
            entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
            StopFollowDriving();
            ResetProgressTracking();
        }

        void SetStance(int stance)
        {
            entity.WatchedAttributes.SetLong("fswCompanionCombatTargetId", 0);
            entity.WatchedAttributes.SetLong("fswCompanionCombatTargetUntil", 0);
            entity.WatchedAttributes.MarkPathDirty("fswCompanionCombatTargetId");
            entity.WatchedAttributes.MarkPathDirty("fswCompanionCombatTargetUntil");
            entity.WatchedAttributes.SetInt(StanceKey, stance);
            entity.WatchedAttributes.MarkPathDirty(StanceKey);
            entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
        }

        public void SetDefensiveCombatTarget(Entity target, bool ownerAttacked = false)
        {
            if (target == null || !target.Alive || !CanAttack(entity, target, true)) return;
            int stance = entity.WatchedAttributes.GetInt(StanceKey, 1);
            if (stance != 1 && !(stance == 3 && ownerAttacked)) return;
            if (target is EntityPlayer && entity.WatchedAttributes.GetInt(PvpModeKey, 0) == 0) return;
            string ownerUid = entity.WatchedAttributes.GetString(OwnerUidKey);
            Entity owner = string.IsNullOrEmpty(ownerUid) ? null : entity.World.PlayerByUid(ownerUid)?.Entity;
            if (owner != null && target.EntityId == owner.EntityId) return;

            defensiveUntilMs = entity.World.ElapsedMilliseconds + 20000;
            entity.WatchedAttributes.SetLong("fswCompanionCombatTargetId", target.EntityId);
            entity.WatchedAttributes.SetLong("fswCompanionCombatTargetUntil", defensiveUntilMs);
            entity.WatchedAttributes.MarkPathDirty("fswCompanionCombatTargetId");
            entity.WatchedAttributes.MarkPathDirty("fswCompanionCombatTargetUntil");
            entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
        }

        void Claim(EntityPlayer playerEntity)
        {
            entity.WatchedAttributes.SetString(OwnerUidKey, playerEntity.Player.PlayerUID);
            var owned = entity.WatchedAttributes.GetTreeAttribute("ownedby") ?? new TreeAttribute();
            owned.SetString("uid", playerEntity.Player.PlayerUID); owned.SetString("name", playerEntity.Player.PlayerName);
            entity.WatchedAttributes.SetAttribute("ownedby", owned); entity.WatchedAttributes.MarkPathDirty("ownedby");
            entity.WatchedAttributes.SetString(OwnerNameKey, playerEntity.Player.PlayerName);
            entity.WatchedAttributes.SetInt(MoveModeKey, 0);
            entity.WatchedAttributes.SetInt(StanceKey, 1);
            entity.WatchedAttributes.SetInt(PvpModeKey, 0);

            // Keep FS Wilds ownership self-contained. Other companion mods may be installed,
            // but FS Wilds does not write their ownership/command attributes or depend on them.
            entity.WatchedAttributes.MarkPathDirty(OwnerUidKey);
            entity.WatchedAttributes.MarkPathDirty(OwnerNameKey);
            entity.WatchedAttributes.MarkPathDirty(MoveModeKey);
            entity.WatchedAttributes.MarkPathDirty(StanceKey);
            entity.WatchedAttributes.MarkPathDirty(PvpModeKey);
            FswCompanionMapSystem.SendUpdate(entity);
            entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
        }

        void Notify(EntityPlayer playerEntity, string message)
        {
            IServerPlayer player = playerEntity.Player as IServerPlayer;
            player?.SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);

            string owner = entity.WatchedAttributes.GetString(OwnerNameKey);
            if (string.IsNullOrEmpty(owner))
            {
                infotext.AppendLine("FS Wilds companion: Unclaimed");
                infotext.AppendLine("Empty-hand right-click once to claim");
                return;
            }

            var health = entity.GetBehavior<EntityBehaviorHealth>();
            if (health != null)
            {
                infotext.AppendLine($"Health: {health.Health:0.#} / {health.MaxHealth:0.#}");
            }

            infotext.AppendLine("Companion of " + owner);
            infotext.AppendLine("Command: " + MoveModeName(entity.WatchedAttributes.GetInt(MoveModeKey, 0)));
            infotext.AppendLine("Stance: " + StanceName(entity.WatchedAttributes.GetInt(StanceKey, 1)));
            infotext.AppendLine("Combat mode: " + (entity.WatchedAttributes.GetInt(PvpModeKey, 0) == 1 ? "PVP" : "PVE"));
            infotext.AppendLine("Shift + right-click: open command menu");
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);
            if (entity.World.Side != EnumAppSide.Server || damage <= 0 || damageSource == null) return;
            Entity attacker = damageSource.GetCauseEntity() ?? damageSource.SourceEntity;
            if (attacker == null) return;

            if (entity.WatchedAttributes.GetInt(StanceKey, 1) == 1)
            {
                SetDefensiveCombatTarget(attacker);
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            if (entity.World.Side == EnumAppSide.Server && !entity.Attributes.GetBool("fswCompanionGrowing"))
                FswCompanionMapSystem.HandleCompanionDeath(entity);
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (entity.World.Side != EnumAppSide.Server || !entity.Alive) return;
            SyncVanillaOwner();
            if (entity.World.ElapsedMilliseconds >= nextMapUpdate)
            {
                nextMapUpdate = entity.World.ElapsedMilliseconds + 2000;
                FswCompanionMapSystem.SendUpdate(entity);
            }
            if (GriffonHealthState.IsIncapacitated(entity)) return;
            if (entity.WatchedAttributes.GetBool("fswGriffonCarried")) return;
            if (IsMounted(entity)) return;
            if (entity.WatchedAttributes.GetInt(MoveModeKey, 0) == 1) entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>()?.HoldGroundForNesting();

            if (entity.WatchedAttributes.GetInt(MoveModeKey, 0) != 0)
            {
                ResetProgressTracking();
                return;
            }

            Entity owner = GetOwnerEntity();
            if (owner == null || !owner.Alive || owner.Pos.Dimension != entity.Pos.Dimension)
            {
                ResetProgressTracking();
                return;
            }

            double distance = HorizontalDistanceTo(owner);
            if (distance >= instantRecallDistance)
            {
                RecallToOwner(owner);
                return;
            }

            if (distance <= followStopDistance)
            {
                ResetProgressTracking();
                return;
            }

            // Actual Follow locomotion is performed by AiTaskFswFollowOwner through
            // Vintage Story's path traverser. This behavior only supplies the recall/stuck
            // failsafe so Follow uses the same authoritative movement system as normal AI.
            if (distance >= followStartDistance)
            {
                CheckStuckRecall(owner, distance);
            }
        }

        Entity GetOwnerEntity()
        {
            string uid = entity.WatchedAttributes.GetString(OwnerUidKey);
            if (string.IsNullOrEmpty(uid)) return null;
            return entity.World.PlayerByUid(uid)?.Entity;
        }

        double HorizontalDistanceTo(Entity other)
        {
            if (other == null || other.Pos.Dimension != entity.Pos.Dimension) return double.MaxValue;
            double dx = other.Pos.X - entity.Pos.X;
            double dz = other.Pos.Z - entity.Pos.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        void StopFollowDriving()
        {
            EntityAgent agent = entity as EntityAgent;
            agent?.ServerControls.StopAllMovement();
            if (agent != null) agent.ServerControls.MovespeedMultiplier = 1f;
        }

        void CheckStuckRecall(Entity owner, double distance)
        {
            if (distance < stuckRecallDistance)
            {
                ResetProgressTracking();
                return;
            }

            long now = entity.World.ElapsedMilliseconds;
            if (now < nextProgressCheckMs) return;
            nextProgressCheckMs = now + 2000;

            if (lastProgressDistance == double.MaxValue || distance < lastProgressDistance - 1.0)
            {
                lastProgressDistance = distance;
                noProgressSinceMs = now;
                return;
            }

            if (noProgressSinceMs == 0) noProgressSinceMs = now;
            if (now - noProgressSinceMs >= (long)(stuckRecallSeconds * 1000f))
            {
                RecallToOwner(owner);
            }
        }

        void ResetProgressTracking()
        {
            lastProgressDistance = double.MaxValue;
            noProgressSinceMs = 0;
            nextProgressCheckMs = 0;
        }

        void RecallToOwner(Entity owner)
        {
            StopFollowDriving();
            entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();

            // Put the companion just behind and slightly to the side of the owner. This is
            // deliberately a failsafe; normal Follow movement is always attempted first.
            float yaw = owner.Pos.Yaw;
            double backX = Math.Sin(yaw) * 2.2;
            double backZ = Math.Cos(yaw) * 2.2;
            Vec3d target = new Vec3d(owner.Pos.X + backX + 0.6, owner.Pos.Y + 0.1, owner.Pos.Z + backZ + 0.6);
            if (owner.Pos.Dimension != entity.Pos.Dimension || IsMounted(entity)) return;
            entity.GetBehavior<FeverstoneWilds.Flight.Behavior.BehaviorFlight>()?.SetFlying(false);
            entity.TeleportTo(target);
            entity.Pos.Yaw = owner.Pos.Yaw;
            ResetProgressTracking();
        }

        static string MoveModeName(int mode)
        {
            if (mode == 1) return "Stay";
            if (mode == 2) return "Wander";
            return "Follow";
        }

        static string StanceName(int stance)
        {
            if (stance == 0) return "Passive";
            if (stance == 2) return "Aggressive";
            if (stance == 3) return "Protective";
            return "Defensive";
        }
    }
}
