using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine.HostedScript;


namespace OneScript.HTTPService
{
    public class BackgroundJobs : ILibraryAsPropertiesLoader
    {
        public List<string> GetPropertiesNamesForInjecting(string info)
        {
            List<string> propNames = new List<string>();
            propNames.Add("ФоновыеЗадания");
            return propNames;
        }

        public void AssignPropertiesValues(HostedScriptEngine engine)
        {
            engine.EngineInstance.Environment.SetGlobalProperty("ФоновыеЗадания", new WebBackgroundJobsManagerImpl());
        }
    }
}
