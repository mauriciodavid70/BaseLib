using System;
using System.Collections.Generic;

namespace BaseLib.Core.Services
{
    public class CoreServiceState : ICoreServiceState
    {
        private readonly IDictionary<string, object> environment;

        public CoreServiceState(IDictionary<string, object> environment = null)
        {
            this.environment = environment ?? new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public DateTimeOffset StartedOn => this.Get<DateTimeOffset>("StartedOn");
       
        public DateTimeOffset FinishedOn => this.Get<DateTimeOffset>("FinishedOn");

        public string OperationId => this.Get<string>("OperationId");

        public string CorrelationId => this.Get<string>("CorrelationId");

        public ICoreServiceRequest Request => this.Get<ICoreServiceRequest>("Request");

        public ICoreServiceResponse Response => this.Get<ICoreServiceResponse>("Response");

        public virtual T Get<T>(string key)
        {
            return environment.TryGetValue(key, out object value) ? (T)value : default(T);
        }
        
        public virtual ICoreServiceState Set<T>(string key, T value)
        {
            this.environment[key] = value;
            return this;
        }
    }

}