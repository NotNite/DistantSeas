using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace DistantSeas.Core;

// Literally just here for reflection's sake
public class DistantSeasWindow : Window, IDisposable {
    public DistantSeasWindow(
        string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false
    ) : base(name, flags, forceMainWindow) { }

    public override void Draw() {
        throw new NotImplementedException();
    }

    public virtual void Dispose() { }
}
