﻿using System;
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
    public class CommonTemplatesManagerImpl : GlobalContextBase<CommonTemplatesManagerImpl>
    {
        
        //static string _commonTemplatesPath;

        static CommonTemplatesManagerImpl()
        {
            //System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            //_commonTemplatesPath = ConvertRelativePathToPhysical(appSettings["commonTemplatesPath"]);
        }
        
        [ContextMethod("ПолучитьОбщийМакет", "GetCommonTemplate")]
        public IValue GetCommonTemplate(string templateName)
        {
            
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            string _commonTemplatesPath = ConvertRelativePathToPhysical(appSettings["commonTemplatesPath"]);

            System.IO.TextWriter tw = System.IO.File.CreateText("c:\\1\\1.txt");
            tw.WriteLine(_commonTemplatesPath);
            tw.WriteLine(templateName);
            tw.WriteLine(_commonTemplatesPath + templateName + ".txt");
            tw.Close();

            // Создаем объект из модуля объекта
            string fileNameWithoutExtension = templateName;

            if (System.IO.File.Exists(_commonTemplatesPath + fileNameWithoutExtension + ".txt"))
            {
                TextDocumentContext document = new TextDocumentContext();
                document.SetText(System.IO.File.ReadAllText(_commonTemplatesPath + fileNameWithoutExtension + ".txt"));
                return document;
            }

            if (System.IO.File.Exists(_commonTemplatesPath + fileNameWithoutExtension + ".thtml"))
                return new HTMLDocumentShellImpl(_commonTemplatesPath + fileNameWithoutExtension + ".thtml");

            if (System.IO.File.Exists(_commonTemplatesPath + fileNameWithoutExtension + ".bin"))
                return new BinaryDataContext(System.IO.File.ReadAllBytes(_commonTemplatesPath + fileNameWithoutExtension + ".thtml"));

            throw new Exception("Cannot find template: " + templateName);
            
            //return ValueFactory.Create("");

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

        public static IAttachableContext CreateInstance()
        {
            return new CommonTemplatesManagerImpl();
        }

    }
}
