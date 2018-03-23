// Copyright (c) Yury Deshin 2018
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using System.Web;

using ScriptEngine.HostedScript.Library;
using ScriptEngine.HostedScript.Library.Binary;

/// <summary>
/// 
/// </summary>

namespace OneScript.HTTPService
{
    [ContextClass("СредстваHTTP", "HTTPMeans")]
    public class HttpMeansImpl : AutoContext<HttpMeansImpl>
    {

        public HttpMeansImpl()
        {
        }

        [ContextMethod("ПолучитьФизическийПутьИзВиртуального", "MapPath")]
        public string MapPath(string virtualPath)
        {
            return HttpContext.Current.Server.MapPath(virtualPath);
        }

        [ScriptConstructor(Name = "Без параметров")]
        public static IRuntimeContextInstance Constructor()
        {
            return new HttpMeansImpl();
        }

        [ContextProperty("ТекущийКонтекст", "CurrentContext")]
        public HTTPServiceContextImpl CurrentContext
        {
            get
            {
                return new HTTPServiceContextImpl(System.Web.HttpContext.Current);
            }
        }
    }
}
