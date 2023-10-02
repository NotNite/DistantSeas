using System;
using CheapLoc;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.GameFonts;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DistantSeas.Core;
using DistantSeas.Fishing;
using DistantSeas.Tracking;

namespace DistantSeas;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Distant Seas";
    private const string CommandName = "/pseas";

    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    public static IStateTracker NormalStateTracker = null!;
    public static DebugStateTracker DebugStateTracker = null!;

    public static IStateTracker StateTracker =>
        Configuration.UseDebugStateTracker ? DebugStateTracker : NormalStateTracker;

    public static ResourceManager ResourceManager = null!;
    public static LocalizationManager LocalizationManager = null!;
    public static FishData FishData = null!;
    public static BaitManager BaitManager = null!;
    public static AlarmManager AlarmManager = null!;
    public static Journal Journal = null!;
    public static AchievementTracker AchievementTracker = null!;

    public static Configuration Configuration = null!;
    public static WindowManager WindowManager = null!;

    public static GameFontHandle HeaderFontHandle = null!;

    public static event Action? EnteredOceanFishing;
    public static event Action? ExitedOceanFishing;

    public Plugin() {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        Journal = new Journal();
        AchievementTracker = new AchievementTracker();
        NormalStateTracker = new NormalStateTracker();
        DebugStateTracker = new DebugStateTracker();

        ResourceManager = new ResourceManager();
        LocalizationManager = new LocalizationManager();
        FishData = new FishData();
        BaitManager = new BaitManager();
        AlarmManager = new AlarmManager();

        WindowManager = new WindowManager();

        CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand) {
            HelpMessage = Loc.Localize("CommandHelpMessage", "Open the Distant Seas window.")
        });

        var style = new GameFontStyle(GameFontFamilyAndSize.Axis36);
        HeaderFontHandle = PluginInterface.UiBuilder.GetGameFontHandle(style);
    }

    public void Dispose() {
        Configuration.Save();
        CommandManager.RemoveHandler(CommandName);

        WindowManager.Dispose();

        NormalStateTracker.Dispose();
        DebugStateTracker.Dispose();

        Journal.Dispose();
        AchievementTracker.Dispose();
        AlarmManager.Dispose();
        BaitManager.Dispose();

        HeaderFontHandle.Dispose();
    }

    private void OnCommand(string command, string args) {
        WindowManager.OpenUi();
    }

    public static void DispatchEnterExit(bool enter) {
        if (enter) {
            EnteredOceanFishing?.Invoke();
        } else {
            ExitedOceanFishing?.Invoke();
        }
    }
}
