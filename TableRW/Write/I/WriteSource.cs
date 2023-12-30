
namespace TableRW.Write.I;

public static class WriteSource<TSource> {
    internal static Func<Expression, Expression, Expression> WriteSrcValue = null!;
    internal static Func<Expression, Expression[]>? InitRow;
    public static (int row, int col) DefaultStart { get; set; } = (0, 0);

    public static void Impl(
        Func<Expression, Expression, Expression> writeSrcValue,
        Func<Expression, Expression[]>? initRow = null
    ) => (WriteSrcValue, InitRow) = (writeSrcValue, initRow);


}


