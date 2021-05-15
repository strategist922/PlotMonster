using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Business.Abstraction
{
    public interface IRulesEngine<TIn, TOut>
    {
        Task<TOut> ProcessAsync(IEnumerable<TIn> inputChannel, CancellationToken cancellationToken);
    }
}