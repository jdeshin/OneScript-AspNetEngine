// Copyright (c) Yury Deshin 2018
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine;

namespace OneScript.HTTPService
{
    public class WebBackgroundJob
    {
        public string MethodName { get; set; }

        public ExceptionInfoContext ErrorInfo { get; set; }

        public string Key { get; set; }

        public DateTime? End { get; set; }

        public string Description { get; set; }

        public DateTime? Begin { get; set; }

        public string Location { get; set; }

        // BackgroundJobState
        public BackgroundJobState State { get; set; }

        public Guid UUID { get; set; }

        public IValue[] ExecutionParameters
        {
            get;
            set;
        }

        public WebBackgroundJob()
        {
            UUID = Guid.NewGuid();
            State = BackgroundJobState.Active;
            Begin = DateTime.Now;
            Key = "";
            Description = "";
            Location = "";
        }
    }

    public enum BackgroundJobState
    {
        Active,
        Completed,
        Failed,
        Cancelled
    }

}

