namespace Core.Utiltiy.Abstraction
{
    public interface IMapper<TIn, TOut>
    {
        TOut Map(TIn input);
    }
}