using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Business.Abstraction
{
    public interface IRulesEngine<TIn, TOut>
    {
        TOut Process(TIn input, CancellationToken cancellationToken);
    }
}