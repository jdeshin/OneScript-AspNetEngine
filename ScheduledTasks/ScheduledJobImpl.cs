using System;
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
    [ContextClass("РегламентноеЗадание", "ScheduledJob")]
    public class ScheduledJobImpl : AutoContext<ScheduledJobImpl>
    {
        //Тип: Строка.
        //Имя пользователя, под которым будет выполняться данное регламентное задание.Если имя пользователя не задано, регламентное задание будет выполняться пользователем по умолчанию, имеющим административные права.Чтения и запись для администратора.
        [ContextProperty("UserName", "UserName")]
        public string UserName
        {
            get;
            set;
        }

        [ContextProperty("ИнтервалПовтораПриАварийномЗавершении", "RestartIntervalOnFailure")]
        public int RestartIntervalOnFailure
        {
            get;
            set;
        }

        [ContextProperty("Использование", "Use")]
        public bool Use
        {
            get;
            set;
        }
        //Тип: Строка.
        //Прикладной идентификатор.Для регламентных заданий уникальность ключа не требуется.
        //Ключ используется при запуске фонового задания на основе регламентного задания.
        //В этом случае проверяется уникальность ключа для всех активных фоновых заданий, связанных с регламентными заданиями, имеющими одинаковый объект метаданных.
        //Другими словами, уникальность ключа проверяется в пределах объекта метаданного регламентного задания.
        //Если условие не выполняется задание не запускается.Возможность чтения и записи доступны только для администратора.
        [ContextProperty("Ключ", "Key")]
        public string Key
        {
            get;
            set;
        }
        //Тип: Число.
        //Количество повторов при аварийном завершении задания.
        [ContextProperty("КоличествоПовторовПриАварийномЗавершении", "RestartCountOnFailure")]
        public int RestartCountOnFailure
        {
            get;
            set;
        }

        //Тип: ОбъектМетаданных: РегламентноеЗадание.
        //Содержит метаданные регламентного задания.
        [ContextProperty("Метаданные", "Metadata")]
        public IValue Metadata
        {
            get
            {
                return ValueFactory.Create();
            }
        }

        //Тип: Строка.
        //Наименование регламентного задания.
        [ContextProperty("Наименование", "Description")]
        public string Description
        {
            get;
            set;
        }

        //Тип: Массив.
        //Массив параметров регламентного задания.Количество и состав параметров должны соответствовать параметрам метода регламентного задания.
        [ContextProperty("Параметры", "Parameters")]
        public ArrayImpl Parameters
        {
            get;
            set;
        }

        //Тип: ФоновоеЗадание.
        //Последнее выполнившееся фоновое задание.
        [ContextProperty("ПоследнееЗадание", "LastJob")]
        public WebBackgroundJobImpl LastJob
        {
            get;
        }

        //Тип: Булево.
        //Указывает, является ли регламентное задание предопределенным.
        //Предопределенные регламентные задания определяются в метаданных.Предопределенные регламентные задания можно изменять, но нельзя удалять.Создание и удаление предопределенных регламентных заданий выполняется автоматически при сохранении основной конфигурации в конфигурацию базы данных. 
        //Истина - предопределенное задание.
        [ContextProperty("Предопределенное", "Predefined")]
        public bool Predefined
        {
            get;
        }

        //Тип: Структура.
        //Содержит значения разделителей.Имя элемента структуры содержит имя общего реквизита; значение – значение общего реквизита.
        //Элементами структуры являются значения всех разделителей регламентного задания с типом НезависимоИСовместно.
       [ContextProperty("РазделениеДанных", "DataSeparation")]
        public StructureImpl DataSeparation
        {
            get;
            set;
        }
        
        //Тип: РасписаниеРегламентногоЗадания.
        //Расписание задания.
        [ContextProperty("УникальныйИдентификатор", "UUID")]
        public GuidWrapper UUID
        {
            get;
            set;
        }
    }
}
