﻿// Copyright (c) Yury Deshin 2018
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

        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            // Создаем объект из модуля менеджера
            return new HTTPMeansDataProcessorObjectModule();
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
        public HTTPContextImpl CurrentContext { get; } = new HTTPContextImpl();

        [ContextMethod("ПолучитьТекущийКонтекст", "GetCurrentContext")]
        public HTTPContextImpl GetCurrentHTTPContext()
        {
            return new HTTPContextImpl();
        }
    }

    // Пустой класс заглушка модуля объекта
    [ContextClass("СредстваHTTPМодульОбъекта", "HTTPMeansObjectModule")]
    public class HTTPMeansDataProcessorObjectModule : AutoContext<HTTPMeansDataProcessorObjectModule>
    {
    }
}
