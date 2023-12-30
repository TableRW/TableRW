using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Read.I;

internal interface IBuildFunc : IBuildTableOption {
    ParameterExpression[] FnParams { get; }
    Expression FnReturn { get; }

}

class BuildFunc<C, Fn>(ContextExpr ctx, (Expression row, Expression col) start)
: BuildTableOption(start), IBuildFunc<C, Fn>, IBuildFunc {
    public ParameterExpression[] FnParams { get; private set; } = null!;
    public Expression FnReturn { get; private set; } = null!;

    ParameterExpression ParamStartRow() {
        StartRow = E.Parameter(typeof(int), "startRow");
        return (ParameterExpression)StartRow;
    }

    ParameterExpression ParamStartCol() {
        StartCol = E.Parameter(typeof(int), "startRow");
        return (ParameterExpression)StartCol;
    }


    internal BuildFunc<C, Fn2> UpdateParams<Fn2>() {
        var upd = BuildFunc<C, Fn2>.FromFnInfo(ctx, (this.StartRow, this.StartCol));
        upd.CollectionAdd =
            this.CollectionAdd.UpdateParameters(this.Collection, upd.Collection);
        return upd;
    }

    internal BuildFunc<C, Fn2> ToCollection<Fn2>()
        => BuildFunc<C, Fn2>.FromICollection(ctx, (this.StartRow, this.StartCol));

    internal BuildFunc<C, Fn2> ToDictionary<Fn2>(LambdaExpression key)
        => BuildFunc<C, Fn2>.FromDictionaryKey(ctx, (StartRow, StartCol), key);


    internal static BuildFunc<C, Fn> FromFnInfo(ContextExpr ctx, (Expression row, Expression col) start) {
        var self = new BuildFunc<C, Fn>(ctx, start);

        var fnArgs = typeof(Fn).GetGenericArguments();
        var src = E.Parameter(fnArgs[0], "src");
        self.FnParams = fnArgs.Length switch {
            2 => new[] { src },
            3 => new[] { src, self.ParamStartRow() },
            4 => new[] { src, self.ParamStartRow(), self.ParamStartCol() },
            _ => throw new InvalidOperationException("Generic parameter error")
        };

        SetFnReturn(fnArgs.Last());
        return self;

        void SetFnReturn(Type t) {
            if (t.IsGenericTypeDefinitionOf(typeof(ValueTuple<,>))) {
                var tupleArgs = t.GetGenericArguments();
                self.Collection = E.Parameter(tupleArgs[0], "collection");
                self.FnReturn = Utils.Expr.NewValueType(self.Collection, ctx.Data!);
            } else {
                self.Collection = E.Parameter(t, "collection");
                self.FnReturn = self.Collection;
            }
            self.NewCollection = Utils.Expr.GetNewExpression(self.Collection.Type);
        }
    }

    internal static BuildFunc<C, Fn> FromICollection(ContextExpr ctx, (Expression row, Expression col) start) {
        var self = FromFnInfo(ctx, start);

        self.CollectionAdd = self.Collection.Call("Add", ctx.Entity);
        return self;
    }

    internal static BuildFunc<C, Fn> FromDictionaryKey(
        ContextExpr ctx, (Expression row, Expression col) start, LambdaExpression fnKey
    ) {
        var self = FromFnInfo(ctx, start);
        var key = fnKey.ExtractBody(ctx.Entity);

        self.CollectionAdd = self.Collection.Call("Add", key, ctx.Entity);
        return self;
    }
}
