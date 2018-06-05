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

using System.Web;
using System.Configuration;

namespace OneScript.HTTPService
{
    public class Enums : ILibraryAsPropertiesLoader
    {
        public List<string> GetPropertiesNamesForInjecting(string info)
        {
            List<string> propertiesNames = new List<string>();
            propertiesNames.Add("Перечисления");
            return propertiesNames;
        }

        public void AssignPropertiesValues(HostedScriptEngine engine)
        {
            StructureImpl enums = new StructureImpl();
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            string enumsPath = appSettings["enumsPath"];

            if (enumsPath != null)
                enumsPath = ConvertRelativePathToPhysical(enumsPath);

            string[] files = System.IO.Directory.GetFiles(enumsPath, "*.txt");

            foreach (string filePathName in files)
            {
                StructureImpl currentEnum = new StructureImpl();
                string[] enumValues = System.IO.File.ReadAllLines(filePathName);
                string enumName = System.IO.Path.GetFileNameWithoutExtension(filePathName);

                foreach (string currentValue in enumValues)
                {
                    currentEnum.Insert(currentValue, ValueFactory.Create(enumName + "." + currentValue));
                }

                enums.Insert(System.IO.Path.GetFileNameWithoutExtension(filePathName), new FixedStructureImpl(currentEnum));
            }

            engine.EngineInstance.Environment.SetGlobalProperty("Перечисления", new FixedStructureImpl(enums));
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
