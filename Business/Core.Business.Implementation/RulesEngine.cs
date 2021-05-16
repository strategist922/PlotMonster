using System;
using System.Collections.Generic;
using System.Threading;
using Core.Business.Abstraction;
using System.Linq;

namespace Core.Business.Implementation
{
    public class RulesEngine<TIn, TOut>: IRulesEngine<TIn, TOut> where TOut: new()
    {
        private readonly IEnumerable<Func<TIn, TOut>> rules;
        public RulesEngine(
            IEnumerable<Func<TIn, TOut>> rules
        )
        {
            this.rules = rules;
        }
        public TOut Process(TIn input, CancellationToken cancellationToken)
        {
            // the rules need to consider all inputs and if any rules pass, that rule needs to return the temp drive that is available
            // this will output that string.
            // can add optimization by returning the previous value 
            return rules.Select(r => r.Invoke(input)).Where(output => output != null).FirstOrDefault(null);
        }
    }
}