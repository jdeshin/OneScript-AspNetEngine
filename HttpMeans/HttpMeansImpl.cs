/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/

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
            return HttpContext.Current.Server.MapPath(virtualPath); ;
        }

        [ScriptConstructor(Name = "Без параметров")]
        public static IRuntimeContextInstance Constructor()
        {
            return new HttpMeansImpl();
        }

        [ContextProperty("Контекст", "Context")]
        public HTTPServiceContextImpl Context
        {
            get
            {
                return new HTTPServiceContextImpl(System.Web.HttpContext.Current);
            }
        }
    }
}
