using E = System.Linq.Expressions.Expression;

namespace TableRW.Read.I;

public static class ReadSource<TSource> {
    internal static Func<Expression, Type, Expression> ReadSrcValue = null!;
    internal static Expression<Func<TSource, int, bool>> IsEndTable = null!;
    internal static Expression<Func<TSource, int, int, bool>> IsNullValue = null!;

    static (int row, int col) DefaultStart = (0, 0);
    internal static (Expression row, Expression col) GetDefaultStart()
        => (E.Constant(DefaultStart.row), E.Constant(DefaultStart.col));

    public static void Impl(
        Func<Expression, Type, Expression> readSrcValue,
        Expression<Func<TSource, int, bool>> isEndTable,
        Expression<Func<TSource, int, int, bool>> isNullValue
    ) => (ReadSrcValue, IsEndTable, IsNullValue)
       = (readSrcValue, isEndTable, isNullValue);

    public static void SetDefaultStart(int row, int column)
        => DefaultStart = (row, column);

    // public static void SetReadSrcValue(Func<Expression, Type, Expression> readSrcValue)
    //     => ReadSrcValue = readSrcValue ?? throw new NotSupportedException("Should not be set to null");
}


