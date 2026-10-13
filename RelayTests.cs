using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;
static class RelayTests {
    sealed class Fake : IDisposable {
        TcpListener listener=new TcpListener(IPAddress.Loopback,0);List<TcpClient> clients=new List<TcpClient>();bool closed;readonly byte marker;
        public int Port {get{return ((IPEndPoint)listener.LocalEndpoint).Port;}}
        public Fake(byte value){marker=value;listener.Start();Task.Run((Action)Accept);}
        void Accept(){while(!closed){try{var client=listener.AcceptTcpClient();lock(clients)clients.Add(client);Task.Run(delegate{try{client.ReceiveTimeout=client.SendTimeout=3000;var stream=client.GetStream();byte[] hello=Read(stream,3);if(hello[0]!=5||hello[2]!=2)throw new Exception("Expected auth");stream.Write(new byte[]{5,2},0,2);var auth=Read(stream,2);var user=Read(stream,auth[1]);int n=stream.ReadByte();var password=Read(stream,n);if(Encoding.UTF8.GetString(user)!="test-user"||Encoding.UTF8.GetString(password)!="test-password")throw new Exception("Authentication mismatch");stream.Write(new byte[]{1,0},0,2);Read(stream,10);stream.Write(new byte[]{5,0,0,1,127,0,0,1,0,80},0,10);client.ReceiveTimeout=0;while(stream.ReadByte()>=0)stream.WriteByte(marker);}catch(Exception){}finally{client.Close();}});}catch(Exception){if(closed)return;}}}
        public void Dispose(){closed=true;listener.Stop();lock(clients)foreach(var client in clients)client.Close();}
    }
    static byte[] Read(Stream stream,int count){var bytes=new byte[count];int offset=0;while(offset<count){int n=stream.Read(bytes,offset,count-offset);if(n==0)throw new EndOfStreamException();offset+=n;}return bytes;}
    static TcpClient Connect(int port,byte destination=99){var socket=new TcpClient();socket.Connect(IPAddress.Loopback,port);socket.ReceiveTimeout=socket.SendTimeout=3000;var stream=socket.GetStream();stream.Write(new byte[]{5,1,0},0,3);var methods=Read(stream,2);if(methods[1]!=0)throw new Exception("Relay methods");stream.Write(new byte[]{5,1,0,1,192,0,2,destination,0,80},0,10);if(Read(stream,10)[1]!=0)throw new Exception("Relay connect");return socket;}
    static void Echo(TcpClient socket,byte marker){var stream=socket.GetStream();stream.WriteByte(42);if(stream.ReadByte()!=marker)throw new Exception("Wrong upstream proxy");}
    public static void VerifyLiveEngine(string binary,string iface){
        using(var first=new Fake(65))using(var second=new Fake(66))using(var a=new SlotRelay())using(var b=new SlotRelay())using(var live=new LiveRouting()){
            a.SetProxy(new ProxyItem{Host="127.0.0.1",Port=first.Port,User="test-user",Password="test-password"});b.SetProxy(new ProxyItem{Host="127.0.0.1",Port=second.Port,User="test-user",Password="test-password"});
            var slotOne=new ClientRoute{Proxy=new ProxyItem{Host="127.0.0.1",Port=first.Port},Patterns=new string[]{"^no-such-game-one$"},IP="192.0.2.99",Tag="game-proxy-1"};
            live.Update(new ClientRoute[]{slotOne,null});var config=live.Config(new int[]{a.Port,b.Port},iface);
            var reservation=new TcpListener(IPAddress.Loopback,0);reservation.Start();int port=((IPEndPoint)reservation.LocalEndpoint).Port;reservation.Stop();
            config["inbounds"]=new object[]{RoutingConfig.D("type","socks","tag","test-in","listen","127.0.0.1","listen_port",port)};
            string path=Path.GetTempFileName();File.WriteAllText(path,new JavaScriptSerializer().Serialize(config));
            try{using(var process=new Process{StartInfo=new ProcessStartInfo(binary,"run -c "+Updates.Quote(path)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true}}){
                var ready=new ManualResetEvent(false);var output=new System.Text.StringBuilder();DataReceivedEventHandler log=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null){lock(output)output.AppendLine(e.Data);if(e.Data.Contains("sing-box started"))ready.Set();}};process.ErrorDataReceived+=log;process.OutputDataReceived+=log;
                process.Start();process.BeginErrorReadLine();process.BeginOutputReadLine();try{
                    if(!ready.WaitOne(10000))throw new Exception("Live engine startup failed: "+output);
                    using(var active=Connect(port)){Echo(active,65);
                        live.Update(new ClientRoute[]{slotOne,new ClientRoute{Proxy=new ProxyItem{Host="127.0.0.1",Port=second.Port},Patterns=new string[]{"^no-such-game-two$"},IP="192.0.2.100",Tag="game-proxy-2"}});
                        bool routed=false;var timer=Stopwatch.StartNew();while(timer.ElapsedMilliseconds<15000){try{using(var later=Connect(port,100))Echo(later,66);routed=true;break;}catch(Exception){Thread.Sleep(250);}}
                        if(!routed)throw new Exception("Runtime second-slot rule reload failed: "+output);Echo(active,65);
                        b.Stop();live.Update(new ClientRoute[]{slotOne,null});Echo(active,65);
                        Console.WriteLine("Real sing-box runtime rule reload preserved the first TCP session while adding/stopping slot two.");
                    }
                }finally{if(!process.HasExited){process.Kill();process.WaitForExit(5000);}ready.Dispose();}
            }}finally{File.Delete(path);}
        }
    }
    public static void Run(){using(var first=new Fake(65))using(var second=new Fake(66))using(var a=new SlotRelay())using(var b=new SlotRelay()){
        a.SetProxy(new ProxyItem{Host="127.0.0.1",Port=first.Port,User="test-user",Password="test-password"});
        using(var active=Connect(a.Port)){Echo(active,65);b.SetProxy(new ProxyItem{Host="127.0.0.1",Port=second.Port,User="test-user",Password="test-password"});using(var later=Connect(b.Port)){Echo(later,66);Echo(active,65);b.Stop();Echo(active,65);}a.SetProxy(new ProxyItem{Host="127.0.0.1",Port=second.Port,User="test-user",Password="test-password"});Echo(active,65);using(var changed=Connect(a.Port))Echo(changed,66);}
    }Console.WriteLine("Authenticated SOCKS forwarding and independent-slot connection preservation passed.");}
}
