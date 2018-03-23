// Copyright (c) Yury Deshin 2018
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine.HostedScript;
using ScriptEngine.HostedScript.Library;


namespace OneScript.HttpServices
{
    // Класс-заглушка для вывода Сообщить.
    // Поведение как на сервере 1С

    public class NullApplicationHost : IHostApplication
    {
        public NullApplicationHost()
        {

        }

        public void Echo(string str, MessageStatusEnum status = MessageStatusEnum.Ordinary)
        {
            // Обработчик Сообщить, в HTTPСервисе ничего не делает, также поступаем и мы
        }

        public void ShowExceptionInfo(Exception exc)
        {
            // Непонятно что это, наверное аналог системного диалога, на сервере нет никаких диалогов
        }

        public bool InputString(out string result, int maxLen)
        {
            // Мы не можем вводить никаких строк на сервере в 1С это недоступно
            result = null;
            return false;
        }

        public string[] GetCommandLineArguments()
        {
            // У нас нет никаких аргументов командной строки
            return new string[0]; // возвращаем массив из 0 аргументов т.к у нас их нет
        }

    }
}
