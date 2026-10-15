using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
static class ProbeTests {
    static void Assert(bool valid){if(!valid)throw new Exception("Proxy probe assertion");}
    static void Reject(Action action){try{action();}catch{return;}throw new Exception("Probe should reject invalid response");}
    static byte[] Read(Stream stream,int size){var bytes=new byte[size];int offset=0;while(offset<size){int n=stream.Read(bytes,offset,size-offset);if(n<=0)throw new EndOfStreamException();offset+=n;}return bytes;}
    static void Handshake(bool auth,bool reject){
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
        var server=Task.Run(delegate{using(var socket=listener.AcceptTcpClient()){socket.ReceiveTimeout=socket.SendTimeout=2000;var stream=socket.GetStream();var hello=Read(stream,3);Assert(hello[0]==5&&hello[2]==(auth?2:0));stream.Write(new byte[]{5,(byte)(auth?2:0)},0,2);
            if(auth){var h=Read(stream,2);Assert(h[0]==1&&Encoding.UTF8.GetString(Read(stream,h[1]))=="user");int n=stream.ReadByte();Assert(Encoding.UTF8.GetString(Read(stream,n))=="pass");stream.Write(new byte[]{1,(byte)(reject?1:0)},0,2);if(reject)return;}
            var header=Read(stream,5);Assert(header[0]==5&&header[1]==1&&header[3]==3);Assert(Encoding.ASCII.GetString(Read(stream,header[4]))=="ipwho.is");var destination=Read(stream,2);Assert(destination[0]==1&&destination[1]==187);stream.Write(new byte[]{5,0,0,1,127,0,0,1,1,187},0,10);
        }});
        try{using(var client=new TcpClient("127.0.0.1",port)){client.ReceiveTimeout=client.SendTimeout=2000;Action request=delegate{ProxyProbe.OpenSocks(client.GetStream(),new ProxyItem{User=auth?"user":null,Password="pass"},"ipwho.is",443);};if(reject)Reject(request);else request();}if(!server.Wait(3000))throw new Exception("Probe fake timeout");server.GetAwaiter().GetResult();}finally{listener.Stop();}
    }
    public static void Run(){
        var result=new ProxyProbeResult();string body="{\"success\":true,\"ip\":\"203.0.113.7\",\"country\":\"Türkiye\",\"country_code\":\"TR\",\"connection\":{\"isp\":\"Example ISP\"}}";
        ProxyProbe.ParseJson(body,result);Assert(result.IP=="203.0.113.7"&&result.Country=="Türkiye"&&result.Provider=="Example ISP");
        byte[] json=Encoding.UTF8.GetBytes(body);using(var response=new MemoryStream()){var header=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n"+json.Length.ToString("X")+"\r\n");response.Write(header,0,header.Length);response.Write(json,0,json.Length);var tail=Encoding.ASCII.GetBytes("\r\n0\r\n\r\n");response.Write(tail,0,tail.Length);ProxyProbe.ParseResponse(response.ToArray(),new ProxyProbeResult());}
        Reject(delegate{ProxyProbe.ParseJson("{\"success\":false,\"ip\":\"203.0.113.7\",\"country\":\"Romania\"}",new ProxyProbeResult());});Reject(delegate{ProxyProbe.ParseJson("{\"success\":true,\"ip\":\"invalid\",\"country\":\"Romania\"}",new ProxyProbeResult());});Reject(delegate{ProxyProbe.ParseResponse(Encoding.ASCII.GetBytes("HTTP/1.1 429 Rate Limited\r\n\r\n{}"),new ProxyProbeResult());});Reject(delegate{ProxyProbe.ParseResponse(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\nFFFF\r\n{}"),new ProxyProbeResult());});
        Handshake(false,false);Handshake(true,false);Handshake(true,true);
        var reservation=new TcpListener(IPAddress.Loopback,0);reservation.Start();int unused=((IPEndPoint)reservation.LocalEndpoint).Port;reservation.Stop();var failure=ProxyProbe.Measure(new ProxyItem{Host="127.0.0.1",Port=unused},true);Assert(failure.TcpMilliseconds==null&&failure.IP==null&&failure.Error!=null);
        Console.WriteLine("SOCKS5 probe authentication, destination, rejection, UTF8 chunked parsing and failed validation tests passed.");
    }
}
