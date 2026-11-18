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
public class FSWildsCommands : GuiDialog
    {
        readonly Entity companion;
        int moveMode;
        int stance;
        int pvpMode;
        FswCompanionReleaseConfirm releaseDialog;
        FswCompanionAccessModes accessDialog;

        public override string ToggleKeyCombinationCode => null;
        public override bool UnregisterOnClose => true;

        public FSWildsCommands(ICoreClientAPI capi, Entity companion, int moveMode, int stance, int pvpMode) : base(capi)
        {
            this.companion = companion;
            this.moveMode = moveMode;
            this.stance = stance;
            this.pvpMode = pvpMode;
            SetupDialog();
        }

        void SetupDialog()
        {
            // Companion-card layout: portrait/owner column on the left and commands on the right.
            // No Vintage Story title bar is used; the entire dialog is one continuous brown panel.
            ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, 525, 365).WithAlignment(EnumDialogArea.CenterMiddle);
            // Keep the shaded background fully inside the composer bounds. Negative-Y
            // background bounds can crash Vintage Story while the dialog is composing.
            ElementBounds bgBounds = ElementBounds.Fill;

            string ownerName = companion.WatchedAttributes.GetString("fswCompanionOwnerName");
            if (string.IsNullOrWhiteSpace(ownerName)) ownerName = "Unknown";

            string companionName = companion.WatchedAttributes.GetString("fswCompanionName");
            if (string.IsNullOrWhiteSpace(companionName)) companionName = companion.GetName();
            bool isFemale = companion.Code != null && companion.Code.Path.IndexOf("female", StringComparison.OrdinalIgnoreCase) >= 0;
            string speciesName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(companion.Code.Path.Replace("tame-", "").Split('-')[0]);

            CairoFont nameFont = CairoFont.WhiteSmallishText().WithFontSize(24);
            CairoFont speciesFont = CairoFont.WhiteSmallishText().WithFontSize(22);
            CairoFont ownerLabelFont = CairoFont.WhiteSmallishText().WithFontSize(20);
            CairoFont ownerNameFont = CairoFont.WhiteSmallishText().WithFontSize(19);
            CairoFont buttonFont = CairoFont.WhiteSmallishText().WithFontSize(18);

            SingleComposer = capi.Gui.CreateCompo("feverstonewilds-companion-" + companion.EntityId, dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                // LEFT COLUMN: restore the reference proportions. Portrait work remains deferred.
                .AddInset(ElementBounds.Fixed(2, 2, 198, 162))
                .AddInset(ElementBounds.Fixed(2, 166, 198, 197))
                .AddStaticText("Owner;", ownerLabelFont, ElementBounds.Fixed(8, 177, 182, 28))
                .AddStaticText(ownerName, ownerNameFont, ElementBounds.Fixed(8, 215, 182, 28))

                // Use the same panel background under the name and commands while retaining native controls.
                .AddCompanionTitleBar(OnTitleBarClose, nameFont, ElementBounds.Fixed(200, 0, 323, 52))
                // Keep the native title-bar controls, but place the companion name ourselves
                // so it stays in the same position as the established custom layout.
                .AddStaticText(companionName, nameFont, ElementBounds.Fixed(211, 8, 225, 32))
                .AddCompanionIconButton("rename-companion", OpenRenameDialog,
                    ElementBounds.Fixed(472, 28, 24, 22), "renameCompanion")
                .AddHoverText("Name companion", CairoFont.WhiteSmallText(), 200,
                    ElementBounds.Fixed(472, 28, 24, 22), "renameCompanionTip")
                .AddCompanionIconButton("release-companion", () => { ReleaseOwnership(); return true; },
                    ElementBounds.Fixed(444, 28, 24, 22), "releaseOwnership")
                .AddHoverText("Release companion ownership", CairoFont.WhiteSmallText(), 250,
                    ElementBounds.Fixed(444, 28, 24, 22), "releaseOwnershipTip")
                .AddCompanionIconButton("gender-" + (isFemale ? "female" : "male"), () => true,
                    ElementBounds.Fixed(499, 28, 24, 22), "companionGender")
                .AddInset(ElementBounds.Fixed(200, 52, 323, 2))

                // Species.
                .AddStaticText(speciesName, speciesFont, ElementBounds.Fixed(211, 60, 266, 30))

                // Movement.
                .AddDynamicText("Movement - Current: " + MoveName(moveMode), CairoFont.WhiteDetailText(), ElementBounds.Fixed(211, 113, 302, 20), "movement")
                .AddButton("Follow", () => SendCommand(0), ElementBounds.Fixed(211, 136, 91, 33))
                .AddButton("Stay", () => SendCommand(1), ElementBounds.Fixed(310, 136, 91, 33))
                .AddButton("Wander", () => SendCommand(2), ElementBounds.Fixed(409, 136, 104, 33))

                // Combat header and PVE/PVP share one row, as in the posted reference.
                .AddDynamicText("Combat stance: " + StanceNameLocal(stance), CairoFont.WhiteDetailText().WithFontSize(12), ElementBounds.Fixed(211, 177, 194, 20), "stance")
                .AddToggleButton("PVE", CairoFont.WhiteSmallishText(), on => { if (on) SendCommand(30); }, ElementBounds.Fixed(407, 173, 49, 28), "pvemode")
                .AddToggleButton("PVP", CairoFont.WhiteSmallishText(), on => { if (on) SendCommand(31); }, ElementBounds.Fixed(464, 173, 49, 28), "pvpmode")

                // Four combat stances in two aligned columns. The existing stance IDs are preserved.
                // Each button uses the same size and existing combat font.
                .AddButton("Passive", () => SendCommand(10), ElementBounds.Fixed(211, 204, 147, 31), buttonFont)
                .AddButton("Defensive", () => SendCommand(11), ElementBounds.Fixed(211, 243, 147, 31), buttonFont)
                .AddButton("Aggressive", () => SendCommand(12), ElementBounds.Fixed(366, 204, 147, 31), buttonFont)

                .AddButton("Protective", () => SendCommand(13), ElementBounds.Fixed(366, 243, 147, 31), buttonFont)

                .AddHoverText(speciesName + " does not attack or retaliate.", CairoFont.WhiteSmallText(), 300,
                    ElementBounds.Fixed(211, 204, 147, 31), "passiveTip")
                .AddHoverText(speciesName + " attacks creatures that hit owner and creatures that owner hits.", CairoFont.WhiteSmallText(), 300,
                    ElementBounds.Fixed(211, 243, 147, 31), "defensiveTip")
                .AddHoverText("Attacks everything within range.", CairoFont.WhiteSmallText(), 300,
                    ElementBounds.Fixed(366, 204, 147, 31), "aggressiveTip")
                .AddHoverText(speciesName + " attacks only creatures that attack owner.", CairoFont.WhiteSmallText(), 300,
                    ElementBounds.Fixed(366, 243, 147, 31), "protectiveTip")

                .AddButton("Recall", () => SendCommand(20), ElementBounds.Fixed(211, 312, 100, 30), buttonFont)
                .AddHoverText("Brings a lost or stuck companion to your position.", CairoFont.WhiteSmallText(), 300,
                    ElementBounds.Fixed(211, 312, 100, 30), "recallTip")
                .AddButton("Access modes", OpenAccessModes, ElementBounds.Fixed(323, 312, 190, 30), buttonFont)
                .Compose();

            SingleComposer.GetToggleButton("pvemode").SetValue(pvpMode == 0);
            SingleComposer.GetToggleButton("pvpmode").SetValue(pvpMode == 1);
        }

        bool OnCloseButton()
        {
            TryClose();
            return true;
        }

        bool OpenRenameDialog()
        {
            if (!CanManageOwnership()) return true;
            FswCompanionRename dialog = new FswCompanionRename(capi, companion);
            dialog.TryOpen();
            return true;
        }

        void ReleaseOwnership()
        {
            if (!CanManageOwnership()) return;
            if (releaseDialog?.IsOpened() == true) return;
            releaseDialog = new FswCompanionReleaseConfirm(capi, companion, () => TryClose(), () => {
                releaseDialog = null;
                if (IsOpened()) capi.Gui.RequestFocus(this);
            });
            releaseDialog.TryOpen();
        }

        bool CanManageOwnership()
        {
            if (FSWildsBehavior.IsOwner(companion, capi.World.Player.PlayerUID)) return true;
            capi.TriggerIngameError(this, "companion-owner-only", "Only the owner can change ownership settings.");
            return false;
        }

        bool OpenAccessModes()
        {
            if (!CanManageOwnership() || accessDialog?.IsOpened() == true) return true;
            accessDialog = new FswCompanionAccessModes(capi, companion, () => {
                accessDialog = null;
                if (IsOpened()) capi.Gui.RequestFocus(this);
            });
            accessDialog.TryOpen();
            return true;
        }

        bool SendCommand(int command)
        {
            // Keep the dialog open when changing commands. Closing it from inside a
            // button callback can leave the compositor's shaded background rendered
            // as a black rectangle. The player can close the menu normally with its
            // title-bar X after making changes.
            capi.Network.SendEntityPacket(companion.EntityId, 8610, new byte[] { (byte)command });
            if (command >= 0 && command <= 2) { moveMode = command; SingleComposer.GetDynamicText("movement").SetNewText("Movement - Current: " + MoveName(moveMode)); }
            if (command >= 10 && command <= 13) { stance = command - 10; SingleComposer.GetDynamicText("stance").SetNewText("Combat stance: " + StanceNameLocal(stance)); }
            if (command == 30) SingleComposer.GetToggleButton("pvpmode").SetValue(false);
            if (command == 31) SingleComposer.GetToggleButton("pvemode").SetValue(false);
            return true;
        }

        void OnTitleBarClose()
        {
            TryClose();
        }

        public override void OnGuiClosed()
        {
            // Let GuiDialog own the composer lifecycle. Manually disposing the
            // SingleComposer here can leave the base dialog trying to update an
            // already-disposed composer and crash in set_SingleComposer.
            base.OnGuiClosed();
            releaseDialog?.TryClose();
            accessDialog?.TryClose();
        }

        static string MoveName(int mode)
        {
            if (mode == 1) return "Stay";
            if (mode == 2) return "Wander";
            return "Follow";
        }

        static string StanceNameLocal(int value)
        {
            if (value == 0) return "Passive";
            if (value == 2) return "Aggressive";
            if (value == 3) return "Protective";
            return "Defensive";
        }
    }

    public class FswCompanionReleaseConfirm : GuiDialog
    {
        readonly Entity companion;
        readonly Action onConfirmed;
        readonly Action onClosed;
        bool decisionQueued;

        public override string ToggleKeyCombinationCode => null;
        public override bool UnregisterOnClose => true;

        public FswCompanionReleaseConfirm(ICoreClientAPI capi, Entity companion, Action onConfirmed, Action onClosed) : base(capi)
        {
            this.companion = companion;
            this.onConfirmed = onConfirmed;
            this.onClosed = onClosed;
            ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            SingleComposer = capi.Gui.CreateCompo("feverstonewilds-companion-release-" + companion.EntityId, bounds)
                .AddShadedDialogBG(ElementBounds.Fixed(0, 0, 460, 180))
                .AddDialogTitleBar("Release companion ownership", () => TryClose())
                .AddStaticText("This will release pet ownership, yes or no to confirm.", CairoFont.WhiteDetailText(),
                    ElementBounds.Fixed(25, 58, 410, 54))
                .AddButton("Yes", OnYes, ElementBounds.Fixed(115, 126, 90, 34))
                .AddButton("No", OnNo, ElementBounds.Fixed(255, 126, 90, 34))
                .Compose();
        }

        bool OnYes()
        {
            return FinishDecision(true);
        }

        bool OnNo()
        {
            return FinishDecision(false);
        }

        bool FinishDecision(bool release)
        {
            if (decisionQueued) return true;
            decisionQueued = true;
            // Finish mouse input before closing/clearing composers to prevent a lingering shaded panel.
            capi.Event.EnqueueMainThreadTask(() => {
                if (!IsOpened()) return;
                if (release) capi.Network.SendEntityPacket(companion.EntityId, 8610, new byte[] { 41 });
                TryClose();
                if (release) onConfirmed?.Invoke();
            }, "fswilds-release-decision");
            return true;
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            capi.Event.EnqueueMainThreadTask(() => {
                ClearComposers();
                onClosed?.Invoke();
            }, "fswilds-release-cleanup");
        }
    }

    public class FswCompanionAccessModes : GuiDialog
    {
        readonly Entity companion;
        readonly Action onClosed;
        int accessMode;
        bool closing;

        public override string ToggleKeyCombinationCode => null;
        public override bool UnregisterOnClose => true;

        public FswCompanionAccessModes(ICoreClientAPI capi, Entity companion, Action onClosed) : base(capi)
        {
            this.companion = companion;
            this.onClosed = onClosed;
            accessMode = companion.WatchedAttributes.GetInt(FSWildsBehavior.AccessModeKey, 0);
            SingleComposer = capi.Gui.CreateCompo("feverstonewilds-companion-access-" + companion.EntityId,
                    ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
                .AddShadedDialogBG(ElementBounds.Fixed(0, 0, 360, 205))
                .AddDialogTitleBar("Access modes", () => TryClose())
                .AddStaticText("Choose who may ride and use commands.", CairoFont.WhiteDetailText(), ElementBounds.Fixed(25, 55, 310, 35))
                .AddToggleButton("Public", CairoFont.WhiteSmallishText(), on => { if (on) SelectMode(0); },
                    ElementBounds.Fixed(30, 98, 140, 34), "public")
                .AddToggleButton("Private", CairoFont.WhiteSmallishText(), on => { if (on) SelectMode(1); },
                    ElementBounds.Fixed(190, 98, 140, 34), "private")
                .AddHoverText("Allows mount to be ridden by public.", CairoFont.WhiteSmallText(), 280,
                    ElementBounds.Fixed(30, 98, 140, 34), "publicTip")
                .AddHoverText("Allows only private use.", CairoFont.WhiteSmallText(), 280,
                    ElementBounds.Fixed(190, 98, 140, 34), "privateTip")
                .AddButton("Confirm", () => Finish(true), ElementBounds.Fixed(55, 155, 110, 32))
                .AddButton("Cancel", () => Finish(false), ElementBounds.Fixed(195, 155, 110, 32))
                .Compose();
            SelectMode(accessMode);
        }

        void SelectMode(int mode)
        {
            accessMode = mode;
            SingleComposer.GetToggleButton("public").SetValue(mode == 0);
            SingleComposer.GetToggleButton("private").SetValue(mode == 1);
        }

        bool Finish(bool confirm)
        {
            if (closing) return true;
            closing = true;
            capi.Event.EnqueueMainThreadTask(() => {
                if (!IsOpened()) return;
                if (confirm) capi.Network.SendEntityPacket(companion.EntityId, 8610, new byte[] { (byte)(50 + accessMode) });
                TryClose();
            }, "fswilds-access-confirm");
            return true;
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            capi.Event.EnqueueMainThreadTask(() => {
                ClearComposers();
                onClosed?.Invoke();
            }, "fswilds-access-cleanup");
        }
    }

    public class FswCompanionRename : GuiDialog
    {
        readonly Entity companion;
        string pendingName;

        public override string ToggleKeyCombinationCode => null;
        public override bool UnregisterOnClose => true;

        public FswCompanionRename(ICoreClientAPI capi, Entity companion) : base(capi)
        {
            this.companion = companion;
            pendingName = companion.WatchedAttributes.GetString("fswCompanionName");
            if (string.IsNullOrWhiteSpace(pendingName)) pendingName = companion.GetName();
            SetupDialog();
        }

        void SetupDialog()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, 390, 155);

            SingleComposer = capi.Gui.CreateCompo("feverstonewilds-companion-rename-" + companion.EntityId, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Rename companion", () => TryClose())
                .AddStaticText("Name", CairoFont.WhiteDetailText(), ElementBounds.Fixed(25, 58, 65, 30))
                .AddTextInput(ElementBounds.Fixed(90, 55, 270, 34), value => pendingName = value, CairoFont.WhiteDetailText(), "nameinput")
                .AddButton("Cancel", OnCancel, ElementBounds.Fixed(105, 105, 80, 34))
                .AddButton("Rename", OnRename, ElementBounds.Fixed(205, 105, 100, 34))
                .Compose();

            SingleComposer.GetTextInput("nameinput").SetValue(pendingName);
        }

        bool OnRename()
        {
            string name = (pendingName ?? "").Trim();
            if (name.Length == 0) name = companion.GetName();
            if (name.Length > 32) name = name.Substring(0, 32);

            byte[] text = Encoding.UTF8.GetBytes(name);
            byte[] packet = new byte[text.Length + 1];
            packet[0] = 40;
            Buffer.BlockCopy(text, 0, packet, 1, text.Length);
            capi.Network.SendEntityPacket(companion.EntityId, 8610, packet);
            TryClose();
            return true;
        }

        bool OnCancel()
        {
            TryClose();
            return true;
        }
    }

}
