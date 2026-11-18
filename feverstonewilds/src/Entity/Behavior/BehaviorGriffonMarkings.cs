using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace FeverstoneWilds
{
    public class BehaviorGriffonMarkings : EntityBehavior
    {
        public const string WingKey = "fswWingtipVariant";
        public const string SpotsKey = "fswBodySpots";
        public const string EyeKey = "fswEyeVariant";
        // Keep the original dark tips at IDs 1/2 so existing griffons retain their markings.
        public static readonly string[] WingOverlays = {
            "dark1", "dark2", "white1", "white2", "gray1", "gray2",
            "bronze1", "bronze2", "gold1", "gold2", "navy1", "navy2", "crimson1", "crimson2"
        };
        public static readonly string[] EyeOverlays = { "gray", "red", "yellow", "gold", "blue", "green" };
        public BehaviorGriffonMarkings(Entity entity) : base(entity) { }
        public override string PropertyName() => "griffonmarkings";

        public static int SelectWing(double roll) => roll < 0.6
            ? 1 + Math.Min(WingOverlays.Length - 1, (int)(roll / 0.6 * WingOverlays.Length)) : 0;
        public static void EnsureVariants(ITreeAttribute data, Random random)
        {
            if (!data.HasAttribute(WingKey))
            {
                data.SetInt(WingKey, SelectWing(random.NextDouble()));
                if (data is SyncedTreeAttribute synced) synced.MarkPathDirty(WingKey);
            }
            if (!data.HasAttribute(SpotsKey))
            {
                data.SetBool(SpotsKey, random.NextDouble() < 0.3);
                if (data is SyncedTreeAttribute synced) synced.MarkPathDirty(SpotsKey);
            }
            if (!data.HasAttribute(EyeKey))
            {
                data.SetInt(EyeKey, 1 + random.Next(EyeOverlays.Length));
                if (data is SyncedTreeAttribute synced) synced.MarkPathDirty(EyeKey);
            }
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            if (entity.World.Side == EnumAppSide.Server) EnsureVariants(entity.WatchedAttributes, entity.World.Rand);
            else
            {
                entity.WatchedAttributes.RegisterModifiedListener(WingKey, Refresh);
                entity.WatchedAttributes.RegisterModifiedListener(SpotsKey, Refresh);
                entity.WatchedAttributes.RegisterModifiedListener(EyeKey, Refresh);
            }
        }
        void Refresh() => entity.MarkShapeModified();

        public override void OnTesselation(ref Shape entityShape, string shapePathForLogging,
            ref bool shapeIsCloned, ref string[] willDeleteElements)
        {
            if (entity.World.Side != EnumAppSide.Client || entityShape == null || entity.Properties.Client?.Textures == null) return;
            int wing = Math.Clamp(entity.WatchedAttributes.GetInt(WingKey), 0, WingOverlays.Length);
            bool spots = entity.WatchedAttributes.GetBool(SpotsKey);
            int eye = Math.Clamp(entity.WatchedAttributes.GetInt(EyeKey), 0, EyeOverlays.Length);
            if (wing == 0 && !spots && eye == 0) return;
            // Cub UVs differ: choices are saved now, displayed on adolescent/adult skins.
            if (!entity.Properties.Client.Textures.ContainsKey("fswgriffonspots")) return;
            if (!shapeIsCloned) { entityShape = entityShape.Clone(); shapeIsCloned = true; }
            ApplyMarkings(entityShape.Elements, wing, spots, eye);
            if (spots) entityShape.TextureSizes["fswgriffonspots"] = new[] { 16, 16 };
            if (wing > 0) entityShape.TextureSizes["fswgriffonwingtip" + wing] = new[] { 16, 16 };
            if (eye > 0) entityShape.TextureSizes[BodyTexture(spots, eye)] = new[] { 16, 16 };
        }

        static string BodyTexture(bool spots, int eye) => eye > 0
            ? (spots ? "fswgriffonspotseye" : "fswgriffoneye") + eye : spots ? "fswgriffonspots" : null;

        public static void ApplyMarkings(ShapeElement[] elements, int wing, bool spots, int eye = 0)
        {
            if (elements == null) return;
            foreach (var element in elements)
            {
                // Shape.Clone shares face objects with the source model.
                if (element.FacesResolved != null)
                    for (int i = 0; i < element.FacesResolved.Length; i++)
                    {
                        var face = element.FacesResolved[i];
                        if (face == null) continue;
                        string code = face.Texture == "griffonskin8" ? BodyTexture(spots, eye)
                            : face.Texture == "griffonskin8Wing" && wing > 0 ? "fswgriffonwingtip" + wing : null;
                        if (code == null) continue;
                        element.FacesResolved[i] = new ShapeElementFace
                        {
                            Texture = code, Uv = (float[])face.Uv?.Clone(),
                            Rotation = face.Rotation, Glow = face.Glow, Enabled = face.Enabled,
                            ReflectiveMode = face.ReflectiveMode,
                            WindMode = (sbyte[])face.WindMode?.Clone(), WindData = (sbyte[])face.WindData?.Clone()
                        };
                    }
                ApplyMarkings(element.Children, wing, spots, eye);
            }
        }

        public static void CopyVariants(Entity source, Entity target)
        {
            foreach (string key in new[] { WingKey, SpotsKey, EyeKey, BehaviorGriffonFeathers.VariantKey })
                if (source.WatchedAttributes.HasAttribute(key))
                    target.WatchedAttributes[key] = source.WatchedAttributes[key].Clone();
        }
    }
}
