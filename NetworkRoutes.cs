using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

internal static class NetworkRoutes {
    [StructLayout(LayoutKind.Sequential)]struct ForwardRow { public uint destination,mask,policy,nextHop,index,type,protocol,age,nextHopAS,metric1,metric2,metric3,metric4,metric5; }
    internal sealed class Route { public uint Destination,Mask,NextHop,Index,Metric; }
    internal sealed class Selection { public string alias{get;set;} public uint index{get;set;} public uint metric{get;set;} public string gateway{get;set;} }
    [DllImport("iphlpapi.dll")]static extern uint GetIpForwardTable(IntPtr table,ref uint bytes,bool sorted);
    [DllImport("iphlpapi.dll")]static extern uint ConvertInterfaceIndexToLuid(uint index,out ulong luid);
    [DllImport("iphlpapi.dll",CharSet=CharSet.Unicode)]static extern uint ConvertInterfaceLuidToAlias(ref ulong luid,StringBuilder alias,UIntPtr length);
    internal static Route Choose(IEnumerable<Route> routes,HashSet<uint> indexes) {
        // Same selection as working Get-NetRoute: gateway-bearing IPv4 /0,
        // ordered by route + interface metric. Metric1 already contains that sum.
        return routes.Where(r=>r.Destination==0&&r.Mask==0&&r.NextHop!=0&&indexes.Contains(r.Index)).OrderBy(r=>r.Metric).FirstOrDefault();
    }
    public static Selection Select() {
        var adapters=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.Supports(NetworkInterfaceComponent.IPv4)&&!n.Name.StartsWith("NeviSOCKS",StringComparison.OrdinalIgnoreCase)&&!n.Name.StartsWith("NiyazisugarProxy",StringComparison.OrdinalIgnoreCase)).ToArray();
        var indexes=new HashSet<uint>(adapters.Select(n=>(uint)n.GetIPProperties().GetIPv4Properties().Index));
        var rows=new List<Route>();uint size=0;uint code=GetIpForwardTable(IntPtr.Zero,ref size,false);
        if(code!=122||size<4)throw new Exception("Varsayilan ag rotasi okunamadi.");
        for(int attempt=0;attempt<3;attempt++) {
            IntPtr buffer=Marshal.AllocHGlobal((int)size);
            try {
                code=GetIpForwardTable(buffer,ref size,false);if(code==122)continue;if(code!=0)throw new Exception("Windows rota tablosu okunamadi.");
                int count=Marshal.ReadInt32(buffer);int stride=Marshal.SizeOf(typeof(ForwardRow));if(count<0||4L+(long)count*stride>size)throw new Exception("Rota tablosu boyutu gecersiz.");
                for(int i=0;i<count;i++){var row=(ForwardRow)Marshal.PtrToStructure(IntPtr.Add(buffer,4+i*stride),typeof(ForwardRow));rows.Add(new Route{Destination=row.destination,Mask=row.mask,NextHop=row.nextHop,Index=row.index,Metric=row.metric1});}break;
            }finally{Marshal.FreeHGlobal(buffer);}
        }
        var chosen=Choose(rows,indexes);if(chosen==null)throw new Exception("Gecitli fiziksel IPv4 varsayilan rotasi bulunamadi. Tunel baslatilmadi.");
        ulong luid;var name=new StringBuilder(257);if(ConvertInterfaceIndexToLuid(chosen.Index,out luid)!=0||ConvertInterfaceLuidToAlias(ref luid,name,(UIntPtr)257)!=0||name.Length==0)throw new Exception("Windows ag arayuzu adi okunamadi.");
        return new Selection{alias=name.ToString(),index=chosen.Index,metric=chosen.Metric,gateway=new IPAddress(BitConverter.GetBytes(chosen.NextHop)).ToString()};
    }
}
