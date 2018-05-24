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
    [ContextClass("ОбработкаМенеджерБазовый", "DataProcessorManagerBase")]
    public class DataProcessorsManagerImpl : AutoContext<DataProcessorsManagerImpl>
    {
        HostedScriptEngine _hostedScript;
        System.Collections.Hashtable _dataProcessorObjectModules; // Список модулей объекта обработок

        public DataProcessorsManagerImpl(HostedScriptEngine hostedScript, System.Collections.Hashtable managerFiles, System.Collections.Hashtable objectFiles)
        {
            _hostedScript = hostedScript;
            _dataProcessorObjectModules = new System.Collections.Hashtable();

            foreach (System.Collections.DictionaryEntry co in objectFiles)
            {
                ICodeSource src = _hostedScript.Loader.FromString((string)co.Value);

                var compilerService = _hostedScript.GetCompilerService();
                var module = compilerService.CreateModule(src);

                var _loaded = _hostedScript.EngineInstance.LoadModuleImage(module);
                _dataProcessorObjectModules.Add(co.Key, _loaded);
            }

            AddDataProcessorManagers(managerFiles);
        }

        [ContextMethod("Создать", "Create")]
        public IValue Create(string name)
        {
            // Создаем объект из модуля объекта
            return (IValue)_hostedScript.EngineInstance.NewObject((LoadedModuleHandle)_dataProcessorObjectModules[name]);
        }

        [ContextMethod("ПолучитьМакет", "GetTemplate")]
        public IValue GetTemplate(string objectName, string templateName)
        {
            // Создаем объект из модуля объекта
            return ValueFactory.Create();
        }


        public const string MandatoryMethodsText =
@"// 
Функция Создать() Экспорт
    Возврат ОбработкаМенеджерБазовый.Создать(""{{DataProcessorName}}"");
КонецФункции
//
Функция ПолучитьМакет(ИмяМакета) Экспорт
    Возврат ОбработкаМенеджерБазовый.ПолучитьМакет(""{{DataProcessorName}}"", ИмяМакета);
КонецФункции
//
";

        public string InsertMandatoryMethods(string managerModuleText, string name)
        {
            return MandatoryMethodsText.Replace("{{DataProcessorName}}", name) + managerModuleText;
        }

        public void AddDataProcessorManagers(System.Collections.Hashtable managerFiles)
        {
            ScriptEngine.HostedScript.Library.StructureImpl dataProcessors = new ScriptEngine.HostedScript.Library.StructureImpl();
            // Добавляем обработки, написанные на OneScript
            foreach (System.Collections.DictionaryEntry cm in managerFiles)
            {
                ICodeSource src = _hostedScript.Loader.FromString(InsertMandatoryMethods((string)cm.Value, (string)cm.Key));
                var compilerService = _hostedScript.GetCompilerService();
                var module = compilerService.CreateModule(src);
                var _loaded = _hostedScript.EngineInstance.LoadModuleImage(module);
                dataProcessors.Insert((string)cm.Key, (IValue)_hostedScript.EngineInstance.NewObject(_loaded));
            }
            // Добавляем библиотеки как обработки
            AddLibrariesAsDataProcessors(dataProcessors);
            _hostedScript.EngineInstance.Environment.SetGlobalProperty("Обработки", new ScriptEngine.HostedScript.Library.FixedStructureImpl(dataProcessors));

        }

        void AddLibrariesAsDataProcessors(ScriptEngine.HostedScript.Library.StructureImpl dataProcessors)
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;

            foreach (string typeInfo in appSettings.AllKeys)
            {
                string info = typeInfo.Replace(" ", "");
                if (info.StartsWith("attachAsDataProcessor;"))
                {
                    // Запись должна быть вида <key="attachAsDataProcessor;ИмяСборки;ИмяТипа" value="[ИмяОбработки]" />
                    // Если пункта ИмяОбработки нет - получаем из атрибута класса
                    string[] dataProcessorInfo = info.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    System.IO.TextWriter tw = System.IO.File.CreateText("c:\\1\\obr.txt");
                    tw.WriteLine(dataProcessorInfo.Length.ToString());
                    if (dataProcessorInfo.Length !=3)
                        continue;

                    string typeName = dataProcessorInfo[2].Trim();
                    string dataProcessorName = appSettings[typeInfo] ?? "";
                    dataProcessorName = dataProcessorName.Replace(" ", "");
                    string assemblyName = dataProcessorInfo[1].Trim();
                    object instance = Activator.CreateInstance(assemblyName, typeName).Unwrap();
                   
                    tw.WriteLine(typeName);
                    tw.Write(assemblyName);
                    tw.Write(dataProcessorName);
                    tw.Close();
                    if (dataProcessorName == "")
                    {
                        ContextClassAttribute attribute = instance.GetType().GetCustomAttributes(typeof(ContextClassAttribute), false).FirstOrDefault() as ContextClassAttribute;

                        if (attribute != null)
                            dataProcessorName = attribute.GetName();
                        else
                            continue;
                    }

                    dataProcessors.Insert(dataProcessorName, (IValue)instance);
                }
            }
        }
    }

}
