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

namespace OneScript.HTTPService
{
    [ContextClass("HTTPКонтекст", "HTTPContext")]
    public class HTTPContextImpl : AutoContext<HTTPContextImpl>
    {
        public HTTPContextImpl()
        {
        }

        [ContextProperty("ФизическийПуть", "PhysicalPath")]
        public IValue PhysicalPath
        {
            get
            {
                return ValueFactory.Create(System.Web.HttpContext.Current.Request.PhysicalPath);
            }
        }

        [ContextProperty("АдресКлиента", "UserHostAddress")]
        public IValue UserHostAddress
        {
            get
            {
                return ValueFactory.Create(System.Web.HttpContext.Current.Request.UserHostAddress);
            }
        }

        [ContextProperty("Сессия", "Session")]
        public SessionStateImpl Session
        {
            get
            {
                return new SessionStateImpl();
            }
        }

        [ContextProperty("Пользователь", "User")]
        public HttpUserImpll User
        {
            get
            {
                return new HttpUserImpll();
            }
        }

        [ContextProperty("Запрос", "Request")]
        public HTTPServiceRequestImpl Request
        {
            get
            {
                return new HTTPServiceRequestImpl(System.Web.HttpContext.Current);
            }
        }
    }
}
