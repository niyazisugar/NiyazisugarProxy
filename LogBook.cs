using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
sealed class LogEntry {public string Time,Level,Message,Detail;public long Id;}
sealed class LogBook {
    readonly Queue<LogEntry> rows=new Queue<LogEntry>();long next;public int Revision {get;private set;}
    static readonly Regex prefix=new Regex(@"^(?:[+-]\d{4}\s+)?\d{4}-\d{2}-\d{2}\s+(\d{2}:\d{2}:\d{2})\s+(TRACE|DEBUG|INFO|WARN|ERROR|FATAL)\s*(?:\[[^\]]*\]\s*)?",RegexOptions.Compiled);
    public void Add(string text){foreach(var line in text.Split(new char[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)){var m=prefix.Match(line);string level=m.Success?m.Groups[2].Value:"INFO",message=m.Success?line.Substring(m.Length):line;if(level=="WARN"||message.IndexOf("failed to search process",StringComparison.OrdinalIgnoreCase)>=0)level="UYARI";if(level=="ERROR"||level=="FATAL")level="HATA";rows.Enqueue(new LogEntry{Id=++next,Time=m.Success?m.Groups[1].Value:DateTime.Now.ToString("HH:mm:ss"),Level=level,Detail=line.Length>4096?line.Substring(0,4096):line,Message=message.Length>4096?message.Substring(0,4096):message});while(rows.Count>1500)rows.Dequeue();Revision++;}}
    public List<LogEntry> Snapshot(){return new List<LogEntry>(rows);}
    public static List<LogEntry> Filter(IEnumerable<LogEntry> source,string search,string level){var list=new List<LogEntry>();foreach(var row in source)if((String.IsNullOrEmpty(level)||row.Level==level)&&(String.IsNullOrWhiteSpace(search)||row.Message.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||row.Time.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||row.Level.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0))list.Add(row);return list;}
    public static string Text(IEnumerable<LogEntry> source){var text=new StringBuilder();foreach(var row in source)text.AppendLine(row.Time+"  "+row.Level+"  "+row.Message);return text.ToString();}
    public string AllText(){return Text(rows);}
}
