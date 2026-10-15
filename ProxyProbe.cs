using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;
sealed class ProxyProbeResult {
    public long? TcpMilliseconds;public string IP,Country,Provider,Error;public bool LookupAttempted;public DateTime CheckedAt=DateTime.Now;
}
// An explicit SOCKS5/TLS request. Never falls back to a direct geolocation request.
static class ProxyProbe {
    static byte[] Read(Stream stream,int count){var bytes=new byte[count];int offset=0;while(offset<count){int n=stream.Read(bytes,offset,count-offset);if(n==0)throw new EndOfStreamException();offset+=n;}return bytes;}
    static void Write(Stream stream,byte[] bytes){stream.Write(bytes,0,bytes.Length);}
    internal static void OpenSocks(Stream stream,ProxyItem proxy,string host,int port){
        bool auth=!String.IsNullOrEmpty(proxy.User);Write(stream,auth?new byte[]{5,1,2}:new byte[]{5,1,0});var method=Read(stream,2);if(method[0]!=5||method[1]!=(auth?2:0))throw new IOException("SOCKS5 kimlik doğrulama yöntemi reddedildi.");
        if(auth){byte[] user=Encoding.UTF8.GetBytes(proxy.User),password=Encoding.UTF8.GetBytes(proxy.Password??"");if(user.Length>255||password.Length>255)throw new IOException("Proxy kimlik bilgileri çok uzun.");using(var packet=new MemoryStream()){packet.WriteByte(1);packet.WriteByte((byte)user.Length);packet.Write(user,0,user.Length);packet.WriteByte((byte)password.Length);packet.Write(password,0,password.Length);Write(stream,packet.ToArray());}var accepted=Read(stream,2);if(accepted[0]!=1||accepted[1]!=0)throw new IOException("Proxy kullanıcı adı veya şifresi reddedildi.");}
        byte[] domain=Encoding.ASCII.GetBytes(host);using(var request=new MemoryStream()){request.Write(new byte[]{5,1,0,3,(byte)domain.Length},0,5);request.Write(domain,0,domain.Length);request.WriteByte((byte)(port>>8));request.WriteByte((byte)port);Write(stream,request.ToArray());}
        var reply=Read(stream,4);if(reply[0]!=5||reply[1]!=0||reply[2]!=0)throw new IOException("Proxy test hedefine bağlantıyı reddetti.");int length=reply[3]==1?4:reply[3]==4?16:reply[3]==3?Read(stream,1)[0]:-1;if(length<0)throw new IOException("Geçersiz SOCKS5 yanıtı.");Read(stream,length+2);
    }
    public static ProxyProbeResult Measure(ProxyItem proxy,bool lookup){
        var result=new ProxyProbeResult{LookupAttempted=lookup};try{using(var socket=new TcpClient())using(var deadline=new Timer(delegate{try{socket.Close();}catch{}},null,20000,Timeout.Infinite)){
            var watch=Stopwatch.StartNew();var connecting=socket.ConnectAsync(proxy.Host,proxy.Port);if(!connecting.Wait(5000))throw new IOException("Proxy TCP bağlantısı zaman aşımına uğradı.");connecting.GetAwaiter().GetResult();result.TcpMilliseconds=Math.Max(1,watch.ElapsedMilliseconds);if(!lookup)return result;
            socket.ReceiveTimeout=socket.SendTimeout=5000;var stream=socket.GetStream();OpenSocks(stream,proxy,"ipwho.is",443);
            using(var tls=new SslStream(stream,false)){tls.ReadTimeout=tls.WriteTimeout=5000;tls.AuthenticateAsClient("ipwho.is",null,SslProtocols.Tls12,true);Write(tls,Encoding.ASCII.GetBytes("GET /?fields=success,ip,country,country_code,connection.isp HTTP/1.1\r\nHost: ipwho.is\r\nUser-Agent: NiyazisugarProxy\r\nAccept: application/json\r\nAccept-Encoding: identity\r\nConnection: close\r\n\r\n"));using(var response=new MemoryStream()){var buffer=new byte[4096];int n;while((n=tls.Read(buffer,0,buffer.Length))>0){if(response.Length+n>65536)throw new IOException("Test yanıtı çok büyük.");response.Write(buffer,0,n);}ParseResponse(response.ToArray(),result);}}
        }}catch(Exception){result.Error=result.TcpMilliseconds.HasValue?"Çıkış IP/ülke doğrulanamadı (SOCKS5, TLS veya servis hatası).":"Proxy TCP bağlantısı kurulamadı (5 sn zaman aşımı veya ağ hatası).";}return result;
    }
    internal static void ParseResponse(byte[] bytes,ProxyProbeResult result){
        var wire=Encoding.GetEncoding(28591);string response=wire.GetString(bytes);int split=response.IndexOf("\r\n\r\n",StringComparison.Ordinal);if(split<0)throw new IOException("HTTP başlığı eksik.");string header=response.Substring(0,split),body=response.Substring(split+4);string[] status=header.Split(new char[]{' '},3);if(status.Length<2||status[1]!="200")throw new IOException("IP servisi isteği başarısız.");
        if(header.IndexOf("Transfer-Encoding: chunked",StringComparison.OrdinalIgnoreCase)>=0){var decoded=new StringBuilder();int pos=0;while(true){int end=body.IndexOf("\r\n",pos,StringComparison.Ordinal);if(end<0)throw new IOException("Eksik HTTP parçası.");string size=body.Substring(pos,end-pos).Split(';')[0];int count;if(!Int32.TryParse(size,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out count)||count<0||count>65536)throw new IOException("Geçersiz HTTP parçası.");pos=end+2;if(count==0)break;if(pos+count+2>body.Length||body.Substring(pos+count,2)!="\r\n")throw new IOException("Eksik HTTP içeriği.");decoded.Append(body.Substring(pos,count));pos+=count+2;}body=decoded.ToString();}
        ParseJson(Encoding.UTF8.GetString(wire.GetBytes(body)),result);
    }
    internal static void ParseJson(string body,ProxyProbeResult result){
        var data=new JavaScriptSerializer{MaxJsonLength=65536}.Deserialize<Dictionary<string,object>>(body);object success,ip,country;if(data==null||!data.TryGetValue("success",out success)||!(success is bool)||!(bool)success||!data.TryGetValue("ip",out ip)||!data.TryGetValue("country",out country))throw new IOException("IP servisi geçerli sonuç vermedi.");IPAddress address;if(!(ip is string)||!IPAddress.TryParse((string)ip,out address)||!(country is string)||String.IsNullOrWhiteSpace((string)country))throw new IOException("IP/ülke bilgisi geçersiz.");
        string name=(string)country;object code;if(data.TryGetValue("country_code",out code)){if((string)code=="TR")name="Türkiye";else if((string)code=="RO")name="Romanya";}
        string provider="—";object connection,value;if(data.TryGetValue("connection",out connection)&&connection is Dictionary<string,object>&&((Dictionary<string,object>)connection).TryGetValue("isp",out value)&&value is string)provider=(string)value;
        result.IP=address.ToString();result.Country=name;result.Provider=provider;
    }
}
