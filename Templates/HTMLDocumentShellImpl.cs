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


namespace OneScript.HTTPService
{
    [ContextClass("ОболочкаHTMLДокументаWeb", "HTMLDocumentShellImplWeb")]
    public class HTMLDocumentShellImpl : AutoContext<HTMLDocumentShellImpl>
    {
        string _text;

        public HTMLDocumentShellImpl(string fileName)
        {
            _text = System.IO.File.ReadAllText(fileName);
        }

        [ContextMethod("ПолучитьТекст", "GetText")]
        public IValue GetText()
        {
            // Создаем объект из модуля объекта
            return ValueFactory.Create(_text);
        }
    }
}
