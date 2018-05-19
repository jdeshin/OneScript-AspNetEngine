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

namespace OneScript.HTTPService
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
        static System.Collections.Hashtable _commonModules;

        static AspNetHostEngine()
        {

            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;

            TextWriter logWriter = AspNetLog.Open(appSettings);

            _pool = new System.Collections.Concurrent.ConcurrentQueue<AspNetHostEngine>();
            // Загружаем сборки библиотек

            _cachingEnabled = (appSettings["cachingEnabled"] == "true");
            AspNetLog.Write(logWriter, "Start assemblies loading.");
            try
            {
                LoadAssemblies(appSettings, logWriter);
                LoadModules(appSettings, logWriter);
                System.Collections.Hashtable dataProcessorManagerFiles = GetDataProcessorManagerFiles(appSettings, logWriter);
                System.Collections.Hashtable dataProcessorObjectFiles = GetDataProcessorObjectFiles(appSettings, logWriter);

                // Создаем пул экземпляров ядра движка
                int workerThreads = 0;
                int completionPortThreads = 0;
                int enginesCount = 0;
                ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

                if (appSettings["maxThreads"] != null)
                    enginesCount = Convert.ToInt32(appSettings["maxThreads"]);
                else
                    enginesCount = workerThreads;

                if (enginesCount > workerThreads)
                    enginesCount = workerThreads;

                AspNetLog.Write(logWriter, "Max threads running/worker/completionPort: " + enginesCount.ToString() + "/"+ workerThreads.ToString() + "/" + completionPortThreads.ToString());

                while (enginesCount > 0)
                {
                    _pool.Enqueue(new AspNetHostEngine(dataProcessorManagerFiles, dataProcessorObjectFiles));
                    enginesCount--;
                }
            }
            catch (Exception ex)
            {
                AspNetLog.Write(logWriter, ex.ToString());
            }
            AspNetLog.Write(logWriter, "Stop assemblies loading.");
            AspNetLog.Close(logWriter);
        }

        static void LoadAssemblies(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            _assembliesForAttaching = new List<System.Reflection.Assembly>();

            foreach (string assemblyName in appSettings.AllKeys)
            {
                if (appSettings[assemblyName] == "attachAssembly")
                {
                    try
                    {
                        _assembliesForAttaching.Add(System.Reflection.Assembly.Load(assemblyName));
                        AspNetLog.Write(logWriter, "loading: " + assemblyName);
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

            // Добавляем текущую сборку т.к. она содержит типы HTTPServiceRequest, HTTPServiceResponse
            //
            _assembliesForAttaching.Add(System.Reflection.Assembly.GetExecutingAssembly());

        }
        static void LoadModules(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            _commonModules = new System.Collections.Hashtable();

            string libPath = ConvertRelativePathToPhysical(appSettings["commonModulesPath"]);

            if (libPath != null)
            {
                string[] files = System.IO.Directory.GetFiles(libPath, "*.os");

                foreach (string filePathName in files)
                {
                    _commonModules.Add(System.IO.Path.GetFileNameWithoutExtension(filePathName), System.IO.File.ReadAllText(filePathName));
                }
            }
        }

        static System.Collections.Hashtable GetDataProcessorManagerFiles(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            System.Collections.Hashtable managerModules = new System.Collections.Hashtable();

            string libPath = ConvertRelativePathToPhysical(appSettings["dataProcessorsPath"]);

            if (libPath != null)
            {
                string [] files = System.IO.Directory.GetFiles(libPath, "*.ManagerModule.os");

                foreach (string filePathName in files)
                {
                    managerModules.Add(System.IO.Path.GetFileNameWithoutExtension(filePathName).Replace(".ManagerModule", ""), System.IO.File.ReadAllText(filePathName));
                }
            }

            return managerModules;
        }

        static System.Collections.Hashtable GetDataProcessorObjectFiles(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            System.Collections.Hashtable managerModules = new System.Collections.Hashtable();

            string libPath = ConvertRelativePathToPhysical(appSettings["dataProcessorsPath"]);

            if (libPath != null)
            {
                string[] files = System.IO.Directory.GetFiles(libPath, "*.ObjectModule.os");

                foreach (string filePathName in files)
                {
                    managerModules.Add(System.IO.Path.GetFileNameWithoutExtension(filePathName).Replace(".ObjectModule", ""), System.IO.File.ReadAllText(filePathName));
                }
            }

            return managerModules;
        }

        public AspNetHostEngine(System.Collections.Hashtable dataProcessorManagerFiles, System.Collections.Hashtable dataProcessorObjectFiles)
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;

            _hostedScript = new HostedScriptEngine();

            // метод настраивает внутренние переменные у SystemGlobalContext
            if (appSettings["enableEcho"] == "true")
                _hostedScript.SetGlobalEnvironment(new ASPNetApplicationHost(), new AspEntryScriptSrc(appSettings["startupScript"] ?? HttpContext.Current.Server.MapPath("~/web.config")));
            else
                _hostedScript.SetGlobalEnvironment(new AspNetNullApplicationHost(), new AspNetNullEntryScriptSrc());

            _hostedScript.Initialize();

            // Размещаем oscript.cfg вместе с web.config. Так наверное привычнее
            _hostedScript.CustomConfig = appSettings["configFilePath"] ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oscript.cfg");

            

            // Аттачим доп сборки. По идее должны лежать в Bin
            foreach (System.Reflection.Assembly assembly in _assembliesForAttaching)
            {
                    _hostedScript.AttachAssembly(assembly);
            }

            // Добавляем свойства для общих модулей
            foreach (System.Collections.DictionaryEntry cm in _commonModules)
            {
                _hostedScript.InjectGlobalProperty((string)cm.Key, ValueFactory.Create(), true);
            }

            // Добавляем свойства для обработок
            _hostedScript.InjectGlobalProperty("Обработки", ValueFactory.Create(), true);
            _hostedScript.InjectGlobalProperty("ОбработкаМенеджерФункцииПлатформы", ValueFactory.Create(), true);

            // Подключаем общие модули
            foreach (System.Collections.DictionaryEntry cm in _commonModules)
            {
                ICodeSource src = _hostedScript.Loader.FromString((string)cm.Value);

                var compilerService = _hostedScript.GetCompilerService();
                var module = compilerService.CreateModule(src);
                var loaded = _hostedScript.EngineInstance.LoadModuleImage(module);
                var instance = (IValue)_hostedScript.EngineInstance.NewObject(loaded);
                _hostedScript.EngineInstance.Environment.SetGlobalProperty((string)cm.Key, instance);
            }

            // Подключаем обработки
            _hostedScript.EngineInstance.Environment.SetGlobalProperty("ОбработкаМенеджерФункцииПлатформы", new DataProcessorsManagerImpl(_hostedScript, dataProcessorManagerFiles, dataProcessorObjectFiles));

            // Подключаем обработки-библиотеки
            // Для будующих расширений
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

