// Copyright (c) Yury Deshin 2018
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine.Environment;

namespace OneScript.HttpServices
{
    // Класс заглушка стартового скрипта. У нас нет стартового скрипта, поскольку это веб-приложение
    class NullEntryScriptSrc : ICodeSource
    {
        public string Code
        {
            get { return ""; }
        }

        public string SourceDescription
        {
            get { return ""; }
        }
    }
}
