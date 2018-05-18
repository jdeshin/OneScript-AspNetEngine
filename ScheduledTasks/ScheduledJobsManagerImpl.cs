using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine;
using ScriptEngine.Machine;
using ScriptEngine.Environment;
using ScriptEngine.HostedScript;

using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library;
using ScriptEngine.HostedScript.Library.Binary;


namespace OneScript.HTTPService
{
    [ContextClass("МенеджерФоновыхЗаданий", "ScheduledJobsManager")]
    class ScheduledJobsManagerImpl : AutoContext<ScheduledJobsManagerImpl>
    {
    }
}
