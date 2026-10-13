using System;
using System.Collections.Generic;
using System.Text;
sealed class EngineLogBuffer {
    readonly object gate=new object();readonly Queue<string> lines=new Queue<string>();long dropped;
    public void Add(string line){if(String.IsNullOrEmpty(line))return;if(line.Length>4096)line=line.Substring(0,4096)+" [satır kısaltıldı]";lock(gate){if(lines.Count>=500){dropped++;return;}lines.Enqueue(line);}}
    public string Drain(){lock(gate){var text=new StringBuilder();if(dropped>0){text.AppendLine("Yoğun log akışı: "+dropped+" satır atlandı; kuyruk sınırlandı.");dropped=0;}int count=0;while(lines.Count>0&&count++<100&&text.Length<24000)text.AppendLine(lines.Dequeue());return text.ToString();}}
    public void Clear(){lock(gate){lines.Clear();dropped=0;}}
    public int Count {get{lock(gate)return lines.Count;}}
}
