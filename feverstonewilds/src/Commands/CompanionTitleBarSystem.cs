using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace FeverstoneWilds.Commands;

// Retain native dragging, menu and close handling, but use the surrounding panel background.
public class CompanionTitleBar : GuiElementDialogTitleBar
{
    public CompanionTitleBar(ICoreClientAPI api, GuiComposer composer, Action close,
        CairoFont font, ElementBounds bounds) : base(api, "", composer, close, font, bounds) { }
}

public class CompanionTitleBarSystem : ModSystem
{
    const string PatchId = "feverstonewilds.companiontitlebar";
    Harmony patches;

    public override void StartClientSide(ICoreClientAPI api) => patches = Install();

    public static Harmony Install()
    {
        var harmony = new Harmony(PatchId);
        harmony.Patch(AccessTools.Method(typeof(GuiElementDialogTitleBar), "ComposeTextElements"),
            transpiler: new HarmonyMethod(typeof(CompanionTitleBarSystem), nameof(UsePanelBackground)));
        return harmony;
    }

    public static bool UsesPanelBackground(GuiElementDialogTitleBar bar) => bar is CompanionTitleBar;

    static IEnumerable<CodeInstruction> UsePanelBackground(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var code = instructions.ToList();
        // VS 1.22.7: bounds initialization precedes the title background; text/icons follow it.
        int bounds = code.FindIndex(i => i.operand is MethodInfo m &&
            m.DeclaringType == typeof(ElementBounds) && m.Name == "CalcWorldBounds");
        int font = code.FindIndex(i => i.operand is MethodInfo m &&
            m.DeclaringType == typeof(CairoFont) && m.Name == "SetupContext");
        int textStart = font - 3; // ldarg.0, ldfld Font, ldarg.1, callvirt SetupContext
        int backgroundStart = bounds + 3; // preserve ldc.r8 5 / stloc.0 used by vanilla composition
        if (bounds < 0 || font < 0 || textStart <= backgroundStart ||
            code[textStart].opcode != OpCodes.Ldarg_0 || code[textStart + 1].opcode != OpCodes.Ldfld ||
            code[backgroundStart - 1].opcode != OpCodes.Stloc_0)
            throw new InvalidOperationException("Unsupported companion title bar composition layout.");

        Label textLabel = generator.DefineLabel();
        code[textStart].labels.Add(textLabel);
        var check = new CodeInstruction(OpCodes.Ldarg_0);
        check.labels.AddRange(code[backgroundStart].labels);
        code[backgroundStart].labels.Clear();
        code.InsertRange(backgroundStart, new[] {
            check,
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompanionTitleBarSystem), nameof(UsesPanelBackground))),
            new CodeInstruction(OpCodes.Brtrue, textLabel)
        });
        return code;
    }

    public override void Dispose()
    {
        patches?.UnpatchAll(PatchId);
        base.Dispose();
    }
}

public static class CompanionTitleBarComposer
{
    public static GuiComposer AddCompanionTitleBar(this GuiComposer composer, Action close,
        CairoFont font, ElementBounds bounds)
    {
        composer.AddInteractiveElement(new CompanionTitleBar(composer.Api, composer, close, font, bounds), "companionTitleBar");
        return composer;
    }
}
