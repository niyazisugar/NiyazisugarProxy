using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
static class RelayTests {
    sealed class Fake : IDisposable {
        TcpListener listener=new TcpListener(IPAddress.Loopback,0);List<TcpClient> clients=new List<TcpClient>();bool closed;readonly byte marker;
        public int Port {get{return ((IPEndPoint)listener.LocalEndpoint).Port;}}
        public Fake(byte value){marker=value;listener.Start();Task.Run((Action)Accept);}
        void Accept(){while(!closed){try{var client=listener.AcceptTcpClient();lock(clients)clients.Add(client);Task.Run(delegate{try{client.ReceiveTimeout=client.SendTimeout=3000;var stream=client.GetStream();byte[] hello=Read(stream,3);if(hello[0]!=5||hello[2]!=2)throw new Exception("Expected auth");stream.Write(new byte[]{5,2},0,2);var auth=Read(stream,2);var user=Read(stream,auth[1]);int n=stream.ReadByte();var password=Read(stream,n);if(Encoding.UTF8.GetString(user)!="test-user"||Encoding.UTF8.GetString(password)!="test-password")throw new Exception("Authentication mismatch");stream.Write(new byte[]{1,0},0,2);Read(stream,10);stream.Write(new byte[]{5,0,0,1,127,0,0,1,0,80},0,10);while(stream.ReadByte()>=0)stream.WriteByte(marker);}catch(Exception){}finally{client.Close();}});}catch(Exception){if(closed)return;}}}
        public void Dispose(){closed=true;listener.Stop();lock(clients)foreach(var client in clients)client.Close();}
    }
    static byte[] Read(Stream stream,int count){var bytes=new byte[count];int offset=0;while(offset<count){int n=stream.Read(bytes,offset,count-offset);if(n==0)throw new EndOfStreamException();offset+=n;}return bytes;}
    static TcpClient Connect(int port){var socket=new TcpClient();socket.Connect(IPAddress.Loopback,port);socket.ReceiveTimeout=socket.SendTimeout=3000;var stream=socket.GetStream();stream.Write(new byte[]{5,1,0},0,3);var methods=Read(stream,2);if(methods[1]!=0)throw new Exception("Relay methods");stream.Write(new byte[]{5,1,0,1,192,0,2,99,0,80},0,10);if(Read(stream,10)[1]!=0)throw new Exception("Relay connect");return socket;}
    static void Echo(TcpClient socket,byte marker){var stream=socket.GetStream();stream.WriteByte(42);if(stream.ReadByte()!=marker)throw new Exception("Wrong upstream proxy");}
    public static void Run(){using(var first=new Fake(65))using(var second=new Fake(66))using(var a=new SlotRelay())using(var b=new SlotRelay()){
        a.SetProxy(new ProxyItem{Host="127.0.0.1",Port=first.Port,User="test-user",Password="test-password"});
        using(var active=Connect(a.Port)){Echo(active,65);b.SetProxy(new ProxyItem{Host="127.0.0.1",Port=second.Port,User="test-user",Password="test-password"});using(var later=Connect(b.Port)){Echo(later,66);Echo(active,65);b.Stop();Echo(active,65);}a.SetProxy(new ProxyItem{Host="127.0.0.1",Port=second.Port,User="test-user",Password="test-password"});Echo(active,65);using(var changed=Connect(a.Port))Echo(changed,66);}
    }Console.WriteLine("Authenticated SOCKS forwarding and independent-slot connection preservation passed.");}
}
