// Copyright (c) Yury Deshin 2018
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library;
using ScriptEngine.HostedScript.Library.Binary;

using ScriptEngine;
using ScriptEngine.Environment;
using ScriptEngine.HostedScript;


namespace OneScript.HTTPService
{
    public class DataProcessors : ILibraryAsPropertiesLoader
    {
        static System.Collections.Hashtable _managerModules;
        static System.Collections.Hashtable _objectModules;

        public List<string> GetPropertiesNamesForInjecting(string info)
        {
            List<string> propertiesNames = new List<string>();
            propertiesNames.Add("Обработки");
            propertiesNames.Add("ОбработкаМенеджерБазовый");
            return propertiesNames;
        }

        public void AssignPropertiesValues(HostedScriptEngine engine)
        {
            DataProcessorsManagerImpl dataProcessorManager = new DataProcessorsManagerImpl(engine, _managerModules, _objectModules);
            engine.EngineInstance.Environment.SetGlobalProperty("ОбработкаМенеджерБазовый", dataProcessorManager);
        }

        static DataProcessors()
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            string libPath = ConvertRelativePathToPhysical(appSettings["dataProcessorsPath"]);

            _managerModules = GetDataProcessorModules(libPath, "МодульМенеджера");
            _objectModules = GetDataProcessorModules(libPath, "МодульОбъекта");

        }

        static System.Collections.Hashtable GetDataProcessorModules(string libPath, string moduleMask)
        {
            System.Collections.Hashtable modules = new System.Collections.Hashtable();

            if (libPath != null)
            {
                string[] files = System.IO.Directory.GetFiles(libPath, "*." + moduleMask + ".os");

                foreach (string filePathName in files)
                {
                    modules.Add(System.IO.Path.GetFileNameWithoutExtension(filePathName).Replace("." + moduleMask, ""), System.IO.File.ReadAllText(filePathName));
                }
            }

            return modules;
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
