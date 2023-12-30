using System.Diagnostics;

namespace TableRW.Write.I;

public interface IContext<out TSource, TRow, TEntity> {
    /// <summary> Index column </summary>
    public int iCol { get; internal set; }

    /// <summary> Index row </summary>
    public int iRow { get; internal set; }

    /// <summary> Data source </summary>
    public TSource Src { get; }
    /// <summary> Current entity </summary>
    public TEntity Entity { get; internal set; }
    public TRow Row { get; internal set; }
}

public interface IContext<out TSource, TRow, TEntity, D> : IContext<TSource, TRow, TEntity> {
    public D Data { get; set; }
}

public interface ISubContext<out TSource, TRow, TEntity, P> : IContext<TSource, TRow, TEntity> {
    public P Parent { get; internal set; }
}

public interface ISubContext<out TSource, TRow, TEntity, P, D>
: ISubContext<TSource, TRow, TEntity, P> {
    public D Data { get; set; }
}


[DebuggerDisplay("iRow = {iRow}, iCol = {iCol}, Entity = \\{ {Entity} \\}")]
public class Context<TSource, TRow, TEntity>(TSource src)
: IContext<TSource, TRow, TEntity> {
    public TSource Src { get; set; } = src;

    public TRow Row { get; set; } = default!;

    public TEntity Entity { get; set; } = default!;

    public int iCol { get; set; } = 0;

    public int iRow { get; set; } = 0;

}

[DebuggerDisplay("iRow = {iRow}, iCol = {iCol}, Entity = \\{ {Entity} \\}")]
public class Context<TSource, TRow, TEntity, D>(TSource src)
: Context<TSource, TRow, TEntity>(src)
, IContext<TSource, TRow, TEntity, D> {

    public D Data { get; set; } = default!;

}

[DebuggerDisplay("iRow = {iRow}, iCol = {iCol}, Entity = \\{ {Entity} \\}")]
public class SubContext<TSource, TRow, TEntity, P, D>(TSource src)
: Context<TSource, TRow, TEntity, D>(src)
, ISubContext<TSource, TRow, TEntity, P, D> {

    public P Parent { get; set; } = default!;
}