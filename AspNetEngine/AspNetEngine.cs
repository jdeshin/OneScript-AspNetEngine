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
                List<System.Reflection.Assembly> assembliesForAttaching = GetAssembliesForAttaching(appSettings, logWriter);
                System.Collections.Hashtable commonModulesForLoading = GetCommonModulesForLoading(appSettings, logWriter);
                List<PropertiesInjectorInfo> propertiesInjectorsInfo = GetPropertiesInjectors(appSettings, logWriter);

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
                    _pool.Enqueue(new AspNetHostEngine(assembliesForAttaching, propertiesInjectorsInfo, commonModulesForLoading));
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

        static List<System.Reflection.Assembly> GetAssembliesForAttaching(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            List<System.Reflection.Assembly> assembliesForAttaching = new List<System.Reflection.Assembly>();

            foreach (string assemblyName in appSettings.AllKeys)
            {
                if (appSettings[assemblyName] == "attachAssembly")
                {
                    try
                    {
                        assembliesForAttaching.Add(System.Reflection.Assembly.Load(assemblyName));
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
            assembliesForAttaching.Add(System.Reflection.Assembly.GetExecutingAssembly());
            return assembliesForAttaching;
        }

        // Возвращает список объектов PropertiesInjector, которые содержат информацию о библиотеке, которая вставляет свойства в HostEngine
        // Сборка должна быть загружена с использованием attachAssembly
        //
        static List<PropertiesInjectorInfo> GetPropertiesInjectors(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            List<PropertiesInjectorInfo> propertiesInjectors = new List<PropertiesInjectorInfo>();

            // Пробегаем по всем ключам appSettings
            foreach (string assemblyInfo in appSettings.AllKeys)
            {
                // name="propertiesInjector;ИмяСборки;ИмяКласса"
                string[] strInjectorInfo = assemblyInfo.Replace(" ", "").Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                if (strInjectorInfo.Length < 3)
                    continue;

                if (strInjectorInfo[0] != "propertiesInjector")
                    continue;

                PropertiesInjectorInfo injectorInfo = new PropertiesInjectorInfo();
                injectorInfo.AssemblyName = strInjectorInfo[1];
                injectorInfo.ClassName = strInjectorInfo[2];
                injectorInfo.Info = appSettings[assemblyInfo];

                propertiesInjectors.Add(injectorInfo);
            }

            return propertiesInjectors;
        }

        static System.Collections.Hashtable GetCommonModulesForLoading(System.Collections.Specialized.NameValueCollection appSettings, TextWriter logWriter)
        {
            System.Collections.Hashtable commonModules = new System.Collections.Hashtable();

            string libPath = ConvertRelativePathToPhysical(appSettings["commonModulesPath"]);

            if (libPath != null)
            {
                string[] files = System.IO.Directory.GetFiles(libPath, "*.os");

                foreach (string filePathName in files)
                {
                    commonModules.Add(System.IO.Path.GetFileNameWithoutExtension(filePathName), System.IO.File.ReadAllText(filePathName));
                }
            }

            return commonModules;
        }

        public AspNetHostEngine(
            List<System.Reflection.Assembly> assembliesForAttaching,
            List<PropertiesInjectorInfo> propertiesInjectorsInfo,
            System.Collections.Hashtable commonModulesForLoading
            )
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
            foreach (System.Reflection.Assembly assembly in assembliesForAttaching)
            {
                    _hostedScript.AttachAssembly(assembly);
            }

            // Добавляем свойства для общих модулей
            foreach (System.Collections.DictionaryEntry cm in commonModulesForLoading)
            {
                _hostedScript.InjectGlobalProperty((string)cm.Key, ValueFactory.Create(), true);
            }

            // Загружаем классы инжекторов свойств
            List<PropertiesInjector> propertiesInjectors = new List<PropertiesInjector>();
            foreach(PropertiesInjectorInfo ci in propertiesInjectorsInfo)
            {
                    ILibraryAsPropertiesLoader instance = (ILibraryAsPropertiesLoader)(Activator.CreateInstance(ci.AssemblyName, ci.ClassName).Unwrap());
                    propertiesInjectors.Add(new PropertiesInjector(instance, ci.Info));
            }

            // Получаем и вставляем списки свойств. Класс инжектора должен иметь метод GetPropertyNamesForInjecting
            foreach(PropertiesInjector injector in propertiesInjectors)
            {
                List<string> propertiesNames = injector.Loader.GetPropertiesNamesForInjecting(injector.Info);
                foreach(string cp in propertiesNames)
                    _hostedScript.InjectGlobalProperty(cp, ValueFactory.Create(), true);
            }

            // Подключаем общие модули
            foreach (System.Collections.DictionaryEntry cm in commonModulesForLoading)
            {
                ICodeSource src = _hostedScript.Loader.FromString((string)cm.Value);

                var compilerService = _hostedScript.GetCompilerService();
                var module = compilerService.CreateModule(src);
                var loaded = _hostedScript.EngineInstance.LoadModuleImage(module);
                var instance = (IValue)_hostedScript.EngineInstance.NewObject(loaded);
                _hostedScript.EngineInstance.Environment.SetGlobalProperty((string)cm.Key, instance);
            }

            // Присваиваем значения свойств
            foreach (PropertiesInjector injector in propertiesInjectors)
                injector.Loader.AssignPropertiesValues(_hostedScript);

            _hostedScript.EngineInstance.Environment.LoadMemory(MachineInstance.Current);
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

    // Класс отражает запись в web.config name="propertiesInjector;ИмяСборки;ИмяКласса" value="Info"
    public class PropertiesInjectorInfo
    {
        public string AssemblyName
        {
            get;
            set;
        }

        public string ClassName
        {
            get;
            set;
        }

        public string Info
        {
            get;
            set;
        }
    }

    // Класс отражает загруженный инжектор
    public class PropertiesInjector
    {
        public ILibraryAsPropertiesLoader Loader
        {
            get;
            set;
        }

        public string Info
        {
            get;
            set;
        }

        public PropertiesInjector(ILibraryAsPropertiesLoader loader, string info)
        {
            Loader = loader;
            Info = info;
        }
    }
}

