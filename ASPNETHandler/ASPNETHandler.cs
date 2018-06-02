// Copyright (c) Yury Deshin 2018
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

namespace OneScript.ASPNETHandler
{
    public class ASPNETHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        AspNetHostEngine _engine;
        // Разрешает или запрещает кэширование исходников *.os В Linux должно быть false иначе после изменений исходника старая версия будет в кэше
        // web.config -> <appSettings> -> <add key="CachingEnabled" value="true"/>
        static bool _cachingEnabled;

        public bool IsReusable
        {
            // Разрешаем повторное использование и храним среду выполнения и контекст 
            get { return true; }
        }
        static ASPNETHandler()
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            _cachingEnabled = (appSettings["cachingEnabled"] == "true");

            // Заставляем создать пул
            int temp = AspNetHostEngine.Pool.Count;
        }

        public ASPNETHandler()
        {
        }

        public void ProcessRequest(HttpContext context)
        {
            // Получаем экземпляр engine из пула
            //
            AspNetHostEngine.Pool.TryDequeue(out _engine);

            try
            {
                _engine.Engine.EngineInstance.Environment.LoadMemory(MachineInstance.Current);
                CallScriptHandler(context);
                context.Response.End();
            }
            finally
            {
                if (_engine != null)
                    AspNetHostEngine.Pool.Enqueue(_engine);
                _engine = null;
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
        private IRuntimeContextInstance CreateServiceInstance(LoadedModuleHandle module)
        {
            var runner = _engine.Engine.EngineInstance.NewObject(module);
            return runner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoadedModuleHandle LoadByteCode(string filePath)
        {
            var code = _engine.Engine.EngineInstance.Loader.FromFile(filePath);
            var compiler = _engine.Engine.GetCompilerService();
            var byteCode = compiler.CreateModule(code);
            var module = _engine.Engine.EngineInstance.LoadModuleImage(byteCode);
            return module;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallScriptHandler(HttpContext context)
        {
            #region Загружаем скрипт (файл .os)
            // Кэшируем исходный файл, если файл изменился (изменили скрипт .os) загружаем заново
            // В Linux под Mono не работает подписка на изменение файла.
            LoadedModuleHandle? module = null;
            ObjectCache cache = MemoryCache.Default;

            if (_cachingEnabled)
            {
                module = cache[context.Request.PhysicalPath] as LoadedModuleHandle?;

                if (module == null)
                {
                    CacheItemPolicy policy = new CacheItemPolicy();

                    List<string> filePaths = new List<string>();
                    filePaths.Add(context.Request.PhysicalPath);
                    policy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));

                    // Загружаем файл и помещаем его в кэш
                    module = LoadByteCode(context.Request.PhysicalPath);
                    cache.Set(context.Request.PhysicalPath, module, policy);
                }
            }
            else
            {
                module = LoadByteCode(context.Request.PhysicalPath);
            }

            #endregion

            var runner = CreateServiceInstance(module.Value);

            ProduceResponse(context, runner);
        }
    }
}
