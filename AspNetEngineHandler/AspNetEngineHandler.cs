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

namespace OneScript.HTTPService
{
    public class AspNetEngineHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        public struct JRPCNotificationParams
        {
            public string pathName;
            public ScriptEngine.HostedScript.Library.StructureImpl jrpcParams;
        };

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
            _runAsJRPCServer = (appSettings["runAsJRPCServer"] == "true");

            // Заставляем создать пул
            //int temp = AspNetHostEngine.Pool.Count;
            AspNetHostEngine.Init();
        }

        public AspNetEngineHandler()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessRequest(HttpContext context)
        {
            if (_runAsJRPCServer)
                ProcessJRPCRequest(context);
            else
                ProcessWebRequest(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessWebRequest(HttpContext context, ScriptEngine.HostedScript.Library.StructureImpl jrpcParams = null)
        {
            AspNetHostEngine eng = null;

            try
            {
                AspNetHostEngine.DequeEngine(out eng);
                CallScriptHandler(context, eng, jrpcParams);
                context.Response.End();
            }
            finally
            {
                AspNetHostEngine.EnqueEngine(eng);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessJRPCRequest(HttpContext context)
        {
            ScriptEngine.HostedScript.Library.StructureImpl structOfParams = null;

            try
            {
                structOfParams = GetJRPCParams(context);
            }
            catch (Exception ex)
            {
                // Ошибка парсинга
                GenerateErrorResponse(-32700, ex.Message, context, ValueFactory.Create());
                context.Response.End();
                return;
            }

            // Проверяем тип свойства method
            if (!structOfParams.HasProperty("method") || structOfParams.GetPropValue("method").DataType != DataType.String)
            {
                // Вызывать нечего, наверное это не jrpc. Неправильный запрос
                GenerateErrorResponse(-32600, "Bad method property type", context, ValueFactory.Create());
                context.Response.End();
                return;
            }

            // Получаем id
            IValue id = ValueFactory.CreateNullValue();
            if (structOfParams.HasProperty("id"))
                id = structOfParams.GetPropValue("id");

         
            if (id == ValueFactory.CreateNullValue() || id == null)
            {
                // Это notification
                // Выполняем асинхронно
                ProcessJRPCNotificationRequest(context, structOfParams);
                context.Response.StatusCode = 200;
                context.Response.End();
                return;
            }

            // Выполняем синхронно
            ProcessWebRequest(context, structOfParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessJRPCNotificationRequest(HttpContext context, ScriptEngine.HostedScript.Library.StructureImpl jrpcParams)
        {
            // Проверяем доступность эндпоинта
            LoadedModule module = null;
            ObjectCache cache = MemoryCache.Default;

            if (_cachingEnabled)
            {
                module = cache[context.Request.PhysicalPath] as LoadedModule;

                if (module == null)
                {
                    if (!System.IO.File.Exists(context.Request.PhysicalPath))
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }
            }
            else
            {
                if (!System.IO.File.Exists(context.Request.PhysicalPath))
                {
                    context.Response.StatusCode = 404;
                    return;
                }
            }

            // Помещаем в очередь
            JRPCNotificationParams notificationParams = new JRPCNotificationParams();
            notificationParams.pathName = context.Request.PhysicalPath;
            notificationParams.jrpcParams = jrpcParams;
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(AspNetEngineHandler.ExecuteJRPCNotification), notificationParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteJRPCNotification(object notificationParams)
        {
            AspNetHostEngine eng = null;
            ScriptEngine.HostedScript.Library.StructureImpl jrpcParams = ((JRPCNotificationParams)notificationParams).jrpcParams;
            try
            {
                AspNetHostEngine.DequeEngine(out eng);
                LoadedModule module = eng.LoadModule(((JRPCNotificationParams)notificationParams).pathName);
                var runner = eng.CreateServiceInstance(module);

                string methodName = jrpcParams.GetPropValue("method").AsString();
                int methodIndex = runner.FindMethod(methodName);

                // Получаем массив параметров. Поддерживаются только запросы с папаметрами попозиционно
                ScriptEngine.HostedScript.Library.ArrayImpl methodParams = null;
                if (jrpcParams.HasProperty("params"))
                    methodParams = (ScriptEngine.HostedScript.Library.ArrayImpl)jrpcParams.GetPropValue("params");

                // Формируем массив параметров для вызова
                MethodInfo methodInfo = runner.GetMethodInfo(methodIndex);
                IValue[] args;

                if (methodParams != null)
                {
                    args = new IValue[methodParams.Count()];
                    int i = 0;

                    for (; i < methodParams.Count(); i++)
                    {
                        args[i] = methodParams.GetIndexedValue(ValueFactory.Create(i));
                    }
                }
                else
                    args = new IValue[0];

                // Значения по умолчанию?

                IValue result = ValueFactory.Create();
                // вызываем функцию или процедуру
                if (methodInfo.IsFunction)
                    runner.CallAsFunction(methodIndex, args, out result);
                else
                    runner.CallAsProcedure(methodIndex, args);
            }
            finally
            {
                AspNetHostEngine.EnqueEngine(eng);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ScriptEngine.HostedScript.Library.StructureImpl GetJRPCParams(HttpContext context)
        {
            ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions jsonFunctions = (ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions)ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions.CreateInstance();

            // Получаем параметры запроса
            ScriptEngine.HostedScript.Library.StructureImpl structOfParams = null;

            // Преобразуем тело из json
            OneScript.HTTPService.HTTPServiceRequestImpl request = new OneScript.HTTPService.HTTPServiceRequestImpl(context);
            ScriptEngine.HostedScript.Library.Json.JSONReader reader = new ScriptEngine.HostedScript.Library.Json.JSONReader();
            reader.SetString(request.GetBodyAsString());
            // Получаем запрос как структуру из тела
            structOfParams = (ScriptEngine.HostedScript.Library.StructureImpl)jsonFunctions.ReadJSON(reader);
            reader.Close();

            return structOfParams;
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
        private void CallScriptHandler(HttpContext context, AspNetHostEngine eng, ScriptEngine.HostedScript.Library.StructureImpl jrpcParams = null)
        {
            LoadedModule module = eng.LoadModule(context.Request.PhysicalPath);

            if (module == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var runner = eng.CreateServiceInstance(module);

            if (jrpcParams != null)
                ProduceJRPCResponse(context, runner, jrpcParams);
            else
                ProduceResponse(context, runner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProduceJRPCResponse(HttpContext context, IRuntimeContextInstance runner, ScriptEngine.HostedScript.Library.StructureImpl jrpcParams)
        {
            ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions jsonFunctions = (ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions)ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions.CreateInstance();
            IValue id = jrpcParams.GetPropValue("id");
            // Определяем версию jrpc. Версия 1 не содержит этого свойства, однако обрабатываем запрос
            string jrpcVersion = "1.0";

            if (jrpcParams.HasProperty("jsonrpc"))
                jrpcVersion = jrpcParams.GetPropValue("jsonrpc").ToString();

            int methodIndex = -1;
            try
            {
                string methodName = jrpcParams.GetPropValue("method").AsString();
                methodIndex = runner.FindMethod(methodName);
            }
            catch(Exception ex)
            {
                // Метод не найден
                GenerateErrorResponse(-32601, ex.Message, context, id);
                return;
            }

            // Получаем массив параметров. Поддерживаются только запросы с папаметрами попозиционно
            ScriptEngine.HostedScript.Library.ArrayImpl methodParams = null;
            if (jrpcParams.HasProperty("params"))
            {
                try
                {
                    methodParams = (ScriptEngine.HostedScript.Library.ArrayImpl)jrpcParams.GetPropValue("params");
                }
                catch(Exception ex)
                {
                    GenerateErrorResponse(-32602, ex.Message, context, id);
                    return;

                }
            }
            // Формируем массив параметров для вызова
            MethodInfo methodInfo = runner.GetMethodInfo(methodIndex);
            IValue[] args;

            if (methodParams != null)
            {
                args = new IValue[methodParams.Count()];
                int i = 0;

                for (; i < methodParams.Count(); i++)
                {
                    args[i] = methodParams.GetIndexedValue(ValueFactory.Create(i));
                }
            }
            else
                args = new IValue[0];

            // Значения по умолчанию?

            IValue result = ValueFactory.Create();
            ScriptEngine.HostedScript.Library.StructureImpl structOfResponse = ScriptEngine.HostedScript.Library.StructureImpl.Constructor();

            try
            {
                // вызываем функцию или процедуру
                if (methodInfo.IsFunction)
                {
                    runner.CallAsFunction(methodIndex, args, out result);
                    structOfResponse.Insert("result", result);
                }
                else
                    runner.CallAsProcedure(methodIndex, args);
            }
            catch(Exception ex)
            {
                // Внутренняя ошибка
                GenerateErrorResponse(-32603, ex.Message, context, id);
                return;
            }

            // Обрабатываем результаты
            context.Response.StatusCode = 200;
            // Создаем ответ
            structOfResponse.Insert("jsonrpc", ValueFactory.Create("2.0"));
            structOfResponse.Insert("id", id);

            ScriptEngine.HostedScript.Library.Json.JSONWriter writer = new ScriptEngine.HostedScript.Library.Json.JSONWriter();

            writer.SetString();
            jsonFunctions.WriteJSON(writer, structOfResponse);
            context.Response.Charset = "utf-8";
            context.Response.Output.Write(writer.Close());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateErrorResponse(int errorCode, string errorMessage, HttpContext context, IValue id)
        {
            ScriptEngine.HostedScript.Library.StructureImpl structOfResponse = ScriptEngine.HostedScript.Library.StructureImpl.Constructor();
            ScriptEngine.HostedScript.Library.StructureImpl structOfError = ScriptEngine.HostedScript.Library.StructureImpl.Constructor();
            ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions jsonFunctions = (ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions)ScriptEngine.HostedScript.Library.Json.GlobalJsonFunctions.CreateInstance();
            ScriptEngine.HostedScript.Library.Json.JSONWriter writer = new ScriptEngine.HostedScript.Library.Json.JSONWriter();

            structOfResponse.Insert("id", id);

            structOfError.Insert("code", ValueFactory.Create(errorCode));
            structOfError.Insert("message", ValueFactory.Create(errorMessage));
            structOfResponse.Insert("error", structOfError);
            writer.SetString();
            jsonFunctions.WriteJSON(writer, structOfResponse);
            context.Response.Charset = "utf-8";
            context.Response.Output.Write(writer.Close());
            context.Response.StatusCode = 200;
        }
    }
}
