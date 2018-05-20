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
    public interface ILibraryAsPropertiesLoader
    {
        List<string> GetPropertiesNamesForInjecting(string info);
        void AssignPropertiesValues(HostedScriptEngine engine);
    }
}
