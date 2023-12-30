using E = System.Linq.Expressions.Expression;

namespace TableRW.Read.I;

public class ReadOption {
    public Expression? StartRow { get; set; }
    public Expression? StartCol { get; set; }

    public (MemberInfo member, ParameterExpression value)? RowKey { get; set; }

}

public class Event {
    public Expression? InitDataFn { get; set; }
    public Expression? StartReadingTableFn { get; set; }
    public Expression? StartReadingRowAction { get; set; }
    public Expression? StartReadingRowFn { get; set; }
    public Expression? EndReadingRowAction { get; set; }
    public Expression? EndReadingRowFn { get; set; }
    public Expression? EndReadingTableAction { get; set; }
}

/// <summary>
/// Generic parameters must be one of these four interfaces: <br />
/// <see cref="IContext{TSource, TEntity}"/>, <br />
/// <see cref="IContext{TSource, TEntity, D}"/>, <br />
/// <see cref="ISubContext{TSource, TEntity, P}"/>, <br />
/// <see cref="ISubContext{TSource, TEntity, P, D}"/>, <br />
/// Otherwise the library's internal `<see cref="TableReaderImplEx.IntoImpl"/>` will fail.
/// </summary>
public class TableReaderImpl<C> : ITableReader<C> {

    public ContextExpr Ctx { get; } = new(typeof(C));

    public ReadOption Opt { get; } = new();

    public Event Event { get; } = new();

    internal ReadSequence ReadSeq { get; } = new(); // { iCol = Ctx.iCol };

    public MemberInfo GetEntityMember(LambdaExpression expression) {
        if (expression.Body is not MemberExpression memberE) {
            throw new NotSupportedException("The lambda body must be MemberExpression");
        }
        return memberE.Member;
    }

    public Type CheckMemberType(MemberInfo member) {
        var entityType = Ctx.Entity.Type;
        if (member.DeclaringType != entityType) {
            throw new NotSupportedException($"Must be a member of {entityType.FullName}");
        }

        return member switch {
            PropertyInfo p => p.CanWrite
                ? p.PropertyType
                : throw new NotSupportedException($"Object members({member.Name}) must be writable"),
            FieldInfo f => f.IsInitOnly
                ? throw new NotSupportedException($"Object members({member.Name}) must be writable")
                : f.FieldType,
            _ => throw new NotSupportedException($"Object members({member.Name}) can only be Field or Property")
        };
    }

    // public Expression AssignEntityProp(MemberInfo prop, Expression value) {
    //     return E.Assign(E.MakeMemberAccess(Ctx.Entity, prop), value);
    // }

    // public Expression GetTableReadExpr(Expression<Func<C, bool>> isEndTableRead)
    //     => GetTableReadExpr(Utils.Expr.ExtractBody(isEndTableRead, Ctx.Context));

    // public Expression GetTableReadExpr(Expression isEndTableRead) {
    //     throw new NotImplementedException();
    // }

    // public Expression BuildReadTable(ReadTableOption opt) {
    //     // isEndTableRead

    //     // 可配置 SetStart，常量，参数，默认值，Sub上下文
    //     //[ Expression E_StartRow,
    //     // Expression E_StartCol, ]

    //     // 作为最后的输出
    //     // ParameterExpression E_Collection,
    //     // Expression E_AddToCollection,
    //     // NewExpression? E_NewCollection

    //     throw new NotImplementedException();
    // }


    public ITableReader<C> AddActionRead(Action<C> action) {
        ReadSeq.AddCtxRead(E.Invoke(E.Constant(action), Ctx.Context));
        return this;
    }

    public ITableReader<C> AddActionRead(Func<C, IterAction?> action) {
        ReadSeq.AddCtxRead(
            Ctx.IterActionJump2(E.Invoke(E.Constant(action), Ctx.Context)));
        return this;
    }

    // public ITableReader<C> AddColumn<TSource>(MemberInfo member) {
    //     var type = CheckMemberType(member); // 立即检查
    //     var value = ReadSource<TSource>.ReadSrcValue(Ctx.Context, type);
    //     ReadSeq.AddColumnRead(member, AssignEntityProp(member, value));
    //     return this;
    // }

    public ITableReader<C> AddSkipColumn(int skip) {
        ReadSeq.AddOffset(skip);
        return this;
    }

    // public ITableReader<C> AddRowKey(MemberInfo rowKey) {
    //     var type = CheckMemberType(rowKey);
    //     Opt.RowKey = (rowKey, E.Variable(type, "rowKey"));
    //     return this;
    // }

    public ITableReader<C> SetStart(int row, int column) {
        (Opt.StartRow, Opt.StartCol) = (E.Constant(row), E.Constant(column));
        return this;
    }

    public ITableReader<C> OnStartReadingRow(Action<C> action) {
        Event.StartReadingRowAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }
    public ITableReader<C> OnStartReadingRow(Func<C, IterAction?> action) {
        Event.StartReadingRowFn =
            Ctx.IterActionJump2(E.Invoke(E.Constant(action), Ctx.Context));
        return this;
    }
    public ITableReader<C> OnEndReadingRow(Action<C> action) {
        Event.EndReadingRowAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }


    public ITableReader<C> OnEndReadingRow(Func<C, IterAction?> action) {
        Event.EndReadingRowFn =
            Ctx.IterActionJump_NotEndRow(E.Invoke(E.Constant(action), Ctx.Context));

        return this;
    }

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
    public ITableReader<C> OnStartReadingTable(Func<C, bool> action) {
        Event.StartReadingTableFn =
            E.IfThen(
                E.IsFalse(E.Invoke(E.Constant(action), Ctx.Context)),
                E.Goto(Ctx.LblEndTable));

        return this;
    }

    public ITableReader<C> OnEndReadingTable(Action<C> action) {
        Event.EndReadingTableAction = E.Invoke(E.Constant(action), Ctx.Context);
        return this;
    }
}

public static class TableReaderImplEx {

    public static TableReaderImpl<C> IntoImpl<C>(this ITableReader<C, object> reader)
        => reader as TableReaderImpl<C>
        ?? throw new NotSupportedException("Conversion failed: reader as TableReaderImpl<C>");

    internal static IBuildFunc<C, Func<Src, List<E>>> DefualtBuildFunc<C, Src, E>(
        this ITableReader<C, IContext<Src, E>> reader
    ) {
        var r = reader.IntoImpl();
        var (dRow, dCol) = ReadSource<Src>.GetDefaultStart();
        var start = (r.Opt.StartRow ?? dRow, r.Opt.StartCol ?? dCol);
        var fnOpt = BuildFunc<C, Func<Src, List<E>>>.FromICollection(r.Ctx, start);

        return fnOpt;
    }

    internal static BuildExpr<Fn> ToBuildExpr<C, Src, Entity, Fn>(
        this ITableReader<C, IContext<Src, Entity>> reader,
        Func<IBuildFunc<C, Func<Src, List<Entity>>>, IBuildFunc<C, Fn>> buildFn
    ) {
        return reader.ToBuildExpr(buildFn(reader.DefualtBuildFunc()));
    }

    internal static BuildExpr<Fn> ToBuildExpr<C, Src, E, Fn>(
        this ITableReader<C, IContext<Src, E>> reader,
        IBuildFunc<C, Fn> build
    ) {
        var r = reader.IntoImpl();
        var data = (r.Ctx, r.Event, r.ReadSeq);
        var fnOpt = (BuildFunc<C, Fn>)build;

        fnOpt.IsEnd = Utils.Expr.ExtractBody(
            ReadSource<Src>.IsEndTable, fnOpt.FnParams[0], r.Ctx.iRow);

        fnOpt.RootReadOpt = new() {
            Src = fnOpt.FnParams[0],
            StartCol = fnOpt.StartCol,
            IsEndTable = fnOpt.IsEnd,
        };

        return new(data, fnOpt);
    }

    // internal static BuildExprSubTable ToBuildExprSubTable<C, Src, Entity, Ret>(
    //     this ITableReader<C, IContext<Src, Entity>> reader,
    //     (Expression row, Expression col) start
    // ) {
    //     var r = reader.IntoImpl();
    //     var data = (r.Ctx, r.Event, r.ReadSeq);
    //     var (rowKeyMember, rowKeyValue) = r.CheckRowKey();
    //     var entityKey = E.MakeMemberAccess(r.Ctx.Entity, rowKeyMember);
    //     var isEnd = E.NotEqual(entityKey, rowKeyValue);
    //
    //     return new(data, start, isEnd);
    // }

}