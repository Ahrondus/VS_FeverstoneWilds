using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace FeverstoneWilds.Commands;

// Use the same normal button background as Yes/No, with independently sized icon art.
public class CompanionIconButton : GuiElementTextButton
{
    readonly IAsset iconAsset;
    LoadedTexture iconTexture;
    int textureSize;

    public CompanionIconButton(ICoreClientAPI api, string icon, ActionConsumable click, ElementBounds bounds)
        : base(api, "", CairoFont.WhiteSmallText().WithFontSize(10),
            CairoFont.WhiteSmallText().WithFontSize(10), click, bounds, EnumButtonStyle.Normal)
    {
        iconAsset = api.Assets.Get(new AssetLocation("feverstonewilds:textures/gui/" + icon + ".svg"));
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        base.RenderInteractiveElements(deltaTime);
        if (!Visible) return;
        int size = (int)scaled(GuiElementDialogTitleBar.unscaledCloseIconSize);
        if (iconTexture == null || textureSize != size)
        {
            iconTexture?.Dispose();
            iconTexture = api.Gui.Icons.GenTexture(size, size, iconAsset, GuiStyle.DialogDefaultTextColor);
            textureSize = size;
        }
        api.Render.Render2DTexturePremultipliedAlpha(iconTexture.TextureId,
            Bounds.absX + (Bounds.OuterWidth - size) / 2,
            Bounds.absY + (Bounds.OuterHeight - size) / 2, (double)size, (double)size);
    }

    public override void Dispose()
    {
        iconTexture?.Dispose();
        base.Dispose();
    }
}

public static class CompanionIconButtonComposer
{
    public static GuiComposer AddCompanionIconButton(this GuiComposer composer, string icon,
        ActionConsumable click, ElementBounds bounds, string key)
    {
        composer.AddInteractiveElement(new CompanionIconButton(composer.Api, icon, click, bounds), key);
        return composer;
    }
}
