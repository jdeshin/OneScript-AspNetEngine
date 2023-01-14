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

using System.Web;

/// <summary>
/// 
/// </summary>

namespace OneScript.HTTPService
{
    [ContextClass("ПараметрыСессииHTTP", "HTTPSessionState")]
    public class SessionStateImpl : AutoContext<SessionStateImpl>
    {
        public SessionStateImpl()
        {
        }

        [ContextProperty("Количество", "Count")]
        public IValue Count
        {
            get
            {
                return ValueFactory.Create(System.Web.HttpContext.Current.Session.Count);
            }
        }

        [ContextProperty("ЭтоНоваяСессия", "IsNewSession")]
        public bool IsNewSession
        {
            get
            {
                return HttpContext.Current.Session.IsNewSession;
            }
        }

        [ContextProperty("Идентификатор", "SessionID")]
        public string SessionID
        {
            get
            {
                return HttpContext.Current.Session.SessionID;
            }
        }

        [ContextMethod("Получить", "Get")]
        public IValue Get(string name)
        {
            return (IValue)HttpContext.Current.Session[name];
        }
        
        [ContextMethod("Вставить", "Insert")]
        public void Insert(string name, IValue value)
        {
            HttpContext.Current.Session[name] = (object)value;
        }

        [ContextMethod("Очистить", "Clear")]
        public void Clear()
        {
            HttpContext.Current.Session.Clear();
        }

        [ContextMethod("Удалить", "Delete")]
        public void Delete(string name)
        {
            HttpContext.Current.Session.Remove(name);
        }

        [ContextMethod("Получить", "Get")]
        public IValue Get(int index)
        {
            return (IValue)HttpContext.Current.Session[index];
        }
    }
}
