using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// One local TCP SOCKS bridge per slot. Existing sessions retain their upstream snapshot.
sealed class SlotRelay : IDisposable {
    readonly TcpListener listener;readonly object gate=new object();readonly List<TcpClient> connections=new List<TcpClient>();
    ProxyItem proxy;bool disposed;
    public int Port {get{return ((IPEndPoint)listener.LocalEndpoint).Port;}}
    public SlotRelay(){listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Task.Run((Action)Accept);}
    public void SetProxy(ProxyItem value){lock(gate)proxy=value==null?null:new ProxyItem{Host=value.Host,Port=value.Port,User=value.User,Password=value.Password};}
    public void Stop(){lock(gate){proxy=null;foreach(var socket in connections)socket.Close();connections.Clear();}}
    void Accept(){while(!disposed){try{var client=listener.AcceptTcpClient();Task.Run(delegate{Serve(client);});}catch(SocketException){if(disposed)return;}catch(ObjectDisposedException){return;}}}
    static byte[] Read(Stream stream,int count){var bytes=new byte[count];int offset=0;while(offset<count){int n=stream.Read(bytes,offset,count-offset);if(n==0)throw new EndOfStreamException();offset+=n;}return bytes;}
    static void Write(Stream stream,byte[] bytes){stream.Write(bytes,0,bytes.Length);}
    void Serve(TcpClient client){TcpClient upstream=null;try{
        ProxyItem snapshot;lock(gate){snapshot=proxy;if(disposed||snapshot==null){client.Close();return;}connections.Add(client);}
        client.ReceiveTimeout=client.SendTimeout=15000;Stream input=client.GetStream();var hello=Read(input,2);if(hello[0]!=5)throw new IOException("SOCKS version");byte[] methods=Read(input,hello[1]);if(Array.IndexOf(methods,(byte)0)<0){Write(input,new byte[]{5,255});return;}Write(input,new byte[]{5,0});
        var request=Read(input,4);if(request[0]!=5||request[1]!=1||request[2]!=0){Write(input,new byte[]{5,7,0,1,0,0,0,0,0,0});return;}
        var address=new MemoryStream();address.Write(request,0,4);
        int length;if(request[3]==1)length=4;else if(request[3]==4)length=16;else if(request[3]==3){byte n=Read(input,1)[0];address.WriteByte(n);length=n;}else throw new IOException("SOCKS address");byte[] destination=Read(input,length+2);address.Write(destination,0,destination.Length);
        upstream=new TcpClient();lock(gate){if(disposed||proxy==null){upstream.Close();return;}connections.Add(upstream);}
        Task connecting=upstream.ConnectAsync(snapshot.Host,snapshot.Port);if(!connecting.Wait(15000))throw new IOException("Proxy connect timeout");connecting.GetAwaiter().GetResult();upstream.ReceiveTimeout=upstream.SendTimeout=15000;Stream output=upstream.GetStream();
        bool auth=!String.IsNullOrEmpty(snapshot.User);Write(output,auth?new byte[]{5,1,2}:new byte[]{5,1,0});var choice=Read(output,2);if(choice[0]!=5||choice[1]!=(auth?2:0))throw new IOException("Proxy authentication method");
        if(auth){byte[] user=Encoding.UTF8.GetBytes(snapshot.User),password=Encoding.UTF8.GetBytes(snapshot.Password??"");if(user.Length>255||password.Length>255)throw new IOException("Proxy credentials length");var packet=new MemoryStream();packet.WriteByte(1);packet.WriteByte((byte)user.Length);packet.Write(user,0,user.Length);packet.WriteByte((byte)password.Length);packet.Write(password,0,password.Length);Write(output,packet.ToArray());var result=Read(output,2);if(result[0]!=1||result[1]!=0)throw new IOException("Proxy authentication rejected");}
        Write(output,address.ToArray());var reply=Read(output,4);var replyBytes=new MemoryStream();replyBytes.Write(reply,0,4);if(reply[3]==1)length=4;else if(reply[3]==4)length=16;else if(reply[3]==3){byte n=Read(output,1)[0];replyBytes.WriteByte(n);length=n;}else throw new IOException("Proxy reply address");var tail=Read(output,length+2);replyBytes.Write(tail,0,tail.Length);Write(input,replyBytes.ToArray());if(reply[1]!=0)return;
        client.ReceiveTimeout=client.SendTimeout=upstream.ReceiveTimeout=upstream.SendTimeout=0;
        Task forward=Task.Run(delegate{try{input.CopyTo(output);upstream.Client.Shutdown(SocketShutdown.Send);}catch(Exception){upstream.Close();client.Close();}});
        try{output.CopyTo(input);client.Client.Shutdown(SocketShutdown.Send);}catch(Exception){}finally{upstream.Close();client.Close();}
        try{forward.Wait(5000);}catch(Exception){}
    }catch(Exception){client.Close();if(upstream!=null)upstream.Close();}finally{lock(gate){connections.Remove(client);if(upstream!=null)connections.Remove(upstream);}client.Close();if(upstream!=null)upstream.Close();}}
    public void Dispose(){disposed=true;Stop();listener.Stop();}
}
