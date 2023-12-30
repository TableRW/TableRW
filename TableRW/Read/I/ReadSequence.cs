using E = System.Linq.Expressions.Expression;
using TableRW.Utils.Ex;

namespace TableRW.Read.I;


internal class RootReadOpt {
    /// <summary> 是常量，或者是函数参数 </summary>
    public Expression StartCol { get; set; } = null!;
    /// <summary>
    /// Read to the last row of the src-table
    /// </summary>
    public Expression IsEndTable { get; set; } = null!;
    public ParameterExpression Src { get; set; } = null!;
}

internal delegate Expression PropReadFn(RootReadOpt rootOpt);


internal class ReadSequence {

    /// <summary>
    /// 相对于开始列的偏移
    /// </summary>
    int _index = 0;
    bool _isFirstColumn = true;

    // List<(MemberInfo? member, int index, Expression | PropReadFn propRead)>
    readonly List<(MemberInfo? member, int index, object propRead)> _readColumns = new();

    /// <summary>
    /// SetDynamicColumn 是为了 AddActionRead 时 Iter 的 iCol 不会失效
    /// </summary>
    public void SetHasCtxRead() => HasCtxRead = true;
    public bool HasCtxRead { get; private set; } = false;
    public int CurrentIndex() => _index;
    public void AddOffset(int offset) => _index += offset;

    // params Expression[] read，应该只有一个表达式，并且不是对 prop 进行赋值。
    public void AddColumnRead(MemberInfo? member, Expression read) {
        _index += _isFirstColumn ? 0 : 1;
        _readColumns.Add((member, _index, read));
        _isFirstColumn = false;
    }

    public void AddColumnRead(MemberInfo? member, PropReadFn read) {
        _index += _isFirstColumn ? 0 : 1;
        _readColumns.Add((member, _index, read));
        _isFirstColumn = false;
    }

    public void AddCtxRead(Expression read) {
        _readColumns.Add((null, _index, read));
        SetHasCtxRead();
    }

    public (int index, object read)? Find(MemberInfo? member) {
        return _readColumns.Where(t => t.member?.EqualType(member) == true)
            .Select(t => (t.index, t.propRead)).FirstOrDefault();
    }

    // public bool HasMember(MemberInfo member)
    //     => _readColumns.Any(t => t.member?.EqualType(member) == true);

    // internal PropReadFn? GetReadRowKey(MemberInfo member, Expression rowKey) {
    //     var (_, index, read) = _readColumns.FirstOrDefault(t => t.member?.EqualType(member) == true);
    //     if (read is not Expression readValue) { return null; }
    //
    //     return (rootOpt) => {
    //         var indexCol = GetIndexCol(rootOpt.StartCol, index);
    //         readValue = readValue.ReplaceMemberAccess(iCol.Member, indexCol);
    //         return E.Assign(rowKey, readValue);
    //     };
    // }

    internal Expression GetIndexCol(Expression startCol, int index)
    => startCol switch {
        ConstantExpression { Value: int v } => E.Constant(v + index),
        ParameterExpression => E.Add(startCol, E.Constant(index)),
        _ => throw new NotSupportedException("startCol supports only ConstantExpression or ParameterExpression"),
    };

    // public void GetExpressions(LambdaBuilder builder) {
    internal void GetExpressions(
        Expression entity, MemberExpression iCol, RootReadOpt rootOpt, List<Expression> readingRowExprs
    ) {
        int? preIndex = null;
        foreach (var (member, index, propRead) in _readColumns) {
            var indexCol = GetIndexCol(rootOpt.StartCol, index);
            var readValue = propRead switch {
                PropReadFn fn => fn(rootOpt),
                Expression read => read,
                _ => throw new NotSupportedException(),
            };

            if (!HasCtxRead || propRead is not PropReadFn) {
                readValue = readValue.ReplaceMemberAccess(iCol.Member, indexCol);
            } else {
                if (preIndex != index) {
                    readingRowExprs.Add(E.Assign(iCol, indexCol));
                }
                preIndex = index;
            }

            if (member == null) {
                readingRowExprs.Add(readValue);
            } else {
                // entity.Prop = readValue
                readingRowExprs.Add(E.Assign(E.MakeMemberAccess(entity, member), readValue));
            }

        };
    }

}
