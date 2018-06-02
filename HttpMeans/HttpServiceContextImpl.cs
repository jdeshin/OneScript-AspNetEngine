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
    [ContextClass("HTTPСервисКонтекст", "HTTPServiceContext")]
    public class HTTPServiceContextImpl : AutoContext<HTTPServiceContextImpl>
    {
        System.Web.HttpContext _context;
        SessionStateImpl _sessionState;

        public HTTPServiceContextImpl(System.Web.HttpContext context)
        {
            _context = context;
            _sessionState = new SessionStateImpl(_context);
        }

        [ContextProperty("ФизическийПуть", "PhysicalPath")]
        public IValue PhysicalPath
        {
            get
            {
                return ValueFactory.Create(_context.Request.PhysicalPath);
            }
        }

        [ContextProperty("АдресКлиента", "UserHostAddress")]
        public IValue UserHostAddress
        {
            get
            {
                return ValueFactory.Create(_context.Request.UserHostAddress);
            }
        }

        [ContextProperty("Сессия", "Session")]
        public SessionStateImpl Session
        {
            get
            {
                return _sessionState;
            }
        }

    }
}
