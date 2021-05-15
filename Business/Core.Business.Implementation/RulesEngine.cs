using System;
using System.Collections.Generic;
using System.Threading;
using Core.Business.Abstraction;
using System.Linq.Async;
using System.Threading.Tasks;
using System.Linq;

namespace Core.Business.Implementation
{
    public class RulesEngine<TIn, TOut>: IRulesEngine<TIn, TOut>
    {
        private readonly IEnumerable<Func<IEnumerable<TIn>, bool>> rules;
        public RulesEngine(
            IEnumerable<Func<IEnumerable<TIn>, bool>> rules
        )
        {
            this.rules = rules;
        }
        public Task<TOut> ProcessAsync(IEnumerable<TIn> input, CancellationToken cancellationToken)
        {
            // can add optimization by returning the previous value 
                var passedRules = rules.Where(r => r.Invoke(input)).FirstOrDefault(default(TOut));
        }
    }
}