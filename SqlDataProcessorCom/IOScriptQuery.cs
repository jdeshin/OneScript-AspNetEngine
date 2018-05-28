using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OScriptSql.Com
{
    //interface IOScriptQuery : IValue
    interface IOScriptQuery
    {
        // props
        //StructureImpl Parameters { get; }
        System.Collections.Hashtable Parameters { get; }
        string Text { get; set; }

        // methods
        //IValue Execute();
        QueryResult Execute();
        //void SetParameter(string ParametrName, IValue ParametrValue);
        void SetParameter(string ParametrName, object ParametrValue, bool isBinary = false);

        // my methods
        void SetConnection(DBConnector connector);


    }
}
