
namespace TableRW.Write.I;

internal class BuildTableOption((int row, int col) start)
: IBuildTableOption {
    public int StartRow { get; set; } = start.row;
    public int StartCol { get; set; } = start.col;
    public Expression Enumerable { get; set; } = null!;
    public ParameterExpression Src { get; set; } = null!;
    public Expression[]? InitRow { get; set; }
}

internal interface IBuildTableOption {
    int StartRow { get; }
    int StartCol { get; }
    Expression Enumerable { get; }
    ParameterExpression Src { get; }
    Expression[]? InitRow { get; }
}
