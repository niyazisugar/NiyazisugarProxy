using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
class Tests {
    static void Assert(bool ok) {if(!ok)throw new Exception("Assertion failed");}
    static string Release(string version,string url,string digest,bool preview) {
        return new JavaScriptSerializer().Serialize(new Dictionary<string,object> {
            {"tag_name",version},{"draft",false},{"prerelease",preview},
            {"assets",new object[]{new Dictionary<string,object>{{"name","NiyazisugarProxy.exe"},{"browser_download_url",url},{"digest",digest}}}}
        });
    }
    static void Reject(Action action) {try {action();}catch(Exception){return;}throw new Exception("Expected rejection");}
    public static int Main() {
        try {
            var current=new Version(1,0,0,0);string url="https://github.com/niyazisugar/NiyazisugarProxy/releases/download/v1.0.1/NiyazisugarProxy.exe";
            string digest="sha256:"+new string('a',64);
            Assert(Updates.Parse(Release("v1.0.0",url,digest,false),current)==null);
            Assert(Updates.Parse(Release("v0.9.0",url,digest,false),current)==null);
            Assert(Updates.Parse(Release("v1.0.1",url,digest,true),current)==null);
            Assert(Updates.Parse(Release("v1.0.1",url,digest,false),current).Version==new Version(1,0,1));
            Reject(delegate {Updates.Parse(Release("v1.0.1","https://example.com/app.exe",digest,false),current);});
            Reject(delegate {Updates.Parse(Release("v1.0.1",url,null,false),current);});
            Reject(delegate {Updates.Parse(Release("v1.0.1",url,"sha256:bad",false),current);});
            string file=Path.GetTempFileName();try {File.WriteAllText(file,"abc",new System.Text.UTF8Encoding(false));Assert(Updates.Hash(file)=="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");}finally{File.Delete(file);}
            Console.WriteLine("Update parsing, rollback version rejection, trusted URL and SHA256 tests passed.");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
