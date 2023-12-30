namespace TableRW.Write.I;

public interface ITableWriter<out TContext> : ITableWriter<TContext, TContext> { }

public interface ITableWriter<out TContext, out C> {
    /// <summary>
    /// Set the row and column to start reading
    /// </summary>
    ITableWriter<TContext> SetStart(int indexRow, int indexColumn);

    // ITableReader<C> AddColumn<TSource>(MemberInfo member);

    ITableWriter<TContext> AddSkipColumn(int skip);

    /// <summary>
    /// Call this action at the current position
    /// </summary>
    ITableWriter<TContext> AddAction(Action<TContext> action);

    /// <summary>
    /// Call this action at the current column
    /// </summary>
    ITableWriter<TContext> AddColumn(Action<TContext> action);

    ITableWriter<C> OnStartWritingRow(Action<C> action);

    ITableWriter<C> OnEndWritingRow(Action<C> action);

    ITableWriter<C> OnStartWritingTable(Action<C> action);

    ITableWriter<C> OnEndWritingTable(Action<C> action);
}
