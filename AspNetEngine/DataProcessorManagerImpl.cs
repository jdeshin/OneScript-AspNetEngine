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
    [ContextClass("ОбработкаМенеджер", "DataProcessorManager")]
    class DataProcessorManagerImpl : AutoContext<DataProcessorManagerImpl>
    {
        HostedScriptEngine _hostedScript;
        LoadedModuleHandle _loaded;

        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            return (IValue)_hostedScript.EngineInstance.NewObject(_loaded);
        }

        public DataProcessorManagerImpl(HostedScriptEngine hostedScript, string text)
        {
            ICodeSource src = _hostedScript.Loader.FromString(text);
            var compilerService = _hostedScript.GetCompilerService();
            var module = compilerService.CreateModule(src);
            _loaded = _hostedScript.EngineInstance.LoadModuleImage(module);
        }
    }
}
