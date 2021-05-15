using System;
using System.Collections.Generic;
using System.Threading;
using Core.Business.Abstraction;
using System.Linq.Async;

namespace Core.Business.Implementation
{
    public class RulesEngine<T>: IRulesEngine<T>
    {
        private readonly IEnumerable<Func<T, bool>> rules;
        public RulesEngine(
            IEnumerable<Func<T, bool>> rules
        )
        {
            this.rules = rules;
        }
        public IAsyncEnumerable<T> ProcessAsync(IAsyncEnumerable<T> inputChannel, CancellationToken cancellationToken)
        {
            // can add optimization by returning the previous value 
            return inputChannel.Select(i => 
            {
                return rules.Where(r => r.Invoke(i)).FirstOrDefault(default(T));
            });
        }
    }
}