using E = System.Linq.Expressions.Expression;

namespace TableRW.Utils.Ex;
internal static class ExpressionEx {

    public static Expression ExtractBody(this LambdaExpression lmd, params object[] newParams) {
        var modify = new UpdateParameters(lmd.Parameters, newParams);
        return modify.Visit(lmd.Body);
    }

    public static Expression UpdateParameters(this Expression exp,
        ParameterExpression old, Expression @new
    ) {
        var modify = new UpdateParameters(new[] { old }, @new);
        return modify.Visit(exp);
    }

    internal static Expression ReplaceMemberAccess(this Expression expr, MemberInfo member, Expression newExpr) {
        var replace = new ReplaceMemberAccess(member, newExpr);
        return replace.Visit(expr);
    }

    internal static MethodCallExpression Call(
        this Expression instance, string methodName, params Expression[] argsExpr
    ) {
        var method = instance.Type.GetMethod(methodName, argsExpr.Select(e => e.Type).ToArray());
        return E.Call(instance, method, argsExpr);
    }

    internal static MethodCallExpression Call<T>(
        this Expression instance, string methodName, params Expression[] argsExpr
    ) {
        var method = typeof(T).GetMethod(methodName, argsExpr.Select(e => e.Type).ToArray());
        return E.Call(instance, method, argsExpr);
    }

}
