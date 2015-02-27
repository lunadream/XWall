<%@ WebHandler Language="C#" Class="Transmit" %>

using System;
using System.Web;

public class Transmit : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        var req = context.Request;
        var res = context.Response;
        var query = req.QueryString;

        if (query["file"] != null) {
            if (query["type"] != null) {
                res.ContentType = query["type"];
            }
            if (query["nocache"] != null) {
                res.Cache.SetCacheability(HttpCacheability.NoCache);
                res.Cache.SetNoStore();
            }
            res.BinaryWrite(System.IO.File.ReadAllBytes(context.Server.MapPath("~/") + @"..\resources\" + query["file"]));
        }
        else if (query["config"] != null) {
            string text;
            var config = System.IO.File.ReadAllText(context.Server.MapPath("~/") + @"..\configs\" + query["config"]);
            if (query["callback"] != null) {
                res.ContentType = "text/javascript";
                text = query["callback"] + "(" + config + ");";
            }
            else {
                if (query["type"] != null) {
                    res.ContentType = query["type"];
                }
                text = config;
            }
            res.Cache.SetCacheability(HttpCacheability.NoCache);
            res.Cache.SetNoStore();
            res.Write(text);
        }
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}