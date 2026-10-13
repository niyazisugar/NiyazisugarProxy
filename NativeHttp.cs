using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
class NativeHttpResponse {public int Status;public string Body;}
static class NativeHttp {
    public static string Failure(int hresult) {
        int code=hresult & 0xffff;
        string reason;
        switch(code) {
            case 12002: reason="Bağlantı zaman aşımına uğradı.";break;
            case 12007: reason="Servis alan adı DNS üzerinden çözülemedi.";break;
            case 12029: reason="Servise TCP bağlantısı kurulamadı.";break;
            case 12030: reason="Bağlantı gönderim sırasında kesildi.";break;
            case 12175: reason="TLS/sertifika doğrulaması başarısız oldu.";break;
            case 12037: reason="Sertifika tarihi veya bilgisayar saati geçersiz.";break;
            case 12038: reason="Sertifikanın alan adı uyuşmuyor.";break;
            case 12045: reason="Sertifika otoritesine güvenilmiyor.";break;
            default: reason="Windows HTTPS gönderimi başarısız.";break;
        }
        return reason+" WinHTTP: "+code+"; HRESULT: 0x"+unchecked((uint)hresult).ToString("X8");
    }
    public static NativeHttpResponse Send(string url,string method,string key,string payload) {
        object com=null;
        try {
            Uri uri=new Uri(url);
            if(uri.Scheme!="https" || !String.IsNullOrEmpty(uri.UserInfo))throw new Exception("Gönderme adresi HTTPS olmalı.");
            if(key!=null && (key.Contains("\r")||key.Contains("\n")))throw new Exception("Bağlantı kodunda satır sonu olamaz.");
            com=Activator.CreateInstance(Type.GetTypeFromProgID("WinHttp.WinHttpRequest.5.1",true));
            dynamic request=com;
            request.SetTimeouts(5000,10000,20000,20000);
            request.Open(method,url,false);
            request.Option[6]=false; // Do not forward Authorization to redirects.
            request.Option[9]=0x800; // TLS 1.2; no registry changes.
            request.Option[4]=0; // No certificate errors are ignored.
            request.SetRequestHeader("User-Agent","NiyazisugarProxy/"+Updates.Current.ToString(3));
            if(key!=null)request.SetRequestHeader("Authorization","Bearer "+key);
            if(payload!=null) {
                request.SetRequestHeader("Content-Type","application/json; charset=utf-8");
                request.Send(Encoding.UTF8.GetBytes(payload));
            } else request.Send();
            string body=(string)request.ResponseText;
            if(body.Length>64000)throw new Exception("Servis cevabı boyut sınırını aşıyor.");
            return new NativeHttpResponse{Status=(int)request.Status,Body=body};
        }catch(COMException ex){throw new Exception(Failure(ex.ErrorCode));}
        finally {if(com!=null&&Marshal.IsComObject(com))Marshal.FinalReleaseComObject(com);}
    }
}
