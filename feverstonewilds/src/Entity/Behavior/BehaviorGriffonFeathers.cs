using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace FeverstoneWilds
{
    public class BehaviorGriffonFeathers : EntityBehavior
    {
        public const string VariantKey = "fswArmFeathers";
        double chance;
        AssetLocation shapeLocation;
        public BehaviorGriffonFeathers(Entity entity) : base(entity) { }
        public override string PropertyName() => "griffonfeathervariant";

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            chance = attributes["chance"].AsDouble(0.4);
            shapeLocation = new AssetLocation(attributes["shape"].AsString());
            if (entity.World.Side == EnumAppSide.Server)
                EnsureVariant(entity.WatchedAttributes, entity.World.Rand, chance);
            else entity.WatchedAttributes.RegisterModifiedListener(VariantKey, RefreshShape);
        }

        public static void EnsureVariant(ITreeAttribute data, Random random, double chance)
        {
            if (data.HasAttribute(VariantKey)) return;
            data.SetBool(VariantKey, random.NextDouble() < chance);
            if (data is SyncedTreeAttribute synced) synced.MarkPathDirty(VariantKey);
        }

        void RefreshShape() => entity.MarkShapeModified();

        public override void OnTesselation(ref Shape entityShape, string shapePathForLogging,
            ref bool shapeIsCloned, ref string[] willDeleteElements)
        {
            if (entity.World.Side != EnumAppSide.Client || entityShape == null) return;
            if (!entity.WatchedAttributes.GetBool(VariantKey)) return;
            var asset = entity.Api.Assets.TryGet(shapeLocation.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
            if (asset == null) return;
            var feathers = asset.ToObject<Shape>();
            if (!shapeIsCloned) { entityShape = entityShape.Clone(); shapeIsCloned = true; }
            // The overlay shares the body's texture code and selected alternate.
            // Convert from the overlay's declared UV space into the main skin's UV space.
            RemapTextures(feathers, entityShape);
            BehaviorGriffonMarkings.ApplyMarkings(feathers.Elements, 0,
                entity.WatchedAttributes.GetBool(BehaviorGriffonMarkings.SpotsKey),
                entity.WatchedAttributes.GetInt(BehaviorGriffonMarkings.EyeKey));
            feathers.Textures.Clear();
            feathers.TextureSizes.Clear();
            feathers.TextureWidth = entityShape.TextureWidth;
            feathers.TextureHeight = entityShape.TextureHeight;
            entityShape.StepParentShape(feathers, null, shapeLocation.ToString(), shapePathForLogging,
                entity.Api.Logger, null);
        }

        public static void RemapTextures(Shape overlay, Shape parent)
        {
            int[] source = overlay.TextureSizes != null && overlay.TextureSizes.TryGetValue("griffonskin8", out var sourceSize)
                ? sourceSize : new[] { overlay.TextureWidth, overlay.TextureHeight };
            int[] target = parent.TextureSizes != null && parent.TextureSizes.TryGetValue("griffonskin8", out var targetSize)
                ? targetSize : new[] { parent.TextureWidth, parent.TextureHeight };
            RemapTextures(overlay.Elements, (float)target[0] / Math.Max(1, source[0]), (float)target[1] / Math.Max(1, source[1]));
        }

        public static void RemapTextures(ShapeElement[] elements, float scaleU = 0.5f, float scaleV = 0.5f)
        {
            if (elements == null) return;
            foreach (var element in elements)
            {
                if (element.FacesResolved != null)
                    foreach (var face in element.FacesResolved)
                    {
                        if (face == null) continue;
                        // VSMC exports newly added, unassigned faces as #null.
                        if (!string.IsNullOrEmpty(face.Texture) && face.Texture != "null" && face.Texture != "griffonskin8") continue;
                        face.Texture = "griffonskin8";
                        if (face.Uv != null)
                            for (int i = 0; i < face.Uv.Length; i++) face.Uv[i] *= i % 2 == 0 ? scaleU : scaleV;
                    }
                RemapTextures(element.Children, scaleU, scaleV);
            }
        }
    }
}
