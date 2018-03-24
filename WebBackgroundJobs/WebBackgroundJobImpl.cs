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
using ScriptEngine.HostedScript;
using ScriptEngine;

namespace OneScript.HTTPService
{
    [ContextClass("ФоновоеЗаданиеWeb", "WebBackgroundJob")]
    public class WebBackgroundJobImpl : AutoContext<WebBackgroundJobImpl>
    {
        WebBackgroundJob _job;
        public WebBackgroundJob Job
        {
            get
            {
                return _job;
            }
        }

        public WebBackgroundJobImpl(WebBackgroundJob job)
        {
            _job = job;
        }

        [ContextProperty("ИмяМетода", "MethodName")]
        public string MethodName
        {
            get
            {
                return _job.MethodName;
            }
        }

        [ContextProperty("ИнформацияОбОшибке", "ErrorInfo")]
        public ExceptionInfoContext ErrorInfo
        {
            get
            {
                return _job.ErrorInfo;
            }
        }

        [ContextProperty("Ключ", "Key")]
        public string Key
        {
            get
            {
                return _job.Key;
            }
        }

        [ContextProperty("Конец", "End")]
        public DateTime? End
        {
            get
            {
                return _job.End;
            }
        }

        [ContextProperty("Наименование", "Description")]
        public string Description
        {
            get
            {
                return _job.Description;
            }
        }

        [ContextProperty("Начало", "Begin")]
        public DateTime? Begin
        {
            get
            {
                return _job.Begin;
            }
        }

        [ContextProperty("РазделениеДанных", "DataSeparation")]
        public StructureImpl DataSeparation
        {
            get
            {
                return new StructureImpl();
            }
        }

        [ContextProperty("РегламентноеЗадание", "ScheduledJob")]
        public IValue ScheduledJob
        {
            get
            {
                return ValueFactory.Create();
            }
        }

        [ContextProperty("Состояние", "State")]
        public WebBackgroundJobStateImpl State
        {
            get
            {
                return (WebBackgroundJobStateImpl)_job.State;
            }
        }

        [ContextProperty("УникальныйИдентификатор", "UUID")]
        public GuidWrapper UUID
        {
            get
            {
                return new GuidWrapper(_job.UUID.ToString());
            }
        }

        [ContextMethod("ОжидатьЗавершения", "WaitForCompletion")]
        public void WaitForCompletion(int? timeout = null)
        {

            int delta = WebBackgroundJobsManager.CheckInterval;
            long timeoutMs = 1000;

            if (timeout == null)
                delta = 0;
            else
                timeoutMs = (long)(timeout * 1000);

            long current = 0;

            do
            {
                System.Threading.Thread.Sleep(WebBackgroundJobsManager.CheckInterval);
                current += delta;

            } while (current < timeoutMs && State == WebBackgroundJobStateImpl.Active);

            System.IO.TextWriter logWriter;

            if (State == WebBackgroundJobStateImpl.Failed)
            {
                logWriter = AspNetLog.Open();
                string logStr = ErrorInfo.ModuleName + "`n"
                    + ErrorInfo.LineNumber + "`n"
                    + ErrorInfo.Description + "`n"
                    + ErrorInfo.DetailedDescription;
                AspNetLog.Write(logWriter, logStr);
                AspNetLog.Close(logWriter);

                throw (new Exception("Фоновое задание завершились с ошибкой."));
            }

            if (State == WebBackgroundJobStateImpl.Completed)
                return;

            string exceptionString = "Timeout expires for job: ";

            exceptionString += "start date: " + Begin.ToString() + " ";
            exceptionString += "method: " + MethodName + " ";

            if (Description != null)
                exceptionString += "description: " + Description + " ";
            if (Key != null)
                exceptionString += "key: " + Description + " ";

            logWriter = AspNetLog.Open();
            AspNetLog.Write(logWriter, exceptionString);
            AspNetLog.Close(logWriter);

            throw (new Exception(exceptionString));
        }

        [ContextMethod("Отменить", "Cancel")]
        public void Cancel()
        {
        }

        [ContextMethod("ПолучитьСообщенияПользователю", "GetUserMessages")]
        public FixedArrayImpl GetUserMessages(bool deleteRecieved)
        {
            return new FixedArrayImpl(new ArrayImpl());
        }
    }

    [EnumerationType("СостояниеФоновогоЗадания", "BackgroundJobState")]
    public enum WebBackgroundJobStateImpl
    {
        [EnumItem("Активно")]
        Active,
        [EnumItem("Завершено")]
        Completed,
        [EnumItem("ЗавершеноАварийно")]
        Failed,
        [EnumItem("Отменено")]
        Cancelled
    }
}
