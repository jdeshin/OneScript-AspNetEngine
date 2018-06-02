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
    [GlobalContext(Category = "Общие макеты")]
    public class TemplatesManagerImpl : GlobalContextBase<TemplatesManagerImpl>
    {
        [ContextMethod("ПолучитьОбщийМакет", "GetCommonTemplate")]
        public IValue GetCommonTemplate(string templateName)
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            string _commonTemplatesPath = ConvertRelativePathToPhysical(appSettings["commonTemplatesPath"]);
            string fileNameWithoutExtension = templateName;

            return LoadTemplate(_commonTemplatesPath, templateName);            
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

        public static IValue LoadTemplate(string path, string fileNameWithoutExtension)
        {
            if (System.IO.File.Exists(path + fileNameWithoutExtension + ".txt"))
            {
                TextDocumentContext document = new TextDocumentContext();
                document.Read(path + fileNameWithoutExtension + ".txt");
                return document;
            }

            if (System.IO.File.Exists(path + fileNameWithoutExtension + ".thtml"))
                return new HTMLDocumentShellImpl(path + fileNameWithoutExtension + ".thtml");

            if (System.IO.File.Exists(path + fileNameWithoutExtension + ".bin"))
                return new BinaryDataContext(System.IO.File.ReadAllBytes(path + fileNameWithoutExtension + ".bin"));

            throw new Exception("Cannot find template: " + fileNameWithoutExtension);
        }

        public static IAttachableContext CreateInstance()
        {
            return new TemplatesManagerImpl();
        }

    }
}
