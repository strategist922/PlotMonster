namespace chia_plotter.Utiltiy.Abstraction
{
    public interface IMapper<TLeft, TRight, TOut>
    {
        TOut Map(TLeft input, TRight output);
    }
}