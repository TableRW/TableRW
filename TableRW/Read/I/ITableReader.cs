namespace TableRW.Read.I;

public interface ITableReader<out TContext> : ITableReader<TContext, TContext> { }

public interface ITableReader<out TContext, out C> {
    /// <summary>
    /// Set the row and column to start reading
    /// </summary>
    ITableReader<TContext> SetStart(int indexRow, int indexColumn);

    ITableReader<TContext> OnStartReadingRow(Action<TContext> action);

    ITableReader<TContext> OnStartReadingRow(Func<TContext, IterAction?> action);

    ITableReader<TContext> OnEndReadingRow(Action<TContext> action);

    ITableReader<TContext> OnEndReadingRow(Func<TContext, IterAction?> action);

    /// <summary>
    /// Invoke the function when starting to read the table,  <para></para>
    /// return true to continue reading,  <para></para>
    /// return false to end reading
    /// </summary>
    /// <param name="action">
    /// Invoke the function when starting to read the table,  <para></para>
    /// return true to continue reading,  <para></para>
    /// return false to end reading
    /// </param>
    ITableReader<TContext> OnStartReadingTable(Func<TContext, bool> action);

    ITableReader<TContext> OnEndReadingTable(Action<TContext> action);

    // ITableReader<C> AddColumn<TSource>(MemberInfo member);

    ITableReader<TContext> AddSkipColumn(int skip);

    /// <summary>
    /// Call this action at the current position
    /// </summary>
    ITableReader<TContext> AddActionRead(Action<TContext> action);

    /// <summary>
    /// Call this action at the current position
    /// </summary>
    ITableReader<TContext> AddActionRead(Func<TContext, IterAction?> action);

}
