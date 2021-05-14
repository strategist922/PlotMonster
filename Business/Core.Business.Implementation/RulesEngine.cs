namespace Core.Business.Implementation
{
    public class RulesEngine<T>: IRulesEngine<T>
    {
        private readonly IEnumerable<Func<T, bool>> rules;
        public RulesEngine(
            IEnumerable<Func<T, bool>> rules
        )
        {
            rules = rules;
        }
        public IAsyncEnumerable<T> ProcessAsync(IAsyncEnumerable<T> inputChannel, CancellationToken cancellationToken)
        {
            // can add optimization by returning the previous value 
            return inputChannel.SelectAsync(i => 
            {
                return rules.Where(r => r.Invoke(i)).FirstOrDefault(default(T));
            });
        }
    }
}