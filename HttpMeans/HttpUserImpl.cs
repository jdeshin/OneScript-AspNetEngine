using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library;
using ScriptEngine.HostedScript.Library.Binary;

using System.Web;


namespace OneScript.HTTPService
{
    [ContextClass("ПользовательHTTP", "HTTPUser")]
    public class HttpUserImpll : AutoContext<HttpUserImpll>
    {
        public HttpUserImpll()
        {
        }

        [ContextMethod("ИмеетРоль", "IsInRole")]
        public IValue IsInRole(string role)
        {
            return ValueFactory.Create(HttpContext.Current.User.IsInRole(role));
        }

        [ContextProperty("Имя", "Name")]
        public string IsNewSession
        {
            get
            {
                return HttpContext.Current.User.Identity.Name;
            }
        }

        [ContextProperty("ПрошелПроверкуПодлинности", "IsAuthenticated")]
        public IValue IsAuthenticated
        {
            get
            {
                return ValueFactory.Create(HttpContext.Current.User.Identity.IsAuthenticated);
            }
        }

        [ContextProperty("ТипПроверкиПодлинности", "AuthenticationType")]
        public string AuthenticationType
        {
            get
            {
                return HttpContext.Current.User.Identity.AuthenticationType;
            }
        }
    }
}
