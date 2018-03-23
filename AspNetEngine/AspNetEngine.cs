// Copyright (c) Yury Deshin 2018
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Runtime.Caching;
using System.IO;

using ScriptEngine;
using ScriptEngine.Machine;
using ScriptEngine.Environment;
using ScriptEngine.HostedScript;

using System.Runtime.CompilerServices;

namespace OneScript.HttpServices
{
    public class AspNetHostEngine
    {
        HostedScriptEngine _hostedScript;
        public HostedScriptEngine Engine
        {
            get
            {
                return _hostedScript;
            }
        }

        // Разрешает или запрещает кэширование исходников *.os В Linux должно быть false иначе после изменений исходника старая версия будет в кэше
        // web.config -> <appSettings> -> <add key="CachingEnabled" value="true"/>
        //
        static bool _cachingEnabled;
        public static bool CachingEnabled
        {
            get
            {
                return _cachingEnabled;
            }
        }

        // Поскольку одновременное создание HostEngine и выполнение кода невозможны
        // при старте приложения создается набор Engine, которые используются для
        // обслуживания клиентских запросов и выполнения кода в потоках
        //
        static System.Collections.Concurrent.ConcurrentQueue<AspNetHostEngine> _pool;
        public static System.Collections.Concurrent.ConcurrentQueue<AspNetHostEngine> Pool
        {
            get
            {
                return _pool;
            }
        }

        // Список дополнительных сборок, которые надо приаттачить к движку. Могут быть разные расширения
        // web.config -> <appSettings> -> <add key="ASPNetHandler" value="attachAssembly"/> Сделано так для простоты. Меньше настроек - дольше жизнь :)
        // не нужен наверное
        static List<System.Reflection.Assembly> _assembliesForAttaching;

        static AspNetHostEngine()
        {
            // Загружаем сборки библиотек
            _assembliesForAttaching = new List<System.Reflection.Assembly>();

            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;

            TextWriter logWriter = AspNetLog.Open(appSettings);

            _cachingEnabled = (appSettings["cachingEnabled"] == "true");
            AspNetLog.Write(logWriter, "Start assemblies loading.");

            foreach (string assemblyName in appSettings.AllKeys)
            {
                if (appSettings[assemblyName] == "attachAssembly")
                {
                    try
                    {
                        _assembliesForAttaching.Add(System.Reflection.Assembly.Load(assemblyName));
                    }
                    // TODO: Исправить - должно падать. Если конфиг сайта неработоспособен - сайт не должен быть работоспособен.
                    catch (Exception ex)
                    {
                        AspNetLog.Write(logWriter, "Error loading assembly: " + assemblyName + " " + ex.ToString());
                        if (appSettings["handlerLoadingPolicy"] == "strict")
                            throw; // Must fail!
                    }
                }
            }

            // Загружаем ASPNetHandler.dll
            //try
            //{
            //    _assembliesForAttaching.Add(System.Reflection.Assembly.Load("ASPNETHandler"));
            //}
            // TODO: Исправить - должно падать. Если конфиг сайта неработоспособен - сайт не должен быть работоспособен.
            //catch (Exception ex)
            //{
            //    AspNetLog.Write(logWriter, "Error loading assembly: ASPNetHandler" + " " + ex.ToString());
            //    if (appSettings["handlerLoadingPolicy"] == "strict")
            //        throw; // Must fail!
            //}

            AspNetLog.Write(logWriter, "Stop assemblies loading.");

            // ToDo: Загружаем и компилируем общие модули

            // Создаем пул экземпляров ядра движка
            int workerThreads = 0;
            int completionPortThreads = 0;

            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            while (workerThreads > 0)
            {
                _pool.Enqueue(new AspNetHostEngine());
                workerThreads--;
            }


            AspNetLog.Close(logWriter);
        }

        public AspNetHostEngine()
        {


            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;

            // Инициализируем логгирование, если надо
            TextWriter logWriter = AspNetLog.Open(appSettings);

            AspNetLog.Write(logWriter, "Start loading. " + DateTime.Now.Ticks.ToString());

            if (appSettings == null)
                AspNetLog.Write(logWriter, "appSettings is null");

            try
            {
                _hostedScript = new HostedScriptEngine();
                // метод настраивает внутренние переменные у SystemGlobalContext
                _hostedScript.SetGlobalEnvironment(new NullApplicationHost(), new NullEntryScriptSrc());
                _hostedScript.Initialize();
                // Размещаем oscript.cfg вместе с web.config. Так наверное привычнее
                _hostedScript.CustomConfig = appSettings["configFilePath"] ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oscript.cfg");
                //_hostedScript.AttachAssembly(System.Reflection.Assembly.GetExecutingAssembly());
                // Аттачим доп сборки. По идее должны лежать в Bin
                foreach (System.Reflection.Assembly assembly in _assembliesForAttaching)
                {
                    try
                    {
                        _hostedScript.AttachAssembly(assembly);
                    }
                    catch (Exception ex)
                    {
                        // Возникла проблема при аттаче сборки
                        AspNetLog.Write(logWriter, "Assembly attaching error: " + ex.Message);
                        if (appSettings["handlerLoadingPolicy"] == "strict")
                            throw;
                    }
                }

                //Загружаем библиотечные скрипты aka общие модули
                string libPath = ConvertRelativePathToPhysical(appSettings["commonModulesPath"]);
                if (libPath != null)
                {
                    string[] files = System.IO.Directory.GetFiles(libPath, "*.os");
                    foreach (string filePathName in files)
                    {
                        _hostedScript.InjectGlobalProperty(System.IO.Path.GetFileNameWithoutExtension(filePathName), ValueFactory.Create(), true);
                    }

                    foreach (string filePathName in files)
                    {
                        try
                        {
                            ICodeSource src = _hostedScript.Loader.FromFile(filePathName);

                            var compilerService = _hostedScript.GetCompilerService();
                            var module = compilerService.CreateModule(src);
                            var loaded = _hostedScript.EngineInstance.LoadModuleImage(module);
                            var instance = (IValue)_hostedScript.EngineInstance.NewObject(loaded);
                            _hostedScript.EngineInstance.Environment.SetGlobalProperty(System.IO.Path.GetFileNameWithoutExtension(filePathName), instance);
                        }
                        catch (Exception ex)
                        {
                            // Возникла проблема при загрузке файла os, логгируем, если логгирование включено
                            AspNetLog.Write(logWriter, "Error loading " + System.IO.Path.GetFileNameWithoutExtension(filePathName) + " : " + ex.Message);
                            if (appSettings["handlerLoadingPolicy"] == "strict")
                                throw;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                // Возникла проблема при инициализации
                AspNetLog.Write(logWriter, ex.ToString());

                if (appSettings["handlerLoadingPolicy"] == "strict")
                    throw; // Must fail!
            }
            finally
            {
                AspNetLog.Write(logWriter, "End loading.");
                AspNetLog.Close(logWriter);
            }
        }


        public void CallCommonModuleProcedure(string moduleName, string methodName, IValue[] parameters)
        {
            IRuntimeContextInstance commonModule = (IRuntimeContextInstance)_hostedScript.EngineInstance.Environment.GetGlobalProperty(moduleName);
            int methodId = commonModule.FindMethod(methodName);
            commonModule.CallAsProcedure(methodId, parameters);
        }

        public void CallCommonModuleProcedure(string fullName, IValue[] parameters)
        {
            // Разбиваем полное имя на имя модуля и имя метода
            string[] separator = { "." };
            string[] names = fullName.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            CallCommonModuleProcedure(names[0], names[1], parameters);
        }

        public IValue CallCommonModuleFunction(string moduleName, string methodName, IValue[] parameters)
        {
            IRuntimeContextInstance commonModule = (IRuntimeContextInstance)_hostedScript.EngineInstance.Environment.GetGlobalProperty(moduleName);
            int methodId = commonModule.FindMethod(methodName);
            IValue result = ValueFactory.Create();
            commonModule.CallAsFunction(methodId, parameters, out result);
            return result;
        }

        public IValue CallCommonModuleFunction(string fullName, IValue[] parameters)
        {
            // Разбиваем полное имя на имя модуля и имя метода
            string[] separator = { "." };
            string[] names = fullName.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            return CallCommonModuleFunction(names[0], names[1], parameters);
        }

        public static string ConvertRelativePathToPhysical(string path)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relPath = path.Replace("~", "");

            if (relPath.StartsWith("/"))
                relPath = relPath.Remove(0, 1);

            relPath = relPath.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
            return System.IO.Path.Combine(baseDir, relPath);
        }


    }
}

