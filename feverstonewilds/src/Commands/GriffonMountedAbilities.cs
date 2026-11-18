using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Commands
{
    [ProtoContract]
    public class GriffonAttackPacket
    {
        [ProtoMember(1)]
        public bool Pressed;
    }

    [ProtoContract]
    public class GriffonGrabPacket
    {
        [ProtoMember(1)]
        public bool Pressed;
    }

    [ProtoContract]
    public class GriffonGrabbedPlayerPacket
    {
        [ProtoMember(1)]
        public bool Grabbed;
    }

    /// <summary>
    /// Context-sensitive mounted Griffon attack. F is only consumed while the
    /// local player is actually riding a tamed FS Wilds Griffon. Damage is
    /// resolved server-side so multiplayer clients cannot directly choose a victim.
    /// </summary>
    public partial class GriffonMountedAbilities : ModSystem
    {
        const string ChannelName = "feverstonewilds-griffonabilities";
        const int CooldownMs = 900;
        const int DamageDelayMs = 330;
        const float BiteDamage = 9.5f;
        const float BiteRange = 4.5f;
        const float BiteVerticalRange = 2.75f;
        const double MinForwardDot = 0.15; // broad head-forward bite cone; small prey such as chickens should register
        const float GrabRange = 4.0f;
        const float GrabVerticalRange = 2.75f;
        const double GrabMinForwardDot = 0.30;

        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        IClientNetworkChannel clientChannel;
        IServerNetworkChannel serverChannel;
        readonly Dictionary<long, long> nextBiteAtByMount = new Dictionary<long, long>();
        readonly Dictionary<long, long> grabbedEntityByMount = new Dictionary<long, long>();
        long lastClientBiteSendAt;
        bool localPlayerGrabbed;
        readonly Dictionary<string, ActionConsumable<KeyCombination>> blockedHotkeyHandlers = new Dictionary<string, ActionConsumable<KeyCombination>>();

        public override void Start(ICoreAPI api)
        {
            InstallUnmountGuard();
            api.Network.RegisterChannel(ChannelName)
                .RegisterMessageType<GriffonAttackPacket>()
                .RegisterMessageType<GriffonGrabPacket>()
                .RegisterMessageType<GriffonGrabbedPlayerPacket>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            // Context-sensitive hotkeys run before competing F/R bindings while driving a griffon.
            capi = api;
            clientChannel = api.Network.GetChannel(ChannelName);
            clientChannel.SetMessageHandler<GriffonGrabbedPlayerPacket>(OnGrabbedPlayerState);
            clientTickId = api.Event.RegisterGameTickListener(OnClientGrabLockTick, 20);
            RegisterAbilityInput();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            serverChannel = api.Network.GetChannel(ChannelName);
            serverChannel.SetMessageHandler<GriffonAttackPacket>(OnBitePacket);
            serverChannel.SetMessageHandler<GriffonGrabPacket>(OnGrabPacket);
            serverTickId = api.Event.RegisterGameTickListener(OnServerTick, 20);
            api.Event.PlayerNowPlaying += ResetPlayerGrab;
            api.Event.OnEntityLoaded += ResetLoadedCarry;
        }


        public void RequestBite()
        {
            SendBitePacket();
        }

        void SendBitePacket()
        {
            long now = capi.World.ElapsedMilliseconds;
            if (now - lastClientBiteSendAt < 100) return;
            lastClientBiteSendAt = now;
            clientChannel?.SendPacket(new GriffonAttackPacket { Pressed = true });
        }

        void OnGrabbedPlayerState(GriffonGrabbedPlayerPacket packet)
        {
            SetLocalGrabLock(packet != null && packet.Grabbed);
        }

        void SetLocalGrabLock(bool grabbed)
        {
            if (capi == null || localPlayerGrabbed == grabbed) return;
            localPlayerGrabbed = grabbed;

            if (grabbed)
            {
                // Downed-style restraint: consume movement/inventory hotkeys while the
                // player is involuntarily held. Save every original handler so normal
                // controls are restored byte-for-byte when R releases the player.
                foreach (var entry in capi.Input.HotKeys)
                {
                    HotKey hotkey = entry.Value;
                    if (hotkey == null) continue;
                    bool block = hotkey.KeyCombinationType == HotkeyType.MovementControls
                        || hotkey.KeyCombinationType == HotkeyType.InventoryHotkeys
                        || (!string.IsNullOrEmpty(hotkey.Code) && hotkey.Code.IndexOf("inventory", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!block || blockedHotkeyHandlers.ContainsKey(entry.Key)) continue;
                    blockedHotkeyHandlers[entry.Key] = hotkey.Handler;
                    hotkey.Handler = _ => true;
                }
            }
            else
            {
                foreach (var entry in blockedHotkeyHandlers)
                {
                    if (capi.Input.HotKeys.TryGetValue(entry.Key, out HotKey hotkey) && hotkey != null)
                    {
                        hotkey.Handler = entry.Value;
                    }
                }
                blockedHotkeyHandlers.Clear();
            }
        }

        void OnClientGrabLockTick(float dt)
        {
            UpdateReleasedAbilityKeys();
            PollMountedAbilityInput();
            if (!localPlayerGrabbed || capi?.World?.Player?.Entity == null) return;
            EntityPlayer player = capi.World.Player.Entity;
            player.Controls?.StopAllMovement();
            if (player.ServerControls != null) player.ServerControls.StopAllMovement();
            // Sneak is also vanilla's voluntary detach input. Keep it false before the
            // mount code can turn an attempted crouch into an unmount/remount snap.
            if (player.Controls != null) player.Controls.Sneak = false;
            if (player.ServerControls != null) player.ServerControls.Sneak = false;
        }

        void SendGrabbedPlayerState(EntityPlayer player, bool grabbed)
        {
            if (serverChannel == null || player?.Player == null) return;
            IServerPlayer serverPlayer = player.Player as IServerPlayer;
            if (serverPlayer != null) serverChannel.SendPacket(new GriffonGrabbedPlayerPacket { Grabbed = grabbed }, serverPlayer);
        }

        void OnBitePacket(IServerPlayer fromPlayer, GriffonAttackPacket packet)
        {
            if (packet == null || !packet.Pressed || fromPlayer?.Entity == null) return;

            EntityAgent rider = fromPlayer.Entity;
            Entity mount = rider.MountedOn?.Entity;
            if (!CanUseAbilities(mount, rider)) { sapi.Logger.Notification("[FSW abilities] Attack rejected: sender is not the active supported mount driver."); return; }
            sapi.Logger.Notification("[FSW abilities] Server received attack for mount {0}", mount.EntityId);

            // The packet is accepted only for the mount the sender is currently riding.
            long now = sapi.World.ElapsedMilliseconds;
            if (nextBiteAtByMount.TryGetValue(mount.EntityId, out long nextAt) && now < nextAt) return;
            nextBiteAtByMount[mount.EntityId] = now + CooldownMs;

            mount.StopAnimation("attack"); mount.StartAnimation("attack");
            

            long mountId = mount.EntityId;
            string riderUid = fromPlayer.PlayerUID;
            sapi.World.RegisterCallback(_ => ResolveBite(mountId, riderUid), DamageDelayMs);
        }

        void OnGrabPacket(IServerPlayer fromPlayer, GriffonGrabPacket packet)
        {
            if (packet == null || !packet.Pressed || fromPlayer?.Entity == null) return;

            EntityAgent rider = fromPlayer.Entity;
            Entity mount = rider.MountedOn?.Entity;
            if (!CanUseAbilities(mount, rider) || !CanGrabWithMount(mount)) return;

            // R uses the same mounted bite animation as F, but remains a distinct
            // grab/release action and does not schedule bite damage.
            mount.StopAnimation("attack"); mount.StartAnimation("attack");

            if (grabbedEntityByMount.TryGetValue(mount.EntityId, out long existingId))
            {
                ReleaseGrab(mount.EntityId, existingId);
                return;
            }

            long now = sapi.World.ElapsedMilliseconds;
            if (nextGrabAtByMount.TryGetValue(mount.EntityId, out long grabAt) && now < grabAt) return;
            nextGrabAtByMount[mount.EntityId] = now + 350;
            Entity target = FindGrabTarget(mount, rider);
            if (target == null) return;
            // Players use Vintage Story's passenger/seat system (the same core idea
            // used by Carried) rather than coordinate forcing. Creatures keep the
            // dedicated carried-state path.
            if (target is EntityPlayer playerTarget)
            {
                // Mark the player as involuntarily grabbed BEFORE mounting. A Harmony
                // guard on EntityAgent.TryUnmount uses this flag to reject vanilla
                // crouch/Shift dismounts at the source, so there is no detach/remount jerk.
                playerTarget.WatchedAttributes.SetBool("fswGriffonGrabbedPlayer", true);
                if (!TryMountPlayerInMouth(mount, playerTarget))
                {
                    playerTarget.WatchedAttributes.SetBool("fswGriffonGrabbedPlayer", false);
                    return;
                }
                grabbedEntityByMount[mount.EntityId] = target.EntityId; driverByMount[mount.EntityId] = fromPlayer.PlayerUID;
                SendGrabbedPlayerState(playerTarget, true);
                return;
            }

            grabbedEntityByMount[mount.EntityId] = target.EntityId; driverByMount[mount.EntityId] = fromPlayer.PlayerUID;
            EnterCreatureCarryState(target);
            HoldGrabbedEntity(mount, target);
        }

        void OnServerTick(float dt)
        {
            if (grabbedEntityByMount.Count == 0) return;
            var releases = new List<KeyValuePair<long, long>>();

            foreach (var pair in grabbedEntityByMount)
            {
                Entity mount = sapi.World.GetEntityById(pair.Key);
                Entity target = sapi.World.GetEntityById(pair.Value);
                if (!IsAbilityMount(mount) || !mount.Alive || target == null || (target is EntityPlayer && !target.Alive) || mount.Pos.Dimension != target.Pos.Dimension)
                {
                    releases.Add(pair);
                    continue;
                }

                EntityAgent driver = GetDriver(mount);
                if (driver is not EntityPlayer driverPlayer || !CanUseAbilities(mount, driver)
                    || !driverByMount.TryGetValue(mount.EntityId, out string expectedDriver)
                    || driverPlayer.Player?.PlayerUID != expectedDriver
                    || !IsOnline(driverPlayer)
                    || (target is EntityPlayer heldPlayer && !IsOnline(heldPlayer)))
                {
                    releases.Add(pair);
                    continue;
                }
                if (target is EntityPlayer playerTarget)
                {
                    // A grabbed Seraph is not a voluntary passenger. Vanilla lets a
                    // mounted player detach with crouch/Shift, which is correct for
                    // normal Rider/elk-style seats but not for the griffon mouth grab.
                    // If the grabbed player tries to self-dismount, immediately put
                    // them back into the dedicated non-controllable FaceAP seat.
                    // ReleaseGrab() removes the grab entry before calling TryUnmount(),
                    // so an intentional R-release is never remounted here.
                    if (playerTarget.MountedOn?.Entity?.EntityId != mount.EntityId)
                    {
                        if (!TryMountPlayerInMouth(mount, playerTarget))
                        {
                            releases.Add(pair);
                        }
                    }
                }
                else
                {
                    HoldGrabbedEntity(mount, target);
                }
            }

            foreach (var pair in releases) ReleaseGrab(pair.Key, pair.Value);
        }

        bool TryMountPlayerInMouth(Entity mount, EntityPlayer player)
        {
            if (player == null || player.MountedOn != null) return false;
            var rideable = mount.GetBehavior<EntityBehaviorRideable>();
            if (rideable == null) return false;

            // Rider is the controllable seat. The added non-controllable FaceAP seat
            // is reserved for R-grabbed Seraphs.
            foreach (var seat in rideable.Seats)
            {
                if (seat == null || seat.Config.Controllable || seat.Config.APName != "FaceAP" || seat.Passenger != null) continue;
                return player.TryMount(seat);
            }
            return false;
        }

        void EnterCreatureCarryState(Entity target)
        {
            // Players keep their separate mounted/network handling. For creatures,
            // put the creature (alive or dead) into a temporary carried state rather than allowing
            // its AI/navigation to keep competing with the mouth anchor.
            if (target == null || target is EntityPlayer) return;

            target.WatchedAttributes.SetBool("fswGriffonCarried", true);
            if (target is EntityAgent agent)
            {
                agent.ServerControls?.StopAllMovement();
                agent.Controls?.StopAllMovement();
                target.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
            }
            target.Pos.Motion.Set(0, 0, 0);
        }

        void HoldGrabbedEntity(Entity mount, Entity target)
        {
            if (target is EntityAgent agent)
            {
                agent.ServerControls?.StopAllMovement();
                agent.Controls?.StopAllMovement();
                // Keep creature AI/pathfinding suspended for the full carry. This is
                // deliberately not applied to players.
                if (!(target is EntityPlayer))
                {
                    target.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.StopTasks();
                }
            }
            target.Pos.Motion.Set(0, 0, 0);

            double yaw = mount.Pos.Yaw;
            double forwardX = Math.Sin(yaw);
            double forwardZ = Math.Cos(yaw);
            // First-pass mouth anchor.  This can later be replaced by a MouthCarry AP
            // for exact bone-relative placement once the AP is added to the shape.
            Vec3d mouth = new Vec3d(
                mount.Pos.X + forwardX * 1.65,
                mount.Pos.Y + 1.35,
                mount.Pos.Z + forwardZ * 1.65
            );
            // Do not use TeleportTo() for every carry update. Teleport packets make the
            // client snap the carried entity to each server sample, which is visible as
            // jitter while the griffon is walking/running. Update the normal authoritative
            // positions instead so Vintage Story can interpolate regular entity movement.
            target.Pos.X = mouth.X;
            target.Pos.Y = mouth.Y;
            target.Pos.Z = mouth.Z;
        }

        void ReleaseGrab(long mountId, long targetId)
        {
            grabbedEntityByMount.Remove(mountId); driverByMount.Remove(mountId);
            Entity target = sapi.World.GetEntityById(targetId);
            if (target != null)
            {
                if (target is EntityPlayer playerTarget)
                {
                    // Clear the involuntary-grab flag BEFORE the intentional R-release
                    // calls TryUnmount, otherwise the crouch guard would also block R.
                    playerTarget.WatchedAttributes.SetBool("fswGriffonGrabbedPlayer", false);
                    SendGrabbedPlayerState(playerTarget, false);
                    if (playerTarget.MountedOn?.Entity?.EntityId == mountId) playerTarget.TryUnmount();
                }
                else
                {
                    target.WatchedAttributes.SetBool("fswGriffonCarried", false);
                }
                target.Pos.Motion.Set(0, 0, 0);
            }
        }

        Entity FindGrabTarget(Entity mount, EntityAgent rider)
        {
            Vec3d origin = mount.Pos.XYZ;
            double yaw = mount.Pos.Yaw;
            double forwardX = Math.Sin(yaw);
            double forwardZ = Math.Cos(yaw);
            double bestScore = double.MaxValue;
            Entity best = null;
            bool pvp = sapi.Server.Config.AllowPvP;

            // Use the same server spatial-query route as the already proven mounted
            // bite targeting. In PVP this includes EntityPlayer directly.
            Entity[] nearby = sapi.World.GetEntitiesAround(origin, GrabRange + 0.8f, GrabVerticalRange, candidate => IsGrabCandidate(mount, rider, candidate, pvp));

            foreach (Entity candidate in nearby)
            {
                ConsiderGrabCandidate(mount, candidate, forwardX, forwardZ, ref bestScore, ref best);
            }
            return best;
        }

        void ConsiderGrabCandidate(Entity mount, Entity candidate, double forwardX, double forwardZ, ref double bestScore, ref Entity best)
        {
            double score = TargetScore(mount, candidate, true);
            if (score < bestScore) { bestScore = score; best = candidate; }
        }
        public static bool IsGrabWhitelisted(Entity entity)
        {
            string path = entity?.Code?.Path?.ToLowerInvariant();
            if (string.IsNullOrEmpty(path)) return false;
            if (entity.Code.Domain == "feverstonewilds" &&
                (path.StartsWith("griffon-cub-", StringComparison.Ordinal) ||
                 path.StartsWith("griffon-juvenile-", StringComparison.Ordinal))) return true;
            string[] families = { "drifter", "chicken", "hen", "rooster", "chick", "pig", "boar", "sow", "goat", "sheep", "deer", "doe", "buck", "fawn", "rabbit", "hare", "raccoon", "fox", "cockatrice" };
            foreach (string family in families) if (path.Contains(family)) return true;
            return false;
        }

        void ResolveBite(long mountId, string riderUid)
        {
            Entity mount = sapi.World.GetEntityById(mountId);

            IServerPlayer riderPlayer = sapi.World.PlayerByUid(riderUid) as IServerPlayer;
            EntityAgent rider = riderPlayer?.Entity;
            if (!CanUseAbilities(mount, rider)) return;

            Entity target = FindBiteTarget(mount, rider);
            if (!(target is EntityAgent targetAgent)) { sapi.Logger.Notification("[FSW abilities] Attack found no eligible target in reach."); return; }

            DamageSource source = new DamageSource()
            {
                Source = EnumDamageSource.Entity,
                SourceEntity = mount,
                CauseEntity = rider,
                Type = EnumDamageType.SlashingAttack,
                DamageTier = 2
            };

            bool hit = targetAgent.ReceiveDamage(source, MountedAttackDamage(mount));
            sapi.Logger.Notification("[FSW abilities] Attack target {0} ({1}), damage {2}, accepted {3}", targetAgent.EntityId, targetAgent.Code, MountedAttackDamage(mount), hit);
        }

        Entity FindBiteTarget(Entity mount, EntityAgent rider)
        {
            Vec3d origin = mount.Pos.XYZ;
            // Entity yaw is the reliable ground-facing direction used by FS Wilds movement.
            // GetViewVector() can reflect look/pitch state and was causing mounted bites to miss ground prey.
            double yaw = mount.Pos.Yaw;
            double forwardX = Math.Sin(yaw);
            double forwardZ = Math.Cos(yaw);
            double bestScore = double.MaxValue;
            Entity best = null;

            Entity[] nearby = sapi.World.GetEntitiesAround(origin, BiteRange + 0.8f, BiteVerticalRange, candidate =>
            {
                if (!(candidate is EntityAgent agent) || !candidate.Alive) return false;
                if (candidate.EntityId == mount.EntityId || candidate.EntityId == rider.EntityId) return false;
                if (candidate.Pos.Dimension != mount.Pos.Dimension) return false;


                if (!CanMountedAttack(mount, rider, candidate)) return false;
                // Respect the companion's existing PVE/PVP mode for manual mounted bites.
                if (candidate is EntityPlayer && (mount.WatchedAttributes.GetInt("fswCompanionPvpMode", 0) == 0 || !sapi.Server.Config.AllowPvP)) return false;
                return true;
            });

            foreach (Entity candidate in nearby)
            {
                double score = TargetScore(mount, candidate, false);
                if (score < bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }

        public static bool IsTamedGriffon(Entity entity)
        {
            string domain = entity?.Code?.Domain;
            string path = entity?.Code?.Path;
            return domain == "feverstonewilds" && path != null && path.StartsWith("griffon-tamed-", StringComparison.OrdinalIgnoreCase);
        }
    }
}
