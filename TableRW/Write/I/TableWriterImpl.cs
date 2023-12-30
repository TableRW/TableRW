using E = System.Linq.Expressions.Expression;

namespace TableRW.Write.I;

public class WriteOption {
    public int? StartRow { get; set; }
    public int? StartCol { get; set; }
}

public class Event {
    public Expression? InitDataFn { get; set; }
    public Expression? StartWritingTableAction { get; set; }
    public Expression? StartWritingRowAction { get; set; }
    public Expression? EndWritingRowAction { get; set; }
    public Expression? EndWritingTableAction { get; set; }
}

/// <summary>
/// Generic parameters must be one of these four interfaces: <br />
/// <see cref="IContext{TSource, TEntity}"/>, <br />
/// <see cref="IContext{TSource, TEntity, D}"/>, <br />
/// <see cref="ISubContext{TSource, TEntity, P}"/>, <br />
/// <see cref="ISubContext{TSource, TEntity, P, D}"/>, <br />
/// Otherwise the library's internal `<see cref="TableWriterImplEx.IntoImpl"/>` will fail.
/// </summary>
public class TableWriterImpl<C> : ITableWriter<C> {

    public ContextExpr Ctx { get; } = new(typeof(C));

    public WriteOption Opt { get; } = new();

    public Event Event { get; } = new();

    internal WriteSequence WriteSeq { get; } = new();

    public ITableWriter<C> AddSkipColumn(int skip) {
        WriteSeq.AddOffset(skip);
        return this;
    }

    public ITableWriter<C> SetStart(int row, int column) {
        (Opt.StartRow, Opt.StartCol) = (row, column);
        return this;
    }

    public ITableWriter<C> AddAction(Action<C> action) {
        WriteSeq.AddCtxAction(E.Invoke(E.Constant(action), Ctx.Context));
        return this;
    }

    public ITableWriter<C> AddColumn(Action<C> action) {
        WriteSeq.AddColumn(E.Invoke(E.Constant(action), Ctx.Context), true);
        return this;
    }

    public ITableWriter<C> OnStartWritingRow(Action<C> action) {
        Event.StartWritingRowAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }

    public ITableWriter<C> OnEndWritingRow(Action<C> action) {
        Event.EndWritingRowAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }


    public ITableWriter<C> OnStartWritingTable(Action<C> action) {
        Event.StartWritingTableAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }

    public ITableWriter<C> OnEndWritingTable(Action<C> action) {
        Event.EndWritingTableAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }
}

public static class TableWriterImplEx {

    public static TableWriterImpl<C> IntoImpl<C>(this ITableWriter<C, object> writer)
    => writer as TableWriterImpl<C>
    ?? throw new NotSupportedException("Conversion failed: writer as TableWriterImpl<C>");


    internal static BuildExpr<TSrc, TEntity> ToBuildExpr<C, TSrc, TRow, TEntity>(
        this ITableWriter<C, IContext<TSrc, TRow, TEntity>> writer
    ) {
        var w = writer.IntoImpl();
        var data = (w.Ctx, w.Event, w.WriteSeq);

        var (defaultRow, defaultCol) = WriteSource<TSrc>.DefaultStart;
        var start = (w.Opt.StartRow ?? defaultRow, w.Opt.StartCol ?? defaultCol);
        var opt = new BuildTableOption(start) {
            Src = E.Parameter(typeof(TSrc), "src"),
            Enumerable = E.Parameter(typeof(IEnumerable<TEntity>), "collection"),
            InitRow = WriteSource<TSrc>.InitRow?.Invoke(w.Ctx.Context),
        };

        return new(data, opt);
    }


}