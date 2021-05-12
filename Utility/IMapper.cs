namespace chia_plotter.Utiltiy.Abstraction
{
    public interface IMapper<TIn, TOut>
    {
        TOut Map(TIn input);
    }
}