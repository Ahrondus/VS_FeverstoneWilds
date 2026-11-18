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
public class FswCompanionMapSystem : ModSystem
    {
        static ICoreServerAPI sapi;
        static IServerNetworkChannel serverChannel;
        ICoreClientAPI capi;
        long clientTick; bool serverSide, mapDirty, disposing;
        readonly Dictionary<long, FswCompanionMapPacket> records = new Dictionary<long, FswCompanionMapPacket>();

        public override void Start(ICoreAPI api)
        {
            api.Network.RegisterChannel("feverstonewilds-companionmap").RegisterMessageType(typeof(FswCompanionMapPacket));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api; serverSide = true;
            serverChannel = api.Network.GetChannel("feverstonewilds-companionmap");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            clientTick = api.Event.RegisterGameTickListener(_ => { if (mapDirty || records.Count > 0) { SyncVanillaOwnedCreatures(); mapDirty = false; } }, 1000);
            api.Network.GetChannel("feverstonewilds-companionmap").SetMessageHandler<FswCompanionMapPacket>(OnMapPacket);
        }

        public static void SendUpdate(Entity entity, bool remove = false)
        {
            if (sapi == null || serverChannel == null || entity == null) return;
            string ownerUid = entity.WatchedAttributes.GetString("fswCompanionOwnerUid");
            if (string.IsNullOrEmpty(ownerUid)) return;
            // Companion locations are private to their owner. Do not broadcast them to
            // group/friendly players; this prevents the tracker from exposing another
            // player's tame locations.
            IServerPlayer owner = sapi.World.PlayerByUid(ownerUid) as IServerPlayer;
            if (owner == null || owner.ConnectionState != EnumClientState.Playing) return;

            serverChannel.SendPacket(new FswCompanionMapPacket()
            {
                EntityId = entity.EntityId,
                X = entity.Pos.X,
                Y = entity.Pos.Y,
                Z = entity.Pos.Z,
                Dimension = entity.Pos.Dimension,
                Yaw = entity.Pos.Yaw,
                Remove = remove,
                Name = GetCompanionName(entity)
            }, owner);
        }

        void OnMapPacket(FswCompanionMapPacket packet)
        {
            if (packet == null || capi == null) return;
            if (packet.Remove) records.Remove(packet.EntityId);
            else records[packet.EntityId] = packet;
            mapDirty = true;
        }

        // Called only on a companion's true/final death. The live tracking marker is
        // removed, the owner receives a private death notice, and a persistent vanilla
        // gravestone waypoint is placed at the companion's last position.
        public static void HandleCompanionDeath(Entity entity)
        {
            if (sapi == null || entity == null) return;

            string ownerUid = entity.WatchedAttributes.GetString("fswCompanionOwnerUid");
            if (string.IsNullOrEmpty(ownerUid)) return;

            IServerPlayer owner = sapi.World.PlayerByUid(ownerUid) as IServerPlayer;
            if (owner == null) return;

            // Remove the moving companion marker first.
            if (owner.ConnectionState == EnumClientState.Playing && serverChannel != null)
            {
                serverChannel.SendPacket(new FswCompanionMapPacket()
                {
                    EntityId = entity.EntityId,
                    X = entity.Pos.X,
                    Y = entity.Pos.Y,
                    Z = entity.Pos.Z,
                    Dimension = entity.Pos.Dimension,
                Yaw = entity.Pos.Yaw,
                    Remove = true,
                    Name = GetCompanionName(entity)
                }, owner);
            }

            string petName = GetCompanionName(entity);
            if (string.IsNullOrEmpty(petName)) petName = "FS Wilds companion";
            int x = (int)Math.Floor(entity.Pos.X);
            int y = (int)Math.Floor(entity.Pos.Y);
            int z = (int)Math.Floor(entity.Pos.Z);

            if (owner.ConnectionState == EnumClientState.Playing)
            {
                owner.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    "Your companion " + petName + " has died. Last location: X: " + x + ", Y: " + y + ", Z: " + z,
                    EnumChatType.Notification
                );
            }

            AddGravestoneWaypoint(owner, entity, petName);
        }

        static void AddGravestoneWaypoint(IServerPlayer owner, Entity entity, string petName)
        {
            try
            {
                WorldMapManager wm = sapi.ModLoader.GetModSystem<WorldMapManager>();
                if (wm == null) return;

                // Avoid direct access to WorldMapManager.MapLayers because VS 1.22's
                // runtime source compiler can otherwise hit the System.Collections 10 mismatch.
                FieldInfo layersField = wm.GetType().GetField("MapLayers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                System.Collections.IEnumerable layers = layersField?.GetValue(wm) as System.Collections.IEnumerable;
                if (layers == null) return;

                foreach (object layer in layers)
                {
                    WaypointMapLayer waypointLayer = layer as WaypointMapLayer;
                    if (waypointLayer == null) continue;

                    Waypoint waypoint = new Waypoint()
                    {
                        Color = ColorUtil.ColorFromRgba(255, 255, 255, 255),
                        OwningPlayerUid = owner.PlayerUID,
                        Position = entity.Pos.XYZ.Clone(),
                        Title = petName + " - Gravestone",
                        Icon = "gravestone",
                        Pinned = true,
                        Guid = Guid.NewGuid().ToString()
                    };

                    waypointLayer.AddWaypoint(waypoint, owner);
                    break;
                }
            }
            catch (Exception e)
            {
                sapi.Logger.Warning("[FS Wilds] Could not create companion gravestone waypoint: " + e.Message);
            }
        }

        void SyncVanillaOwnedCreatures()
        {
            ModSystemEntityOwnership ownership = capi.ModLoader.GetModSystem<ModSystemEntityOwnership>();
            if (ownership == null) return;

            // Use PaleoWilds' compatibility path to the native owned-creature registry.
            PropertyInfo shipsProp = ownership.GetType().GetProperty("SelfOwnerShips", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            System.Collections.IDictionary ships = shipsProp?.GetValue(ownership) as System.Collections.IDictionary;
            if (ships == null) return;

            if (MergeRecords(ships, records) && !disposing) ReloadOwnedCreatureLayer();
        }

        public static bool MergeRecords(System.Collections.IDictionary ships, IDictionary<long, FswCompanionMapPacket> records)
        {
            bool changed = false;
            var removeKeys = new System.Collections.ArrayList();
            foreach (object keyObj in ships.Keys)
            {
                if (keyObj is string key && key.StartsWith("feverstonewilds-companion-") &&
                    (!long.TryParse(key.Substring("feverstonewilds-companion-".Length), out long id) || !records.ContainsKey(id)))
                    removeKeys.Add(key);
            }
            foreach (object key in removeKeys) { ships.Remove(key); changed = true; }
            foreach (FswCompanionMapPacket rec in records.Values)
            {
                string key = "feverstonewilds-companion-" + rec.EntityId;
                if (ships[key] is not EntityOwnership entry)
                {
                    entry = new EntityOwnership { EntityId = rec.EntityId, Pos = new EntityPos(), Color = "#f5a000" };
                    ships[key] = entry;
                    changed = true;
                }
                entry.Pos.X = rec.X; entry.Pos.Y = rec.Y; entry.Pos.Z = rec.Z;
                entry.Pos.Dimension = rec.Dimension;
                entry.Pos.Yaw = rec.Yaw;
                entry.Name = string.IsNullOrEmpty(rec.Name) ? "FS Wilds companion" : rec.Name;
            }
            // Existing components retain these EntityOwnership references and read their positions live.
            return changed;
        }
        void ReloadOwnedCreatureLayer()
        {
            try
            {
                WorldMapManager wm = capi.ModLoader.GetModSystem<WorldMapManager>();
                if (wm == null || wm.worldMapDlg == null || !wm.worldMapDlg.IsOpened()) return;

                // Reflection avoids directly touching the MapLayers List<> property, which
                // triggers the VS 1.22 runtime source compiler's System.Collections mismatch.
                object dlg = wm.worldMapDlg;
                PropertyInfo prop = dlg.GetType().GetProperty("MapLayers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                System.Collections.IEnumerable layers = (prop?.GetValue(dlg) ?? dlg.GetType().GetField("MapLayers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(dlg)) as System.Collections.IEnumerable;
                if (layers == null) return;
                foreach (object layer in layers)
                {
                    if (layer is OwnedEntityMapLayer ownedLayer)
                    {
                        ownedLayer.Reload();
                        break;
                    }
                }
            }
            catch { }
        }

        public static void PrepareGrowth(Entity entity)
        {
            entity.Attributes.SetBool("fswCompanionGrowing", true);
            SendUpdate(entity, true);
        }

        public override void Dispose()
        {
            disposing = true;
            if (capi != null)
            {
                capi.Event.UnregisterGameTickListener(clientTick);
                records.Clear();
                SyncVanillaOwnedCreatures();
            }
            if (serverSide) { sapi = null; serverChannel = null; }
            base.Dispose();
        }
        static string GetCompanionName(Entity entity)
        {
            string name = entity?.WatchedAttributes?.GetString("fswCompanionName");
            if (string.IsNullOrWhiteSpace(name)) name = entity?.GetName();
            if (string.IsNullOrWhiteSpace(name)) name = "FS Wilds companion";
            return name;
        }

    }
}
