using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using DistantSeas.Core;
using DistantSeas.Windows;

namespace DistantSeas.Core;

public class WindowManager : IDisposable {
    private WindowSystem windowSystem = new("DistantSeas");
    private List<DistantSeasWindow> windows = new();

    public WindowManager() {
        this.InitializeWindows();
        Plugin.PluginInterface.UiBuilder.Draw += this.Draw;
        Plugin.PluginInterface.UiBuilder.OpenConfigUi += this.OpenUi;
    }

    public void Dispose() {
        this.windowSystem.RemoveAllWindows();
        this.windows.ForEach(window => window.Dispose());
        this.windows.Clear();

        Plugin.PluginInterface.UiBuilder.Draw -= this.Draw;
    }
    
    public void OpenUi() {
        this.GetWindow<MainWindow>().IsOpen = true;
    }
    
    public T GetWindow<T>() where T : DistantSeasWindow {
        foreach (var window in this.windows) {
            if (window is T w) return w;
        }

        PluginLog.Warning("Window not found?: {0}", typeof(T).Name);
        return null!;
    }

    private void InitializeWindows() {
        var reflectedWindows = Plugin.ResourceManager.DistantSeasAssembly
                               .GetTypes()
                               .Where(type => type.IsSubclassOf(typeof(DistantSeasWindow)));

        foreach (var windowType in reflectedWindows) {
            var window = (DistantSeasWindow) Activator.CreateInstance(windowType)!;
            this.windows.Add(window);
            this.windowSystem.AddWindow(window);
        }
    }

    private void Draw() {
        this.windowSystem.Draw();
    }
}
