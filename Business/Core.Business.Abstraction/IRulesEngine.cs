namespace Core.Business.Abstraction
{
    public interface IRulesEngine
    {
        IAsyncEnumerable<bool> ProcessAsync(IAsyncEnumerable<T> inputChannel, CancellationToken cancellationToken);
    }
}