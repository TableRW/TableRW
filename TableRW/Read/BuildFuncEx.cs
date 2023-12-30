using TableRW.Read.I;

namespace TableRW.Read;

public static class BuildFuncEx {

    private static BuildFunc<C, F> IntoImpl<C, F>(this IBuildFunc<object, F, C> b)
        => (BuildFunc<C, F>)b;

    public static IBuildFunc<C, Func<S, int, R>> StartRow<C, S, R>(
        this IBuildFunc<ISource<S>, Func<S, R>, C> b
    ) => b.IntoImpl().UpdateParams<Func<S, int, R>>();

    public static IBuildFunc<C, Func<S, int, int, R>> Start<C, S, R>(
        this IBuildFunc<ISource<S>, Func<S, R>, C> b
    ) => b.IntoImpl().UpdateParams<Func<S, int, int, R>>();

    public static IBuildFunc<C, Func<Src, (R, Data)>> ReturnData
    <C, Src, E, Data, R>(
        this IBuildFunc<IContext<Src, E, Data>, Func<Src, R>, C> b
    ) => b.IntoImpl().UpdateParams<Func<Src, (R, Data)>>();

    public static IBuildFunc<C, Func<Src, int, (R, Data)>> ReturnData
    <C, Src, E, Data, R>(
        this IBuildFunc<IContext<Src, E, Data>, Func<Src, int, R>, C> b
    ) => b.IntoImpl().UpdateParams<Func<Src, int, (R, Data)>>();

    public static IBuildFunc<C, Func<Src, int, int, (R, Data)>> ReturnData
    <C, Src, E, Data, R>(
        this IBuildFunc<IContext<Src, E, Data>, Func<Src, int, int, R>, C> b
    ) => b.IntoImpl().UpdateParams<Func<Src, int, int, (R, Data)>>();

    public static IBuildFunc<C, Func<Src, List<E>>> ToList<C, Src, E, R>(
        this IBuildFunc<IEntity<E>, Func<Src, R>, C> b
    ) => b.IntoImpl().ToCollection<Func<Src, List<E>>>();

    public static IBuildFunc<C, Func<Src, int, List<E>>> ToList<C, Src, E, R>(
        this IBuildFunc<IEntity<E>, Func<Src, int, R>, C> b
    ) => b.IntoImpl().ToCollection<Func<Src, int, List<E>>>();

    public static IBuildFunc<C, Func<Src, int, int, List<E>>> ToList<C, Src, E, R>(
        this IBuildFunc<IEntity<E>, Func<Src, int, int, R>, C> b
    ) => b.IntoImpl().ToCollection<Func<Src, int, int, List<E>>>();

#if DEBUG // have free time to develop support

    public static IBuildFunc<C, Func<Src, HashSet<E>>> ToHashSet<C, Src, E, R>(
        this IBuildFunc<IEntity<E>, Func<Src, R>, C> b
    ) => b.IntoImpl().ToCollection<Func<Src, HashSet<E>>>();

    public static IBuildFunc<C, Func<Src, int, HashSet<E>>> ToHashSet<C, Src, E, R>(
        this IBuildFunc<IEntity<E>, Func<Src, int, R>, C> b
    ) => b.IntoImpl().ToCollection<Func<Src, int, HashSet<E>>>();

    public static IBuildFunc<C, Func<Src, int, int, HashSet<E>>> ToHashSet
    <C, Src, E, R>(
        this IBuildFunc<IEntity<E>, Func<Src, int, int, R>, C> b
    ) => b.IntoImpl().ToCollection<Func<Src, int, int, HashSet<E>>>();

#endif

    // private static Func<Expression, ContextExpr, CollectionOption> GetDicAddFn<TEntity, TKey>(
    //     Expression<Func<TEntity, TKey>> key
    // ) => (dic, ctx) => {
    //     var member = Utils.Expr.ExtractBody(key, ctx.Entity);
    //     var add = dic.Type.GetMethod("Add", new[] { member.Type, ctx.Entity.Type });
    //     var call = E.Call(dic, add, member, ctx.Entity);
    //     return new() { Add = call };
    // };

    public static IBuildFunc<C, Func<Src, Dictionary<Key, E>>> ToDictionary
    <C, Src, E, R, Key>(
        this IBuildFunc<IEntity<E>, Func<Src, R>, C> b,
        Expression<Func<E, Key>> key
    ) => b.IntoImpl().ToDictionary<Func<Src, Dictionary<Key, E>>>(key);

    public static IBuildFunc<C, Func<Src, int, Dictionary<Key, E>>> ToDictionary
    <C, Src, E, R, Key>(
        this IBuildFunc<IEntity<E>, Func<Src, int, R>, C> b,
        Expression<Func<E, Key>> key
    ) => b.IntoImpl().ToDictionary<Func<Src, int, Dictionary<Key, E>>>(key);

    public static IBuildFunc<C, Func<Src, int, int, Dictionary<Key, E>>> ToDictionary
    <C, Src, E, R, Key>(
        this IBuildFunc<IEntity<E>, Func<Src, int, int, R>, C> b,
        Expression<Func<E, Key>> key
    ) => b.IntoImpl().ToDictionary<Func<Src, int, int, Dictionary<Key, E>>>(key);

#if DEBUG // have free time to develop support

    public static IBuildFunc<C, Func<Src, TCollection>> ToCollection
    <C, Src, E, R, TCollection>(
        this IBuildFunc<IEntity<E>, Func<Src, R>, C> b,
         Expression<Action<TCollection, E>> addFn
    ) => throw new NotImplementedException();

    public static IBuildFunc<C, Func<Src, int, TCollection>> ToCollection
    <C, Src, E, R, TCollection>(
        this IBuildFunc<IEntity<E>, Func<Src, int, R>, C> b,
        Expression<Action<TCollection, E>> addFn
    ) => throw new NotImplementedException();

    public static IBuildFunc<C, Func<Src, int, int, TCollection>> ToCollection
    <C, Src, E, R, TCollection>(
        this IBuildFunc<IEntity<E>, Func<Src, int, int, R>, C> b,
         Expression<Action<TCollection, E>> addFn
    ) => throw new NotImplementedException();

#endif

}
