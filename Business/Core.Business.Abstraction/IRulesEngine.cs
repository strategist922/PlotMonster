using System.Collections.Generic;
using System.Threading;

namespace Core.Business.Abstraction
{
    public interface IRulesEngine<T>
    {
        IAsyncEnumerable<T> ProcessAsync(IAsyncEnumerable<T> inputChannel, CancellationToken cancellationToken);
    }
}