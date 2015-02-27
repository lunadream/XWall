<%@ WebHandler Language="C#" Class="Rule" %>

using System;
using System.Web;
using System.IO;

public class Rule : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        var req = context.Request;
        var res = context.Response;
        var query = req.QueryString;

        var root = context.Server.MapPath("~/") + @"..\configs\";
        string file = "";
        string rule = "";
        
        if (query["new"] != null) {
            file = root + "new-rule-cmd";
            rule = query["new"];
        }
        else if (query["del"] != null) {
            file = root + "del-rule-cmd";
            rule = query["del"];
        }

        var ruleCommandWatcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file));
        File.WriteAllText(file, rule);
        var result = (!ruleCommandWatcher.WaitForChanged(WatcherChangeTypes.Deleted, 10000).TimedOut).ToString().ToLower();

        string text;
        if (query["callback"] != null) {
            res.ContentType = "text/javascript";
            text = query["callback"] + "(" + result + ");";
        }
        else {
            if (query["type"] != null) {
                res.ContentType = query["type"];
            }
            text = result.ToString();
        }
        res.Write(text);
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}