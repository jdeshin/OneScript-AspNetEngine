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

namespace OneScript.DataProcessor
{
    // Это класс менеджера обработки
    [ContextClass("ТестоваяОбработка", "TestDataProcessor")]
    public class Demo : AutoContext<Demo>
    {
        public Demo()
        {

        }

        // Метод платформы
        [ContextMethod("Создать", "Create")]
        public IValue Create()
        {
            // Создаем объект из модуля объекта

            return new DemoImpl();
        }

        // Статический метод модуля менеджера
        [ContextMethod("ПолучитьТоЖеСамоеЧисло", "GetTheSameDigit")]
        public int GetTheSameDigit(int digit)
        {
            return digit;
        }


    }

    // Это класс модуля объекта обработки
    [ContextClass("ТестоваяОбработкаОбъект", "TestDataProcessorObject")]
    public class DemoImpl : AutoContext<DemoImpl>
    {
        public DemoImpl()
        {

        }

        [ContextProperty("Свойство", "Property")]
        public int Property
        {
            get;
            set;
        }

        [ContextMethod("Сложить", "Add")]
        public int Add(int number1, int number2)
        {
            return number1 + number2;
        }
    }
}
