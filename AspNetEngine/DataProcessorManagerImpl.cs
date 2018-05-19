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
    [ContextClass("_ОбработкаМенеджерФункцииПлатформы", "_DataProcessorManagerPlatformFunction")]
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

        public const string MandatoryMethodsText =
@"// 
Функция Создать() Экспорт
    Возврат ОбработкаМенеджерФункцииПлатформы.Создать(""{{DataProcessorName}}"");
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
                ICodeSource src = _hostedScript.Loader.FromString( InsertMandatoryMethods((string)cm.Value, (string)cm.Key) );
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
                if (appSettings[typeInfo] == "attachAsDataProcessor")
                {
                    // Запись должна быть вида <key="ИмяСборки;ИмяТипа;[ИмяОбработки]"
                    // Если пункта ИмяОбработки нет - получаем из атрибута класса
                    string[] dataProcessorInfo = typeInfo.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                    if (dataProcessorInfo.Length < 2)
                        continue;

                    string typeName = dataProcessorInfo[1].Trim();
                    string dataProcessorName = "";
                    string assemblyName = dataProcessorInfo[0].Trim();
                    object instance = Activator.CreateInstance(assemblyName, typeName).Unwrap();

                    if (dataProcessorInfo.Length == 2)
                    {
                        ContextClassAttribute attribute = instance.GetType().GetCustomAttributes(typeof(ContextClassAttribute), false).FirstOrDefault() as ContextClassAttribute;

                        if (attribute != null)
                            dataProcessorName = attribute.GetName();
                        else
                            continue;
                    }
                    else
                        dataProcessorName = dataProcessorInfo[2].Trim();

                    dataProcessors.Insert(dataProcessorName, (IValue)instance);
                }
            }
        }
    }

}
