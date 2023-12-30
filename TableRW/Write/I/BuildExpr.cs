using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Write.I;

class BuildExpr<TSource, TEntity> {

    protected readonly ContextExpr Ctx;
    protected readonly Event Event;
    protected readonly WriteSequence WriteSeq;

    protected readonly IBuildTableOption Opt;
    protected readonly ParameterExpression EntityItor = E.Variable(typeof(IEnumerator<TEntity>), "entityItor");
    public BuildExpr(
        (ContextExpr Ctx, Event Event, WriteSequence WriteSeq) data,
        IBuildTableOption opt
    ) {
        (Ctx, Event, WriteSeq) = (data.Ctx, data.Event, data.WriteSeq);
        Opt = opt;
    }

    public Expression<Action<TSource, IEnumerable<TEntity>>> Lambda()
    => E.Lambda<Action<TSource, IEnumerable<TEntity>>>(
        BuildWritingTable(), Opt.Src, (ParameterExpression)Opt.Enumerable);

    internal protected virtual BlockExpression BuildWritingTable() {
        var writingExprs = new List<Expression>(40);
        BuildStartWritingTable(writingExprs);
        BuildLoopWritingRow(writingExprs);
        BuildEndWritingTable(writingExprs);

        var blockType = writingExprs.Last().Type;
        var readBlock = E.Block(blockType, [Ctx.Context, EntityItor], writingExprs);

        return readBlock;
    }

    internal protected virtual void BuildStartWritingTable(List<Expression> writingTableExprs) {
        // ctx = new (src)
        writingTableExprs.Add(E.Assign(Ctx.Context, Ctx.GetNewContext(Opt.Src)));
        // ctx.Data = InitData(src)
        if (Event.InitDataFn != null) {
            writingTableExprs.Add(E.Assign(Ctx.Data!, E.Invoke(Event.InitDataFn, Opt.Src)));
        }
        if (Ctx.InitData != null) {
            writingTableExprs.Add(E.Assign(Ctx.Data!, Ctx.InitData));
        }
        if (Ctx.InitParent != null) {
            writingTableExprs.Add(E.Assign(Ctx.Parent!, Ctx.InitParent));
        }

        // ctx.iRow = startRow
        writingTableExprs.Add(E.Assign(Ctx.iRow, E.Constant(Opt.StartRow)));

        var getItor = E.Assign(EntityItor, E.Call(Opt.Enumerable, "GetEnumerator", []));
        writingTableExprs.Add(getItor);

        // OnStartWritingTable
        if (Event.StartWritingTableAction != null) {
            var startCol = Opt.StartCol + WriteSeq.FirstIndex;
            if (startCol != 0) {
                writingTableExprs.Add(E.Assign(Ctx.iCol, E.Constant(startCol)));
            }
            writingTableExprs.Add(Event.StartWritingTableAction);
        }
    }

    internal protected virtual void BuildLoopWritingRow(List<Expression> writingTableExprs) {
        var lblEndLoop = E.Label("lblEndLoop");
        var loopRows = E.Loop(
            E.IfThenElse(
                EntityItor.Call<System.Collections.IEnumerator>("MoveNext"),
                E.Block(BuildWritingRow()),
                E.Break(lblEndLoop)),
            lblEndLoop);
        writingTableExprs.Add(loopRows);
    }

    internal protected virtual void BuildEndWritingTable(List<Expression> writingTableExprs) {
        // OnEndWritingTable
        if (Event.EndWritingTableAction != null) {
            var endCol = Opt.StartCol + WriteSeq.LastIndex;
            writingTableExprs.Add(E.Assign(Ctx.iCol, E.Constant(endCol)));
            writingTableExprs.Add(Event.EndWritingTableAction);
        }

    }

    internal protected virtual IEnumerable<Expression> BuildWritingRow() {
        var writingRowExprs = new List<Expression>(200);
        BuildStartWritingRow(writingRowExprs);
        WriteSeq.GetExpressions(Opt.StartCol, Ctx.iCol, writingRowExprs);
        BuildEndWritingRow(writingRowExprs);
        return writingRowExprs;
    }

    internal protected virtual void BuildStartWritingRow(List<Expression> writingRowExprs) {
        // ctx.Entity = entityItor.Current
        writingRowExprs.Add(E.Assign(Ctx.Entity, E.Property(EntityItor, "Current")));

        if (Opt.InitRow != null) {
            writingRowExprs.AddRange(Opt.InitRow);
        }

        // OnStartWritingRow
        if (Event.StartWritingRowAction != null) {
            var startCol = Opt.StartCol + WriteSeq.FirstIndex;
            if (startCol != 0) {
                writingRowExprs.Add(E.Assign(Ctx.iCol, E.Constant(startCol)));
            }
            writingRowExprs.Add(Event.StartWritingRowAction);
        }
    }

    internal protected virtual void BuildEndWritingRow(List<Expression> writingRowExprs) {
        // OnEndWritingRow
        if (Event.EndWritingRowAction != null) {
            var endCol = Opt.StartCol + WriteSeq.LastIndex;
            writingRowExprs.Add(E.Assign(Ctx.iCol, E.Constant(endCol)));
            writingRowExprs.Add(Event.EndWritingRowAction);
        }

        // iRow++
        writingRowExprs.Add(E.PostIncrementAssign(Ctx.iRow));

    }

}
