
namespace TableRW;

public delegate DParams DParams(params object?[] args);
public delegate DParams DParamsConst<T>(params T[] args);

public static class DParamsEx {

    public static IEnumerable<Expression> GetParams<T>(
        this Expression<Func<DParams, T, DParams>> dParams,
        string notSupportedExceptionMessage
    ) {
        if (dParams.Body is not InvocationExpression invocationE
        || invocationE.Arguments.Count < 1
        || invocationE.Arguments[0] is not NewArrayExpression newArrayE) {
            throw new NotSupportedException(notSupportedExceptionMessage);
        }

        var args = newArrayE.Expressions.Select(arg => arg switch {
            // When the parameter is a structure, it is boxed
            // .Convert(Object, .Convert(Int32, .Constant(2))
            // .Convert(Object, .Member(e, StructProperty))
            UnaryExpression { NodeType: ExpressionType.Convert, Type: var type, Operand: var op }
            when type == typeof(object) => op,
            _ => arg,
        });
        return args;
    }

    public static DParams Skip(this DParams self, int skip)
        => throw new InvalidOperationException();

    internal static int? IsSkip(Expression e)
        => e is MethodCallExpression {
            Method: { Name: nameof(Skip), ReturnType: var retType },
            Arguments: { Count: 2 } args
        } && retType == typeof(DParams)
        && args[1] is ConstantExpression { Value: int skip } ? skip : null;
}