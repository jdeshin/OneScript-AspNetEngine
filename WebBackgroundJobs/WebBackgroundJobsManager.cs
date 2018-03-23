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
    public static class WebBackgroundJobsManager
    {
        static System.Collections.Concurrent.ConcurrentDictionary<string, string> jobsKeys = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        static System.Collections.Concurrent.ConcurrentDictionary<Guid, WebBackgroundJob> jobs = new System.Collections.Concurrent.ConcurrentDictionary<Guid, WebBackgroundJob>();

        public static System.Collections.Concurrent.ConcurrentDictionary<Guid, WebBackgroundJob> Jobs
        {
            get
            {
                return jobs;
            }
        }
        public static int CheckInterval { get; set; }

        static WebBackgroundJobsManager()
        {
            System.Collections.Specialized.NameValueCollection appSettings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            try
            {
                CheckInterval = Convert.ToInt32(appSettings["jobsCheckInterval"] ?? "1000");
            }
            catch
            {
                CheckInterval = 1000;
            }
        }

        public static void ExecuteJob(object stateInfo)
        {
            WebBackgroundJob job = (WebBackgroundJob)stateInfo;

            if (job.Key != "" && job.Key != null)
            {
                // Пробуем вставить в таблицу ключей
                //, если вставка неудачна, значит фоновое задание уже выполняется
                if (!jobsKeys.TryAdd(job.Key, job.Key))
                {
                    // Такое значение уже есть в списке, не запускаем задание?
                    throw new RuntimeException("Фоновое задание с таким значением ключа уже выполняется");
                }
            }

            // Заполняем значения работы и вставляем ее в список
            jobs.TryAdd(job.UUID, job);
            job.Begin = DateTime.Now;
            AspNetHostEngine engine = null;

            try
            {

                if (!AspNetHostEngine.Pool.TryDequeue(out engine))
                    throw new RuntimeException("cannot deque engine");

                engine.CallCommonModuleProcedure(job.MethodName, job.ExecutionParameters);
                job.State = BackgroundJobState.Completed;
                job.ExecutionParameters = null;
            }
            catch (ScriptEngine.ScriptException ex)
            {
                job.ErrorInfo = new ExceptionInfoContext(ex);
                job.State = BackgroundJobState.Failed;

                System.IO.TextWriter logWriter = AspNetLog.Open();
                AspNetLog.Write(logWriter, "Error executing background job ");
                AspNetLog.Write(logWriter, ex.ToString());
                AspNetLog.Close(logWriter);
            }
            catch (Exception ex)
            {
                job.State = BackgroundJobState.Failed;

                System.IO.TextWriter logWriter = AspNetLog.Open();
                AspNetLog.Write(logWriter, "Error executing background job ");
                AspNetLog.Write(logWriter, ex.ToString());
                AspNetLog.Close(logWriter);
            }
            finally
            {
                job.End = DateTime.Now;
                if (engine != null)
                    AspNetHostEngine.Pool.Enqueue(engine);

                try
                {
                    WebBackgroundJob outjob;
                    jobs.TryRemove(job.UUID, out outjob);
                }
                catch {  /* Ничего не делаем*/}

                try
                {
                    string outStr;
                    if (job.Key != null && job.Key != "")
                        jobsKeys.TryRemove(job.Key, out outStr);
                }
                catch {  /* Ничего не делаем*/}
            }
        }
    }
}
