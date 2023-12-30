using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;
using TableRW.Write.I;

namespace TableRW.Write;


public static class TableWriterEx {

    public static ITableWriter<C> InitData<C, TSource, TRow, TEntity, D>(
        this ITableWriter<C, IContext<TSource, TRow, TEntity, D>> writer,
        Func<TSource, D> initData
    ) {
        var w = writer.IntoImpl();
        w.Event.InitDataFn = E.Constant(initData);
        return w;
    }

    public static Expression<Action<TSource, IEnumerable<E>>> Lambda<C, TSource, TRow, E>(
        this ITableWriter<C, IContext<TSource, TRow, E>> writer
    ) => writer.ToBuildExpr().Lambda();


    public static ITableWriter<C> AddColumns<C, TSource, TRow, TEntity>(
        this ITableWriter<C, IContext<TSource, TRow, TEntity>> writer,
        Expression<Func<DParams, TEntity, DParams>> members
    ) {
        var w = writer.IntoImpl();
        var unsupported_msg = $"Unsupported expressions, valid examples: `(s, e) => s(e.Prop1, s.{nameof(DParamsEx.Skip)}(2), e.Prop2 + e.Prop3)`";
        members.GetParams(unsupported_msg).ForEach(e =>
            DParamsEx.IsSkip(e) is { } skip
            ? writer.AddSkipColumn(skip)
            : writer.AddColumnExpr(e.UpdateParameters(members.Parameters[1], w.Ctx.Entity)));
        return w;
    }

    private static ITableWriter<C> AddColumnExpr<C, TSource, TRow, TEntity>(
        this ITableWriter<C, IContext<TSource, TRow, TEntity>> writer,
        Expression value
    ) {
        var w = writer.IntoImpl();
        var setValue = WriteSource<TSource>.WriteSrcValue(w.Ctx.Context, value);
        w.WriteSeq.AddColumn(setValue, false);
        return w;
    }

}
