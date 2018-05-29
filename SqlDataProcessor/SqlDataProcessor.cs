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

namespace OneScript.SqlDataProcessor
{
    [ContextClass("СоединениеСУБД", "DBConnection")]
    public class DBConnector : AutoContext<DBConnector>
    {
        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            // Создаем объект из модуля объекта

            return new OScriptSql.DBConnector ();
        }
    }

    [ContextClass("ЗапросСУБД", "DBQuery")]
    public class DBQuery : AutoContext<DBQuery>
    {
        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            // Создаем объект из модуля объекта

            return new OScriptSql.Query ();
        }
    }

    [ContextClass("ПараметрыСоединенияСУБД", "DBConnectionProperties")]
    public class DBConnectionProperties : AutoContext<DBConnectionProperties>
    {
        OScriptSql.EnumDBType _dbmsTypes = new OScriptSql.EnumDBType();

        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            // Создаем объект из модуля менеджера
            return new DBConnectionPropertiesObjectModule();
        }

        // Глобальные функции
        [ContextMethod("ПолучитьСтрокуСоединения", "GetConnectionString")]
        public string GetConnectionString(string connectionName)
        {
            return ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }

        [ContextMethod("ПолучитьТипСУБД", "GetDBMSType")]
        public int GetDBMSType(string connectionName)
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            
            switch (appSettings[connectionName].ToLower())
            {
                case "sqlite":
                    return _dbmsTypes.sqlite;
                case "mssqlserver":
                    return _dbmsTypes.MSSQLServer;
                case "mysql":
                    return _dbmsTypes.MySQL;
                case "postgresql":
                    return _dbmsTypes.PostgreSQL;
                default:
                    throw new Exception("Unknown DBMS type: " + appSettings[connectionName]);
            };
        }
    }

    // Пустой класс заглушка модуля объекта
    [ContextClass("ПараметрыСоединенияСУБДМодульОбъекта", "DBConnectionPropertiesObjectModule")]
    public class DBConnectionPropertiesObjectModule : AutoContext<DBConnectionPropertiesObjectModule>
    {
    }


}
