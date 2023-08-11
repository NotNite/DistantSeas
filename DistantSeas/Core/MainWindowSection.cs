using System;
using Dalamud.Interface;

namespace DistantSeas.Core;

public abstract class MainWindowSection : IDisposable {
    public FontAwesomeIcon Icon;
    public string Name;
    public MainWindowCategory Category;

    protected MainWindowSection(
        FontAwesomeIcon icon,
        string name,
        MainWindowCategory category
    ) {
        this.Icon = icon;
        this.Name = name;
        this.Category = category;
    }

    public abstract void Draw();
    public virtual void Dispose() { }
}
