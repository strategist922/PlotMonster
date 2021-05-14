using System;
using Core.Utiltiy.Abstraction;

namespace Core.Utiltiy.Implementation
{
    public class Mapper<TIn, TOut>: IMapper<TIn, TOut>
    {
        private readonly Func<TIn, TOut> mapDelegate;
        public Mapper(
            Func<TIn, TOut> mapDelegate
        )
        {
            this.mapDelegate = mapDelegate;
        }

        public TOut Map(TIn input)
        {
            return mapDelegate.Invoke(input);
        }
    }
}