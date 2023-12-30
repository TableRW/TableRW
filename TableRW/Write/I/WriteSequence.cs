using E = System.Linq.Expressions.Expression;
using TableRW.Utils.Ex;

namespace TableRW.Write.I;

internal class WriteSequence {

    /// <summary>
    /// 相对于开始列的偏移
    /// </summary>
    int _index = 0;
    bool _isFirstColumn = true;

    // List<(int, Expression, isAction)>
    readonly List<(int, Expression, bool isAction)> _writeColumns = new();

    public int FirstIndex => _writeColumns.FirstOrDefault().Item1;
    public int LastIndex => _writeColumns.LastOrDefault().Item1;
    public int CurrentIndex() => _index;
    public void AddOffset(int offset) => _index += offset;

    public void AddColumn(Expression read, bool isAction) {
        _index += _isFirstColumn ? 0 : 1;
        _writeColumns.Add((_index, read, isAction));
        _isFirstColumn = false;
    }

    public void AddCtxAction(Expression read) {
        _writeColumns.Add((_index, read, true));
    }

    internal void GetExpressions(int startCol, MemberExpression iCol, List<Expression> readingRowExprs) {
        var (preIsAction, preIndex) = (false, (int?)null);
        foreach (var (index, value, isAction) in _writeColumns) {
            var indexCol = E.Constant(index + startCol);
            if (isAction && (preIsAction == false || preIndex != index)) {
                readingRowExprs.Add(E.Assign(iCol, indexCol));
            }
            var value1 = value.ReplaceMemberAccess(iCol.Member, indexCol);
            readingRowExprs.Add(value1);
            (preIsAction, preIndex) =(isAction, index);
        };
    }

}
