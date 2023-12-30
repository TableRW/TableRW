
namespace TableRW.Read.I;

internal class BuildTableOption((Expression row, Expression col) start)
: IBuildTableOption {
    public Expression StartRow { get; set; } = start.row;
    public Expression StartCol { get; set; } = start.col;
    public ParameterExpression Collection { get; set; } = null!;
    public Expression NewCollection { get; set; } = null!;
    public Expression CollectionAdd { get; set; } = null!;
    public Expression IsEnd { get; set; } = null!;
    public RootReadOpt RootReadOpt { get; set; } = null!;
}

internal interface IBuildTableOption {
    /// <summary> 可以是常量，也可以是变量，也可能是参数 </summary>
    Expression StartRow { get; }
    /// <summary> 可以是常量，也可以是变量，也可能是参数 </summary>
    Expression StartCol { get; }
    /// <summary> 可以是 List<>, Array, Dictionary<>, HashSet<> </summary>
    ParameterExpression Collection { get; }
    Expression NewCollection { get; }
    Expression CollectionAdd { get; }
    Expression IsEnd { get; }
    RootReadOpt RootReadOpt { get; }
}
