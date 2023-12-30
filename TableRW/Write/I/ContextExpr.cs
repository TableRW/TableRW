using TableRW.Utils.Ex;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Write.I;

public class ContextExpr {
    public ParameterExpression Context { get; }
    // public ParameterExpression Src;
    public Expression Entity { get; }
    public MemberExpression Row { get; }
    public MemberExpression iRow { get; }
    public MemberExpression iCol { get; }
    public Expression? Data { get; }
    public Expression? InitData { get; internal set; }
    public Expression? Parent { get; }
    public Expression? InitParent { get; internal set; }
    public string DeepNo { get; }
    public ContextExpr(Type contextType) {
        CheckTypeConstraint();

        DeepNo = GetDeepNo();
        Context = E.Parameter(contextType, "ctx" + DeepNo);

        Row = E.MakeMemberAccess(Context, contextType.GetProperty("Row")!);
        iRow = E.MakeMemberAccess(Context, contextType.GetProperty("iRow")!);
        iCol = E.MakeMemberAccess(Context, contextType.GetProperty("iCol")!);
        Entity = E.MakeMemberAccess(Context, contextType.GetProperty("Entity")!);

        if (contextType.GetInterfaceProp("IContext`4", "Data") is { } p) {
            Data = E.MakeMemberAccess(Context, p);
        }
        // Context.Type.GetProperties() 只能获取到当前接口的 属性
        if (contextType.GetInterfaceProp("ISubContext`4", "Parent") is { } p2) {
            Parent = E.MakeMemberAccess(Context, p2);
        }

        string GetDeepNo() {
            return ParentCount(contextType) is var n and not 0 ? $"{n}" : "";

            // SubContext 有多层
            int ParentCount(Type? t)
                => t == null ? -1 : 1 + ParentCount(
                   t.GetInterfaceProp("ISubContext`3", "Parent")?.PropertyType);
        }

        void CheckTypeConstraint() {
            var icType = typeof(IContext<,,>);

            var hasIc = contextType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == icType);

            if (hasIc) { return; }

            if (contextType.IsInterface && contextType.IsGenericType
            && contextType.GetGenericTypeDefinition() == icType) {
                return;
            }

            var icName = icType.Namespace + icType.Name;
            var msg = $"The type constraint must satisfy the \"{icName}\" interfaces";
            throw new NotSupportedException(msg);
        }
    }

    public Expression GetNewContext(ParameterExpression src) {
        // contextType 可能是一个类，直接调用构造函数 new
        if (Utils.Expr.TryGetNewExpression(Context.Type, src) is { } newExpr) {
            return newExpr;
        }

        // contextType 可能是一个接口，直接使用本库提供的默认类
        var d = Context.Type.GetGenericTypeDefinition();
        var dataType = Data?.Type ?? typeof(object);

        if (d == typeof(IContext<,,>) || d == typeof(IContext<,,,>)) {
            var ctxClass = typeof(Context<,,,>)
                .MakeGenericType(src.Type, Row.Type, Entity.Type, dataType);

            return Utils.Expr.GetNewExpression(ctxClass, src);
        } else if (d == typeof(ISubContext<,,,>) || d == typeof(ISubContext<,,,,>)) {
            var ctxClass = typeof(SubContext<,,,,>)
                .MakeGenericType(src.Type, Row.Type, Entity.Type, Parent!.Type, dataType);

            return Utils.Expr.GetNewExpression(ctxClass, src);
        }
        throw new NotSupportedException("Interface cannot be instantiated with default");

    }

}
