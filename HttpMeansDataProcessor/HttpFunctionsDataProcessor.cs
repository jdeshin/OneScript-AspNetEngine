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

    [ContextClass("ФункцииHTTP", "HTTPFunctions")]
    public class HTTPFunctionsDataProcessorImpl : AutoContext<HTTPFunctionsDataProcessorImpl>
    {
        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            // Создаем объект из модуля менеджера
            return new HTTPFunctionsDataProcessorObjectModule();
        }

        // Глобальные функции
        [ContextMethod("ПолучитьФизическийПутьИзВиртуального", "MapPath")]
        public string MapPath(string virtualPath)
        {
            return HttpContext.Current.Server.MapPath(virtualPath);
        }

        [ContextMethod("ПолучитьТекущийКонтекст", "GetCurrentContext")]
        public HTTPContextImpl GetCurrentHTTPContext()
        {
            return new HTTPContextImpl();
        }

    }

    // Пустой класс заглушка модуля объекта
    [ContextClass("ФункцииHTTPМодульОбъекта", "HTTPFunctionsObjectModule")]
    public class HTTPFunctionsDataProcessorObjectModule : AutoContext<HTTPFunctionsDataProcessorObjectModule>
    {
    }


}
