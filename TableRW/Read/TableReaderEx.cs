using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;
using TableRW.Read.I;

namespace TableRW.Read;


public static class TableReaderEx {

    public static ITableReader<C> InitData<C, TSource, TEntity, D>(
        this ITableReader<C, IContext<TSource, TEntity, D>> reader,
        Func<TSource, D> initData
    ) {
        var r = reader.IntoImpl();
        r.Event.InitDataFn = E.Constant(initData);
        return r;
    }

    public static Expression<Func<Src, List<E>>> Lambda<C, Src, E>(
        this ITableReader<C, IContext<Src, E>> reader
    ) => reader.ToBuildExpr(reader.DefualtBuildFunc()).Lambda();

    public static Expression<F> Lambda<C, Src, E, F>(
        this ITableReader<C, IContext<Src, E>> reader,
        Func<IBuildFunc<C, Func<Src, List<E>>>, IBuildFunc<C, F>> buildFn
    ) => reader.ToBuildExpr(buildFn).Lambda();

}


public static class TableReaderEntityEx {

    public static ITableReader<C> AddColumns<C, TSource, TEntity>(
        this ITableReader<C, IContext<TSource, TEntity>> reader,
        Expression<Func<DParams, TEntity, DParams>> members
    ) {
        var r = reader.IntoImpl();
        members.GetEntityMembersWithSkipColumn()
            .ForEach(m =>
                m is MemberInfo m_ ? reader.AddColumn(m_) :
                m is SkipColumn skip ? reader.AddSkipColumn(skip.N) :
                m is RowKey key ? reader.AddRowKey(key.Member) : null);

        return r;
    }

    // 不确定是否需要
    //internal static ITableReader<C> AddColumn<C, TSource, TEntity, TKey>(
    //    this ITableReader<C, IContext<TSource, TEntity>> reader,
    //    Expression<Func<TEntity, TKey>> prop
    //) {
    //    var r = reader.IntoImpl();
    //    reader.AddColumn(r.GetEntityMember(prop));
    //    return r;
    //}

    public static ITableReader<C> AddColumn<C, TSource>(
        this ITableReader<C, ISource<TSource>> reader,
        MemberInfo member
    ) {
        var r = reader.IntoImpl();
        var type = r.CheckMemberType(member); // 立即检查
        var value = ReadSource<TSource>.ReadSrcValue(r.Ctx.Context, type);
        r.ReadSeq.AddColumnRead(member, value);
        return r;
    }

    /// <summary>
    /// Get the value of the column and call this action
    /// </summary>
    /// <typeparam name="TValue">value of the column</typeparam>
    public static ITableReader<C> AddColumnRead<C, TSource, TValue>(
        this ITableReader<C, ISource<TSource>> reader,
        Func<TValue, Func<C, IterAction?>> action
    ) {
        var r = reader.IntoImpl();
        var value = ReadSource<TSource>.ReadSrcValue(r.Ctx.Context, typeof(TValue));
        var read = r.Ctx.IterActionJump2(
            E.Invoke(E.Invoke(E.Constant(action), value), r.Ctx.Context));

        r.ReadSeq.SetHasCtxRead();
        r.ReadSeq.AddColumnRead(null, read);
        return r;
    }

    /// <summary>
    /// Get the value of the column and call this action
    /// </summary>
    /// <typeparam name="TValue">value of the column</typeparam>
    public static ITableReader<C> AddColumnRead<C, TSource, TValue>(
        this ITableReader<C, ISource<TSource>> reader,
        Func<TValue, Action<C>> action
    ) {
        var r = reader.IntoImpl();
        var value = ReadSource<TSource>.ReadSrcValue(r.Ctx.Context, typeof(TValue));
        var read = E.Invoke(E.Invoke(E.Constant(action), value), r.Ctx.Context);

        r.ReadSeq.SetHasCtxRead();
        r.ReadSeq.AddColumnRead(null, read);
        return r;
    }

}
