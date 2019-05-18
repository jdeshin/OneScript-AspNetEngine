// Copyright (c) Yury Deshin 2018
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library;


namespace OneScript.HTTPService
{
    [ContextClass("МенеджерФоновыхЗаданийWeb", "WebBackgroundJobsManager")]
    public class WebBackgroundJobsManagerImpl : AutoContext<WebBackgroundJobsManagerImpl>
    {
        [ScriptConstructor(Name = "Без параметров")]
        public static IRuntimeContextInstance Constructor()
        {
            return new WebBackgroundJobsManagerImpl();
        }

        [ContextMethod("Выполнить", "Execute")]
        public WebBackgroundJobImpl Execute(string methodName, ArrayImpl parameters, string key = "", string description = "")
        {
            WebBackgroundJob job = new WebBackgroundJob();
            WebBackgroundJobImpl jobImpl = new WebBackgroundJobImpl(job);

            if (key != null)
                job.Key = key;

            if (description != null)
                job.Description = description;

            job.ExecutionParameters = new IValue[0];

            if (parameters != null)
            {
                job.ExecutionParameters = new IValue[parameters.Count()];
                int index = 0;

                foreach (IValue cv in parameters)
                {
                    job.ExecutionParameters[index] = cv;
                    index++;
                }
            }

            job.MethodName = methodName;

            ThreadPool.QueueUserWorkItem(new WaitCallback(WebBackgroundJobsManager.ExecuteJob), job);

            return jobImpl;
        }

        [ContextMethod("ОжидатьЗавершения", "WaitForCompletion")]
        public void WaitForCompletion(ArrayImpl backgroundJobs, int? timeout = null)
        {
            int delta = WebBackgroundJobsManager.CheckInterval;
            long timeoutMs = 1000;

            if (timeout == null)
                delta = 0;
            else
                timeoutMs = (long)(timeout * 1000);

            long current = 0;
            WebBackgroundJobImpl failedJob = null;
            WebBackgroundJobImpl notCompletedJob = null;

            do
            {
                System.Threading.Thread.Sleep(WebBackgroundJobsManager.CheckInterval);
                current += delta;

                notCompletedJob = null;

                foreach (IValue cj in backgroundJobs)
                {
                    if (((WebBackgroundJobImpl)cj).State == WebBackgroundJobStateImpl.Active)
                        notCompletedJob = (WebBackgroundJobImpl)cj;
                    if (((WebBackgroundJobImpl)cj).State == WebBackgroundJobStateImpl.Failed)
                    {
                        failedJob = (WebBackgroundJobImpl)cj;
                        break;
                    }
                }

            } while (current < timeoutMs && notCompletedJob != null && failedJob == null);

            System.IO.TextWriter logWriter;

            if (failedJob != null)
            {
                logWriter = AspNetLog.Open();
                string logStr = failedJob.ErrorInfo.ModuleName + "`n"
                    + failedJob.ErrorInfo.LineNumber + "`n"
                    + failedJob.ErrorInfo.Description + "`n"
                    + failedJob.ErrorInfo.DetailedDescription;
                AspNetLog.Write(logWriter, logStr);
                AspNetLog.Close(logWriter);

                throw (new Exception("Одно или несколько фоновых заданий завершились с ошибкой."));
            }

            if (notCompletedJob == null)
                return;

            string exceptionString = "Timeout expires for job: ";

            exceptionString += "start date: " + notCompletedJob.Begin.ToString() + " ";
            exceptionString += "method: " + notCompletedJob.MethodName + " ";

            if (notCompletedJob.Description != null)
                exceptionString += "description: " + notCompletedJob.Description + " ";
            if (notCompletedJob.Key != null)
                exceptionString += "key: " + notCompletedJob.Description + " ";

            logWriter = AspNetLog.Open();
            AspNetLog.Write(logWriter, exceptionString);
            AspNetLog.Close(logWriter);

            throw (new Exception(exceptionString));
        }

        [ContextMethod("ПолучитьФоновыеЗадания", "GetBackgroundJobs")]
        public ArrayImpl GetBackgroundJobs(StructureImpl filter = null)
        {
            if (filter == null || filter == ValueFactory.Create())
                return GetAllJobs();

            ArrayImpl result = new ArrayImpl();

            foreach (System.Collections.Generic.KeyValuePair<Guid, WebBackgroundJob> ci in WebBackgroundJobsManager.Jobs)
            {
                bool shouldAdd = true;

                foreach (KeyAndValueImpl cfi in filter)
                {
                    if (
                           (cfi.Key.AsString().ToLower() == "ключ" && cfi.Value.AsString() != ci.Value.Key)
                        || (cfi.Key.AsString().ToLower() == "наименование" && cfi.Value.AsString() != ci.Value.Description)
                        || (cfi.Key.AsString().ToLower() == "имяметода" && cfi.Value.AsString() != ci.Value.MethodName)
                        || (cfi.Key.AsString().ToLower() == "начало" && cfi.Value.AsDate() != ci.Value.Begin)
                        || (cfi.Key.AsString().ToLower() == "конец" && cfi.Value.AsDate() != ci.Value.End)
                        || (cfi.Key.AsString().ToLower() == "состояние" && cfi.Value.AsNumber() != (int)ci.Value.State)
                        || (cfi.Key.AsString().ToLower() == "регламентноезадание")
                        || (cfi.Key.AsString().ToLower() == "уникальныйидентификатор" && ((GuidWrapper)cfi.Value).AsString() != ci.Value.UUID.ToString())
                       )
                    {
                        shouldAdd = false;
                        break;
                    }
                }

                if (shouldAdd)
                    result.Add(new WebBackgroundJobImpl(ci.Value));
            }

            return result;
        }

        [ContextMethod("НайтиПоУникальномуИдентификатору", "FindByUUID")]
        public IValue FindByUUID(GuidWrapper UUID)
        {
            System.Guid findKey = new System.Guid(UUID.AsString());

            foreach (System.Collections.Generic.KeyValuePair<Guid, WebBackgroundJob> ci in WebBackgroundJobsManager.Jobs)
            {
                if (ci.Key == findKey)
                {
                    return new WebBackgroundJobImpl(ci.Value);
                }
            }

            return ValueFactory.Create();
        }

        ArrayImpl GetAllJobs()
        {
            ArrayImpl result = new ArrayImpl();

            foreach (System.Collections.Generic.KeyValuePair<Guid, WebBackgroundJob> ci in WebBackgroundJobsManager.Jobs)
                result.Add(new WebBackgroundJobImpl(ci.Value));

            return result;
        }
    }
}
