﻿// Copyright (c) Yury Deshin 2018
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Runtime.Caching;
using System.IO;

using ScriptEngine;
using ScriptEngine.Machine;
using ScriptEngine.Environment;
using ScriptEngine.HostedScript;
using OneScript.HTTPService;

using System.Runtime.CompilerServices;

namespace OneScript.HTTPService
{
    public class AspNetEngineHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        //AspNetHostEngine _engine;
        // Разрешает или запрещает кэширование исходников *.os В Linux должно быть false иначе после изменений исходника старая версия будет в кэше
        // web.config -> <appSettings> -> <add key="CachingEnabled" value="true"/>
        static bool _cachingEnabled;
        static bool _runAsJRPCServer;

        public bool IsReusable
        {
            // Разрешаем повторное использование и храним среду выполнения и контекст 
            get { return true; }
        }
        static AspNetEngineHandler()
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            _cachingEnabled = (appSettings["cachingEnabled"] == "true");
            _runAsJRPCServer = (appSettings["cachingEnabled"] == "true");

            // Заставляем создать пул
            int temp = AspNetHostEngine.Pool.Count;
        }

        public AspNetEngineHandler()
        {
        }

        public void ProcessRequest(HttpContext context)
        {
            // Получаем экземпляр engine из пула
            //
            AspNetHostEngine _eng;
            AspNetHostEngine.Pool.TryDequeue(out _eng);

            try
            {
                _eng.Engine.EngineInstance.Environment.LoadMemory(MachineInstance.Current);
                CallScriptHandler(context, _eng);
                context.Response.End();
            }
            finally
            {
                if (_eng != null)
                    AspNetHostEngine.Pool.Enqueue(_eng);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TextWriter OpenLog(System.Collections.Specialized.NameValueCollection appSettings = null)
        {
            if (appSettings == null)
                appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;

            string logPath = appSettings["logToPath"];

            try
            {
                if (logPath != null)
                {
                    logPath = HttpContext.Current.Server.MapPath(logPath);
                    string logFileName = Guid.NewGuid().ToString().Replace("-", "") + ".txt";
                    return File.CreateText(Path.Combine(logPath, logFileName));
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteToLog(TextWriter logWriter, string message)
        {
            if (logWriter == null)
                return;
            try
            {
                logWriter.WriteLine(message);
            }
            catch { /* что-то не так, ничего не делаем */ }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CloseLog(TextWriter logWriter)
        {
            if (logWriter != null)
            {
                try
                {
                    logWriter.Flush();
                    logWriter.Close();
                }
                catch
                { /*что-то не так, ничего не делаем.*/ }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProduceResponse(HttpContext context, IRuntimeContextInstance runner)
        {
            int methodIndex = runner.FindMethod("ОбработкаВызоваHTTPСервиса");
            IValue result;
            IValue[] args = new IValue[1];
            args[0] = new OneScript.HTTPService.HTTPServiceRequestImpl(context);
            runner.CallAsFunction(methodIndex, args, out result);

            // Обрабатываем результаты
            var response = (OneScript.HTTPService.HTTPServiceResponseImpl)result;
            context.Response.StatusCode = response.StatusCode;

            if (response.Headers != null)
            {
                foreach (var ch in response.Headers)
                {
                    context.Response.AddHeader(ch.Key.AsString(), ch.Value.AsString());
                }
            }

            if (response.Reason != "")
            {
                context.Response.Status = response.Reason;
            }

            if (response.BodyStream != null)
            {
                response.BodyStream.Seek(0, SeekOrigin.Begin);
                response.BodyStream.CopyTo(context.Response.OutputStream);
            }

            context.Response.Charset = response.ContentCharset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IRuntimeContextInstance CreateServiceInstance(LoadedModule module, AspNetHostEngine _eng)
        {
            var runner = _eng.Engine.EngineInstance.NewObject(module);
            return runner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoadedModule LoadByteCode(string filePath, AspNetHostEngine _eng)
        {
            var code = _eng.Engine.EngineInstance.Loader.FromFile(filePath);
            var compiler = _eng.Engine.GetCompilerService();
            var byteCode = compiler.Compile(code);
            var module = _eng.Engine.EngineInstance.LoadModuleImage(byteCode);
            return module;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallScriptHandler(HttpContext context, AspNetHostEngine _eng)
        {
            #region Загружаем скрипт (файл .os)
            // Кэшируем исходный файл, если файл изменился (изменили скрипт .os) загружаем заново
            // В Linux под Mono не работает подписка на изменение файла.
            LoadedModule module = null;
            //MemoryCache cache = MemoryCache.Default;
            ObjectCache cache = MemoryCache.Default;

            if (_cachingEnabled)
            {
                module = cache[context.Request.PhysicalPath] as LoadedModule;

                if (module == null)
                {

                    // Загружаем файл и помещаем его в кэш
                    if (!System.IO.File.Exists(context.Request.PhysicalPath))
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }

                    module = LoadByteCode(context.Request.PhysicalPath, _eng);
                    CacheItemPolicy policy = new CacheItemPolicy();
                    List<string> filePaths = new List<string>();
                    filePaths.Add(context.Request.PhysicalPath);
                    policy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));

                    cache.Set(context.Request.PhysicalPath, module, policy);
                }
            }
            else
            {
                if (!System.IO.File.Exists(context.Request.PhysicalPath))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                module = LoadByteCode(context.Request.PhysicalPath, _eng);
            }

            #endregion

            var runner = CreateServiceInstance(module, _eng);

            if (_runAsJRPCServer)
                ProduceJRPCResponse(context, runner);
            else
                ProduceResponse(context, runner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProduceJRPCResponse(HttpContext context, IRuntimeContextInstance runner)
        {
            // Преобразуем тело из json
            OneScript.HTTPService.HTTPServiceRequestImpl request = new OneScript.HTTPService.HTTPServiceRequestImpl(context);
            ScriptEngine.HostedScript.Library.Json.JSONReader reader = new ScriptEngine.HostedScript.Library.Json.JSONReader();
            reader.SetString(request.GetBodyAsString());
            ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions jsonFunctions = (ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions)ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions.CreateInstance();

            // Получаем запрос как структуру из тела
            ScriptEngine.HostedScript.Library.StructureImpl structOfParams = (ScriptEngine.HostedScript.Library.StructureImpl)jsonFunctions.ReadJSON(reader);

            if (!structOfParams.HasProperty("method"))
            {
                // Вызывать нечего, наверное это не jrpc
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Cannot find a method property";
                return;
            }

            // Создаем ответ
            ScriptEngine.HostedScript.Library.StructureImpl structOfResponse = ScriptEngine.HostedScript.Library.StructureImpl.Constructor();

            // Определяем версию jrpc
            string jrpcVersion = "1.0";

            if (structOfParams.HasProperty("jsonrpc"))
                jrpcVersion = structOfParams.GetPropValue("jsonrpc").ToString();

            // Получаем id
            IValue id = ValueFactory.Create();
            if (structOfParams.HasProperty("id"))
                id = structOfParams.GetPropValue("id");

            string methodName = structOfParams.GetPropValue("method").AsString();

            ScriptEngine.HostedScript.Library.ArrayImpl methodParams = null;
            if (structOfParams.HasProperty("params"))
                methodParams = (ScriptEngine.HostedScript.Library.ArrayImpl)structOfParams.GetPropValue("params");

            //methodParams.GetIndexedValue()
            int methodIndex = runner.FindMethod(methodName);
            MethodInfo methodInfo = runner.GetMethodInfo(methodIndex);
            IValue []args = new IValue[methodInfo.ArgCount];
            int i = 0;
            
            for(; i < methodParams.Count(); i++)
            {
                args[i] = methodParams.GetIndexedValue(ValueFactory.Create(i));
            }

            // Значения по умолчанию?

            IValue result = ValueFactory.Create();

            if (methodInfo.IsFunction)
                runner.CallAsFunction(methodIndex, args, out result);
            else
                runner.CallAsProcedure(methodIndex, args);

            // Обрабатываем результаты
            var response = (OneScript.HTTPService.HTTPServiceResponseImpl)result;
            context.Response.StatusCode = response.StatusCode;

            if (response.Headers != null)
            {
                foreach (var ch in response.Headers)
                {
                    context.Response.AddHeader(ch.Key.AsString(), ch.Value.AsString());
                }
            }

            if (response.Reason != "")
            {
                context.Response.Status = response.Reason;
            }

            if (response.BodyStream != null)
            {
                response.BodyStream.Seek(0, SeekOrigin.Begin);
                response.BodyStream.CopyTo(context.Response.OutputStream);
            }

            context.Response.Charset = response.ContentCharset;
        }
    }
}
