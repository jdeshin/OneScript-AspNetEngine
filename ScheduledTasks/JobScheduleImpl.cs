﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine;
using ScriptEngine.Machine;
using ScriptEngine.Environment;
using ScriptEngine.HostedScript;

using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library;
using ScriptEngine.HostedScript.Library.Binary;

namespace OneScript.HTTPService
{
    [ContextClass("РасписаниеРегламентногоЗадания", "JobSchedule")]
    public class JobScheduleImpl : AutoContext<JobScheduleImpl>
    {
        //Тип: Дата.
        //Время, после которого задание будет принудительно завершено.
        [ContextProperty("ВремяЗавершения", "CompletionTime")]
        public IValue CompletionTime
        {
            get;
            set;
        }
        
        //Тип: Дата.
        //Время конца расписания.Регламентные задания могут быть запущены только в том случае, если текущее время меньше или равно времени конца.Если время конца не задано, срок запуска неограничен.
        //В случае, когда ВремяНачала расписания больше ВремяКонца, выполняется переход через сутки, т.е.расписание будет выполняться только тогда, когда текущее время больше ВремяНачала, но меньше ВремяКонца следующего дня.
        //Пример:
        //ВремяНачала ВремяКонца Комментарий 8:00 – Работает с 8:00 до 24:00 – 8:00    Работает с 24:00 до 8:0013:00         14:00    Работает с 13:00 до 14:0014:00         13:00    Работает с 14:00 до 13:00 следующего дня, то есть работает всегда, кроме интервала с 13:00 до 14:00 
        [ContextProperty("ВремяКонца", "EndTime")]
        public IValue EndTime
        {
            get;
            set;
        }

        //Тип: Дата.
        //Дата конца расписания.Регламентные задания могут быть запущены только в том случае, если текущая дата меньше или равна дате конца.Если дата конца не задана, срок запуска неограничен.
        [ContextProperty("ВремяНачала", "BeginTime")]
        public IValue BeginTime
        {
            get;
            set;
        }

        [ContextProperty("ДатаКонца", "EndDate")]
        public IValue EndDate
        {
            get;
            set;
        }

        [ContextProperty("ДатаНачала", "BeginDate")]
        public IValue BeginDate
        {
            get;
            set;
        }

        [ContextProperty("DayInMonth", "DayInMonth")]
        public IValue ДеньВМесяце
        {
            get;
            set;
        }

        [ContextProperty("ДеньНеделиВМесяце", "WeekDayInMonth")]
        public IValue WeekDayInMonth
        {
            get;
            set;
        }
        // Массив
        [ContextProperty("ДетальныеРасписанияДня", "DetailedDailySchedules")]
        public IValue DetailedDailySchedules
        {
            get;
            set;
        }

        // Массив
        [ContextProperty("ДниНедели", "WeekDays")]
        public IValue WeekDays
        {
            get;
            set;
        }

        //Тип: Число.
        //Интервал времени в секундах от начала запуска регламентного задания, после которого задание будет принудительно завершено.
        [ContextProperty("ИнтервалЗавершения", "CompletionInterval")]
        public IValue CompletionInterval
        {
            get;
            set;
        }
        //Тип: Массив.
        //Массив номеров месяцев, по которым задание может быть запущено(январь - 1, февраль - 2 и т.д.).
        [ContextProperty("Месяцы", "Months")]
        public IValue Months
        {
            get;
            set;
        }
        //Тип: Число.
        //Минимальный интервал времени(в секундах) между повторными запусками задания.Интервал считается от времени завершения предыдущего запуска до времени начала последующего.
       [ContextProperty("ПаузаПовтора", "RepeatPause")]
        public IValue RepeatPause
        {
            get;
            set;
        }

        //Тип: Число.
        //Период времени в неделях, через который нужно повторять запуск регламентного задания.
        //1, задание может быть запущено каждую неделю;
        //2 - через неделю;
        //        и т.д.
        [ContextProperty("ПериодНедель", "WeeksPeriod")]
        public IValue WeeksPeriod
        {
            get;
            set;
        }
        
        //Тип: Число.
        //Период времени в секундах, через который нужно запускать задание в течение дня.Если равен 0, задание в течение дня будет запущено однократно(задание может быть запущено только в те дни, которые определяются данным расписанием).
        [ContextProperty("ПериодПовтораВТечениеДня", "RepeatPeriodInDay")]
        public IValue RepeatPeriodInDay
        {
            get;
            set;
        }

        //Тип: Число.
        //Период времени в днях, через который нужно повторять запуск регламентного задания.
        //Если значение свойства равно 0, задание будет запущено в течение только одного дня (задание может быть запущено в течение одного дня несколько раз, в зависимости от ПериодПовтораВТечениеДня). Если ПериодПовтораДней = 1, задание будет запущено каждый день, ПериодПовтораДней = 2, задание будет запущено через день и т.д.
        [ContextProperty("ПериодПовтораДней", "DaysRepeatPeriod")]
        public IValue DaysRepeatPeriod
        {
            get;
            set;
        }

    }
}
