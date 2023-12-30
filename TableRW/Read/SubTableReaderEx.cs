using TableRW.Read.I;
using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Read;


public static class SubTableReaderEx {


    internal static ITableReader<C> AddRowKey<C, TSrc>(
        this ITableReader<C, ISource<TSrc>> reader, MemberInfo rowKey
    ) {
        var r = reader.IntoImpl();
        var type = r.CheckMemberType(rowKey);
        r.Opt.RowKey = (rowKey, E.Variable(type, "rowKey" + r.Ctx.DeepNo));
        reader.AddColumn(rowKey);
        return r;
    }

    // 可以不考虑开放
    internal static ITableReader<C> AddRowKey<C, TSrc, TEntity, TKey>(
        this ITableReader<C, IContext<TSrc, TEntity>> reader,
        Expression<Func<TEntity, TKey>> rowKey
    ) {
        var r = reader.IntoImpl();
        var member = r.GetEntityMember(rowKey);

        return reader.AddRowKey(member);
    }

    private static ITableReader<C> AddSubTableImpl<C, SubC, TSrc, TSubEntity>(
        this ITableReader<C, object> reader,
        LambdaExpression subProp,
        ITableReader<SubC, IContext<TSrc, TSubEntity>> sub
    ) {
        var r = reader.IntoImpl();
        var subReader = sub.IntoImpl();
        var propMember = r.GetEntityMember(subProp);
        var propType = r.CheckMemberType(propMember);
        var readRowKeyFn = GetReadRowKeyFn();
        var (rowKeyMember, rowKey) = r.Opt.RowKey!.Value;

        subReader.Ctx.InitData = r.Ctx.Data;
        r.ReadSeq.AddColumnRead(propMember, GetSubCollection());
        return r; //------------------------------------------------------

        PropReadFn GetReadRowKeyFn() {
            if (r.Opt.RowKey is not var (member, rowKey)) {
                throw new NotSupportedException("Please set the `RowKey` of the table.");
            }

            if (r.ReadSeq.Find(member) is not (var index, Expression read)) {
                throw new InvalidOperationException(
                "Please set the read column of `RowKey`, the `RowKey` must be read before reading `SubTable`.");
            }

            return (rootOpt) => {
                var indexCol = r.ReadSeq.GetIndexCol(rootOpt.StartCol, index);
                var readValue = read.ReplaceMemberAccess(r.Ctx.iCol.Member, indexCol);
                return E.Assign(rowKey, readValue);
            };
        }

        PropReadFn GetSubCollection() => (rootOpt) => {
            var opt = GetTableOption(rootOpt);

            if (typeof(List<TSubEntity>) is var t1 && propType.IsAssignableFrom(t1)) {
                opt.Collection = E.Parameter(t1, "collection" + subReader.Ctx.DeepNo);
                // buildOpt.NewCollection = // 选填
                opt.CollectionAdd = opt.Collection.Call("Add", subReader.Ctx.Entity);

                return NewBuildExpr(opt).BuildReadingTable();
            }
            if (propType.IsAssignableFrom(typeof(TSubEntity[]))) {
                opt.Collection = E.Parameter(t1, "collection" + subReader.Ctx.DeepNo);
                // buildOpt.NewCollection = // 选填
                opt.CollectionAdd = opt.Collection.Call("Add", subReader.Ctx.Entity);
                var buildExpr = NewBuildExpr(opt);
                var collection = E.Call(buildExpr.BuildReadingTable(), "ToArray", new Type[] { });

                return collection;
            }
            if (propType.GetInterface("IDictionary`2") is { } t2) {
                var typeDic = typeof(Dictionary<,>).MakeGenericType(t2.GetGenericArguments());
                if (propType.IsAssignableFrom(typeDic)) {
                    throw new InvalidOperationException("Dictionary.Key must be the type of RowKey");
                }
                opt.Collection = E.Parameter(typeDic, "collection" + subReader.Ctx.DeepNo);
                // todo rowKey 作为 Dictionary key 是不对的，考虑使用 subRead.RowKey
                opt.CollectionAdd = opt.Collection.Call("Add", rowKey, subReader.Ctx.Entity);
                var buildExpr = NewBuildExpr(opt);

                throw new NotImplementedException();
                // return buildExpr.BuildReadingTable();
            }
            if (typeof(HashSet<TSubEntity>) is var t3 && propType.IsAssignableFrom(t3)) {
                throw new NotImplementedException();
            }
            throw new NotSupportedException("Constructing this collection is not supported");
        };

        BuildSubTableOption GetTableOption(RootReadOpt rootOpt) {
            var currIndex = E.Constant(subReader.ReadSeq.CurrentIndex());
            var parentRowkey = E.MakeMemberAccess(r.Ctx.Entity, rowKeyMember);
            var readRowKey = readRowKeyFn(rootOpt)
                .ReplaceMemberAccess(r.Ctx.iRow.Member, subReader.Ctx.iRow);
            var lblEndSubRead = E.Label("EndSubRead");

            return new((r.Ctx.iRow, E.Add(rootOpt.StartCol, currIndex))) {
                PerentICol = r.ReadSeq.HasCtxRead ? r.Ctx.iCol : null,
                RowKey = rowKey,
                InitRowkey = E.Assign(rowKey, parentRowkey),
                ReadRowKey = readRowKey,
                IsEnd = E.NotEqual(rowKey, parentRowkey), // rowKey != parentEntity.RowKey
                RootReadOpt = rootOpt,
                LblEndSubRead = lblEndSubRead,
                IsNullRowKey = GetIsNullRowKey(),
            };

            Expression? GetIsNullRowKey() {
                if (subReader.Opt.RowKey is not (var member, _)) { return null; }

                if (subReader.ReadSeq.Find(member) is not (var index, _)) { return null; }

                var indexCol = subReader.ReadSeq.GetIndexCol(rootOpt.StartCol, index);
                var isNullRowKey = ReadSource<TSrc>.IsNullValue
                    .ExtractBody(rootOpt.Src, subReader.Ctx.iRow, indexCol);

                subReader.Ctx.HasIterAction =
                    subReader.Ctx.HasIterAction || isNullRowKey != null;

                return E.IfThen(isNullRowKey, E.Goto(lblEndSubRead));
            }
        }

        BuildExprSubTable NewBuildExpr(BuildSubTableOption opt) {
            opt.NewCollection ??= Utils.Expr.GetNewExpression(opt.Collection.Type);

            return new((subReader.Ctx, subReader.Event, subReader.ReadSeq), opt);
        }

    }

    // IContext<S, E>, ICollection, members
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity>(
        this ITableReader<C, IContext<Src, TEntity>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Expression<Func<DParams, TSubEntity, DParams>> members
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity>>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type, r.Ctx.Entity);
        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        sub.AddColumns(members);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // IContext<S, E>, ICollection, action
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity>(
        this ITableReader<C, IContext<Src, TEntity>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Action<ITableReader<ISubContext<Src, TSubEntity, ParentEntity<TEntity>>>> subTable
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity>>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type, r.Ctx.Entity);
        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        subTable(sub);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // IContext<S, E, TData>, ICollection, action
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity, TData>(
        this ITableReader<C, IContext<Src, TEntity, TData>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Action<ITableReader<ISubContext<Src, TSubEntity, ParentEntity<TEntity>, TData>>> subTable
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity>, TData>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type, r.Ctx.Entity);
        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        subTable(sub);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // ISubContext<S, E, P>, ICollection, members
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity, P>(
        this ITableReader<C, ISubContext<Src, TEntity, P>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Expression<Func<DParams, TSubEntity, DParams>> members
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity, P>>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type,
            r.Ctx.Entity, r.Ctx.Parent!);

        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        sub.AddColumns(members);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // ISubContext<S, E, P>, ICollection, action
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity, P>(
        this ITableReader<C, ISubContext<Src, TEntity, P>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Action<ITableReader<ISubContext<Src, TSubEntity, ParentEntity<TEntity, P>>>> subTable
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity, P>>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type,
            r.Ctx.Entity, r.Ctx.Parent!);

        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        subTable(sub);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // ISubContext<S, E, P, D>, ICollection, members
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity, P, D>(
        this ITableReader<C, ISubContext<Src, TEntity, P, D>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Expression<Func<DParams, TSubEntity, DParams>> members
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity, P>, D>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type,
            r.Ctx.Entity, r.Ctx.Parent!);

        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        sub.AddColumns(members);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // ISubContext<S, E, P, D>, ICollection, action
    public static ITableReader<C> AddSubTable<C, Src, TEntity, TSubEntity, P, D>(
        this ITableReader<C, ISubContext<Src, TEntity, P, D>> reader,
        Expression<Func<TEntity, ICollection<TSubEntity>>> subProp,
        Action<ITableReader<ISubContext<Src, TSubEntity, ParentEntity<TEntity, P>, D>>> subTable
    ) {
        var r = reader.IntoImpl();
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity, P>, D>>();

        sub.Ctx.InitParent = Utils.Expr.GetNewExpression(sub.Ctx.Parent!.Type,
            r.Ctx.Entity, r.Ctx.Parent!);

        sub.ReadSeq.AddOffset(r.ReadSeq.CurrentIndex() + 1);
        subTable(sub);
        return reader.AddSubTableImpl(subProp, sub);
    }

    // IContext<S, E>, IDictionary, action
    // not impl
    private static ITableReader<C> _AddSubTable<C, Src, TEntity, TKey, TSubEntity>(
        this ITableReader<C, IContext<Src, TEntity>> reader,
        Expression<Func<TEntity, IDictionary<TKey, TSubEntity>>> subProp,
        Action<ITableReader<ISubContext<Src, TSubEntity, ParentEntity<TEntity>>>> subTable
    ) {
        var sub = new TableReaderImpl<ISubContext<Src, TSubEntity, ParentEntity<TEntity>>>();
        sub.ReadSeq.AddOffset(reader.IntoImpl().ReadSeq.CurrentIndex() + 1);
        subTable(sub);
        return reader.AddSubTableImpl(subProp, sub);
    }


}