using E = System.Linq.Expressions.Expression;

namespace TableRW.Read.I;

class BuildExpr<Fn>(
    (ContextExpr Ctx, Event Event, ReadSequence ReadSeq) data,
    IBuildFunc opt
) : BuildExpr(data, opt) {

    public Expression<Fn> Lambda()
        => E.Lambda<Fn>(BuildReadingTable(), opt.FnParams);

    protected internal override void BuildEndReadingTable(List<E> readingExprs)
        => readingExprs.Add(opt.FnReturn);
}

class BuildExpr {

    protected readonly ContextExpr Ctx;
    protected readonly Event Event;
    protected readonly ReadSequence ReadSeq;

    // protected readonly Expression IsEnd;
    protected readonly IBuildTableOption Opt;

    public BuildExpr(
        (ContextExpr Ctx, Event Event, ReadSequence ReadSeq) data,
        IBuildTableOption opt
    ) {
        (Ctx, Event, ReadSeq) = (data.Ctx, data.Event, data.ReadSeq);
        Opt = opt;
    }

    internal protected virtual BlockExpression BuildReadingTable() {
        var readingExprs = new List<Expression>(40);
        BuildStartReadingTable(readingExprs);
        BuildLoopReadingRow(readingExprs);
        BuildEndReadingTable(readingExprs);

        var blockType = readingExprs.Last().Type;
        var readBlock = E.Block(blockType, [Ctx.Context, Opt.Collection], readingExprs);

        return readBlock;
    }

    internal protected virtual void BuildStartReadingTable(List<Expression> readingTableExprs) {
        var src = Opt.RootReadOpt.Src;
        // ctx = new (src)
        readingTableExprs.Add(E.Assign(Ctx.Context, Ctx.GetNewContext(src)));
        // ctx.Data = InitData(src)
        if (Event.InitDataFn != null) {
            readingTableExprs.Add(E.Assign(Ctx.Data, E.Invoke(Event.InitDataFn, src)));
        }
        if (Ctx.InitData != null) {
            readingTableExprs.Add(E.Assign(Ctx.Data, Ctx.InitData));
        }
        if (Ctx.InitParent != null) {
            readingTableExprs.Add(E.Assign(Ctx.Parent, Ctx.InitParent));
        }

        // // ctx.entity = new Entity()
        // readingTableExprs.Add(E.Assign(Ctx.Entity, Ctx.NewEntity));

        // ctx.iRow = startRow
        readingTableExprs.Add(E.Assign(Ctx.iRow, Opt.StartRow));
        // // ctx.iCol = startCol
        // readingTableExprs.Add(E.Assign(Ctx.iCol, Opt.StartCol));

        // collection = new TCollection();
        readingTableExprs.Add(E.Assign(Opt.Collection, Opt.NewCollection));

        // OnStartReadingTable
        if (Event.StartReadingTableFn != null) {
            readingTableExprs.Add(Event.StartReadingTableFn);
        }
    }

    internal protected virtual void BuildLoopReadingRow(List<Expression> readingTableExprs) {
        //var e_continueRow = E.Label("continueRow");
        var loopRows = E.Loop(
            E.IfThenElse(
                Opt.IsEnd,
                E.Break(Ctx.LblEndTable),
                E.Block(BuildReadingRow())),
            Ctx.LblEndTable);
        //  e_continueRow);

        readingTableExprs.Add(loopRows);
    }

    internal protected virtual void BuildEndReadingTable(List<Expression> readingTableExprs) {
        // OnEndReadingTable
        if (Event.EndReadingTableAction != null) {
           readingTableExprs.Add(Event.EndReadingTableAction);
        }

        readingTableExprs.Add(Opt.Collection);
    }

    internal protected virtual IEnumerable<Expression> BuildReadingRow() {
        var readingRowExprs = new List<Expression>(200);
        BuildStartReadingRow(readingRowExprs);
        ReadSeq.GetExpressions(Ctx.Entity, Ctx.iCol, Opt.RootReadOpt, readingRowExprs);
        BuildEndReadingRow(readingRowExprs);
        return readingRowExprs;
    }

    internal protected virtual void BuildStartReadingRow(List<Expression> readingRowExprs) {
        // if (hasIterAction) {
        //     // iterAction = null
        //     readingRowExprs.Add(E.Assign(E_IterAction, E.Constant(null, E_IterAction.Type)));
        // }


        if (ReadSeq.HasCtxRead) {
            // ctx.iCol = startCol
            readingRowExprs.Add(E.Assign(Ctx.iCol, Opt.StartCol));
            // ctx.PreEntity = ctx.Entity
            readingRowExprs.Add(E.Assign(Ctx.PreEntity, Ctx.Entity));
        }
        // ctx.Entity = new Entity()
        readingRowExprs.Add(E.Assign(Ctx.Entity, Ctx.NewEntity));

        // OnStartReadingRow
        if (Event.StartReadingRowFn != null) {
            readingRowExprs.Add(Event.StartReadingRowFn);
        } else if (Event.StartReadingRowAction != null) {
            readingRowExprs.Add(Event.StartReadingRowAction);
        }
    }

    internal protected virtual void BuildEndReadingRow(List<Expression> readingRowExprs) {
        // EndRow:
        if (Ctx.HasIterAction) {
            readingRowExprs.Add(E.Label(Ctx.LblEndRow));
        }

        // OnEndReadingRow
        if (Event.EndReadingRowFn != null) {
            readingRowExprs.Add(Event.EndReadingRowFn);
        } else if (Event.EndReadingRowAction != null) {
            readingRowExprs.Add(Event.EndReadingRowAction);
        }

        // AddToCollection
        readingRowExprs.Add(Opt.CollectionAdd);
        // ContiueRow:
        if (Ctx.HasIterAction) {
            readingRowExprs.Add(E.Label(Ctx.LblContiueRow));
        }
        // iRow++
        readingRowExprs.Add(E.PostIncrementAssign(Ctx.iRow));

        // if (hasIterAction) {
        //     // if (iterAction == IterAction.EndTable) { break; }
        //     var e_endTable = E.Constant(IterAction.EndTable, E_IterAction.Type);
        //     readingRowExprs.Add(E.IfThen(E.Equal(E_IterAction, e_endTable), E.Break(E_LblEndTable)));
        // }
    }

}
