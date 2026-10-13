using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
static class ClientFiles {
    public static bool Supported(string path) {
        string ext=Path.GetExtension(path);
        return String.Equals(ext,".exe",StringComparison.OrdinalIgnoreCase)||String.Equals(ext,".bin",StringComparison.OrdinalIgnoreCase);
    }
    public static string[] Patterns(IEnumerable<string> selected,string launcher) {
        var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string dir=Path.GetDirectoryName(Path.GetFullPath(launcher));
        paths.Add(Path.GetFullPath(launcher));
        foreach(string p in selected) {
            string full=Path.GetFullPath(p);
            if(!Supported(full)||!File.Exists(full))throw new Exception("Geçerli .exe veya .bin client dosyası seçin.");
            if(!String.Equals(Path.GetDirectoryName(full),dir,StringComparison.OrdinalIgnoreCase))throw new Exception("Client ve launcher aynı oyun klasöründe olmalı.");
            paths.Add(full);
        }
        foreach(string name in new string[]{"metin2client.bin","sezar2client.bin"}) {
            string p=Path.Combine(dir,name);if(File.Exists(p))paths.Add(p);
        }
        var patterns=new List<string>();foreach(string p in paths)patterns.Add("(?i)^"+Regex.Escape(p)+"$");return patterns.ToArray();
    }
}
