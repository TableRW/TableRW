using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Utils;

public static class Expr {

    public static Expression ExtractBody(LambdaExpression lmd, params object[] newParams) {
        return lmd.ExtractBody(newParams);
    }

    public static Expression ExtractBody<T1, TResult>(Expression<Func<T1, TResult>> lmd, params object[] newParams) {
        return lmd.ExtractBody(newParams);
    }

    public static Expression ExtractBody<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lmd, params object[] newParams) {
        return lmd.ExtractBody(newParams);
    }

    public static Expression ExtractBody<T1, T2>(Expression<Action<T1, T2>> lmd, params object[] newParams) {
        return lmd.ExtractBody(newParams);
    }

    //public static Expression ExtractBody<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lmd, params object[] newParams) {
    //    return lmd.ExtractBody(newParams);
    //}

    public static NewExpression? TryGetNewExpression(Type type, params Expression[] ctorArgs) {
        var argsType = ctorArgs.Select(e => e.Type).ToArray();
        var ctor = type.GetConstructor(argsType);

        return ctor != null ? E.New(ctor, ctorArgs) : null;
    }

    public static NewExpression GetNewExpression(Type type, params Expression[] ctorArgs) {
        var argsType = ctorArgs.Select(e => e.Type).ToArray();
        var ctor = type.GetConstructor(argsType);
        if (ctor == null) {
            var argsTypeName = string.Join(", ", argsType.Select(t => t.Name));
            throw new InvalidOperationException($"`{type.Name}` Constructor not found;\n argsType: {argsTypeName}");
        }
        return E.New(ctor, ctorArgs);
    }

    public static NewExpression GetNewExpression<T>(params Expression[] ctorArgs)
        => GetNewExpression(typeof (T), ctorArgs);

    internal static NewExpression NewValueType(params Expression[] ctorArgs) {
        var argsType = ctorArgs.Select(e => e.Type).ToArray();
        var tuple_type = (ctorArgs.Length switch {
            1 => typeof(ValueTuple<>),
            2 => typeof(ValueTuple<,>),
            3 => typeof(ValueTuple<,,>),
            4 => typeof(ValueTuple<,,,>),
            5 => typeof(ValueTuple<,,,,>),
            6 => typeof(ValueTuple<,,,,,>),
            7 => typeof(ValueTuple<,,,,,,>),
            8 => typeof(ValueTuple<,,,,,,,>),
            _ => throw new InvalidOperationException("Number of invalid tuple elements"),
        }).MakeGenericType(argsType);

        return E.New(tuple_type.GetConstructor(argsType)!, ctorArgs);
    }

}

class UpdateParameters(ICollection<ParameterExpression> oldParams, params object[] newParams)
: ExpressionVisitor {

    private readonly Dictionary<ParameterExpression, Expression> old_new =
        oldParams.Zip(
            newParams.Select(p => p is Expression exp ? exp : E.Constant(p))
                     .Concat(oldParams.Skip(newParams.Length)),
            (old, @new) => new KeyValuePair<ParameterExpression, Expression>(old, @new))
        .ToDictionary(kv => kv.Key, kv => kv.Value);

    protected override Expression VisitParameter(ParameterExpression node)
        => old_new.TryGetValue(node, out var @new) ? @new : node;
}

class ReplaceMemberAccess(MemberInfo member, Expression newExpr) : ExpressionVisitor {

    protected override Expression VisitMember(MemberExpression node)
        => node.Member.EqualType(member) ? newExpr 
        : node.Update(node.Expression.ReplaceMemberAccess(member, newExpr));
}