namespace LosslessCodecBenchmarks;

public static class EnumerableExtensions
{
    // Thanks Eric https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences) 
    { 
        IEnumerable<IEnumerable<T>> emptyProduct = 
            new[] { Enumerable.Empty<T>() }; 
        return sequences.Aggregate( 
            emptyProduct, 
            (accumulator, sequence) => 
                from accumulatorSequence in accumulator 
                from item in sequence 
                select accumulatorSequence.Concat(new[] {item}));
    }
}
