﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OScriptSql.Com
{
    /// <summary>
    /// Тип поддерживаемой СУБД
    /// </summary>
    //[ContextClass("ТипСУБД", "DBType")]
    //class EnumDBType: AutoContext<EnumDBType>
    public class EnumDBType
    {
        //[ContextProperty("sqlite", "sqlite")]
        public int sqlite
        {
            get { return 0; }
        }

        //[ContextProperty("MSSQLServer", "MSSQLServer")]
        public int MSSQLServer
        {
            get { return 1; }
        }

        //[ContextProperty("MySQL", "MySQL")]
        public int MySQL
        {
            get { return 2; }
        }

        //[ContextProperty("PostgreSQL", "PostgreSQL")]
        public int PostgreSQL
        {
            get { return 3; }
        }

    }
}
