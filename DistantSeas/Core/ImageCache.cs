using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiScene;

namespace DistantSeas.Core; 

public class ImageCache : IDisposable {
    private Dictionary<uint, TextureWrap> icons = new();
    private Dictionary<string, TextureWrap> textures = new();
    
    public TextureWrap GetIcon(uint id) {
        if (this.icons.TryGetValue(id, out var cached)) return cached;

        var texture = Plugin.TextureProvider.GetIcon(id)!;
        this.icons.Add(id, texture);
        return texture;
    }

    public TextureWrap GetTextureFromGame(string path) {
        if (this.textures.TryGetValue(path, out var cached)) return cached;

        var texture = Plugin.TextureProvider.GetTextureFromGame(path)!;
        this.textures.Add(path, texture);
        return texture;
    }

    public TextureWrap GetTextureFromFile(string path) {
        if (this.textures.TryGetValue(path, out var cached)) return cached;
        
        var fileInfo = new FileInfo(path);
        var texture = Plugin.TextureProvider.GetTextureFromFile(fileInfo)!;
        this.textures.Add(path, texture);
        return texture;
    }

    public void Dispose() {
        this.icons.Values.ToList().ForEach(x => x.Dispose());
        this.icons.Clear();
        
        this.textures.Values.ToList().ForEach(x => x.Dispose());
        this.textures.Clear();
    }
}
