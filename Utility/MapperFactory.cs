using System;
using System.Collections.Generic;
using System.Linq;
using chia_plotter.Utiltiy.Abstraction;

namespace chia_plotter.Utiltiy.Implementation
{
    public class MapperFactory<TLeft, TRight, TOut>
    {
        private readonly IEnumerable<IMapper<TLeft, TRight, TOut>> mappers;
        private readonly Func<IMapper<TLeft, TRight, TOut>, bool> decisionDelegate;
        public MapperFactory(
            IEnumerable<IMapper<TLeft, TRight, TOut>> mappers,
            Func<IMapper<TLeft, TRight, TOut>, bool> decisionDelegate)
        {
            this.mappers = mappers;
            this.decisionDelegate = decisionDelegate;
        }

        public IMapper<TLeft, TRight, TOut> CreateInstance()
        {
            return mappers.Where(decisionDelegate.Invoke).First();
        }
    }
}