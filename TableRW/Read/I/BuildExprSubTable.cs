using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Read.I;


internal class BuildSubTableOption((Expression row, Expression col) start)
: BuildTableOption(start) {
    // /// <summary>
    // /// End sub-table read
    // /// </summary>
    // public Expression IsEnd { get; set; } = null!;
    public ParameterExpression RowKey { get; set; } = null!;
    public Expression InitRowkey { get; set; } = null!;
    public Expression ReadRowKey { get; set; } = null!;
    public Expression? PerentICol { get; set; }
    public Expression? IsNullRowKey { get; set; }
    public LabelTarget LblEndSubRead { get; set; } = null!;
}

internal class BuildExprSubTable(
    (ContextExpr Ctx, Event Event, ReadSequence ReadSeq) data,
    BuildSubTableOption opt
) : BuildExpr(data, opt) {

    protected internal override BlockExpression BuildReadingTable() {
        var block = base.BuildReadingTable();

        return block.Update(
            block.Variables.Concat(new[] { opt.RowKey }),
            block.Expressions);
    }

    protected internal override void BuildStartReadingTable(List<E> exprs) {
        exprs.Add(opt.InitRowkey);
        base.BuildStartReadingTable(exprs);

        if (opt.IsNullRowKey != null) {
            if (Event.StartReadingTableFn == null) {
                exprs.Add(opt.IsNullRowKey);
            } else {
                exprs.Insert(exprs.Count - 1, opt.IsNullRowKey);
            }
        }
    }

    protected internal override void BuildEndReadingRow(List<E> readingRowExprs) {
        base.BuildEndReadingRow(readingRowExprs);

        // iRow is out of src row range
        var isEndTable = opt.RootReadOpt.IsEndTable.ReplaceMemberAccess(Ctx.iRow.Member, Ctx.iRow);

        // if ($ctx1.iRow >= ($src.Rows).Count) { goto EndTable; }
        // else { rowKey = readRowKey; }
        readingRowExprs.Add(E.IfThenElse(
            isEndTable, E.Goto(Ctx.LblEndTable), opt.ReadRowKey));

    }

    protected internal override void BuildEndReadingTable(List<E> readingExprs) {
        // OnEndReadingTable
        if (Event.EndReadingTableAction != null) {
            readingExprs.Add(Event.EndReadingTableAction);
        }
        if (opt.PerentICol != null) {
            readingExprs.Add(E.Assign(opt.PerentICol, this.Ctx.iCol));
        }

        // update parent ctx iRow
        // ctx1.iRow = ctx2.iRow - 1
        readingExprs.Add(E.Assign(Opt.StartRow, E.Subtract(Ctx.iRow, E.Constant(1))));

        readingExprs.Add(E.Label(opt.LblEndSubRead));
        readingExprs.Add(Opt.Collection);
    }
}
