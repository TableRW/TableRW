

namespace TableRW.Read;

public static class DParamsReadEx {

    public static IReadOnlyList<MemberInfo> GetEntityMembers<TEntity>(
        this Expression<Func<DParams, TEntity, DParams>> members
    ) {
        var unsupported_msg = $"Unsupported expressions, valid examples: `(s, e) => s(e.Prop1, e.Prop2)`";
        HashSet<MemberInfo> set = [];

        // 要保证 MemberInfo 无重复，立即迭代检查
        List<MemberInfo> args = members.GetParams(unsupported_msg).Select(arg => arg switch {
            MemberExpression member
                => set.Add(member.Member)
                ? member.Member
                : throw new NotSupportedException($"Duplicate entity member `{member.Member.Name}`"),
            _ => throw new NotSupportedException(unsupported_msg),
        }).ToList();

        return args;
    }


    /// <summary>
    /// Get the entity members in the expression.
    /// </summary>
    /// <returns> ICollection&lt;MemberInfo | Skip> </returns>
    public static IReadOnlyList<object> GetEntityMembersWithSkipColumn<TEntity>(
        this Expression<Func<DParams, TEntity, DParams>> members
    ) {
        var unsupported_msg = $"Unsupported expressions, valid examples: `(s, e) => s(e.Prop1, s.{nameof(DParamsEx.Skip)}(2), e.Prop2)`";

        var set = new HashSet<MemberInfo>();

        // 要保证 MemberInfo 无重复，立即迭代检查
        // List<Skip | MemberInfo>
        List<object> args = members.GetParams(unsupported_msg).Select(arg => arg switch {
            MemberExpression member
                => set.Add(member.Member)
                ? (object)member.Member
                : throw new NotSupportedException($"Duplicate entity member `{member.Member.Name}`"),
            MethodCallExpression {
                Method: { Name: nameof(DParamsEx.Skip), ReturnType: var retType },
                Arguments: { Count: 2 } args
            } when retType == typeof(DParams) && args[1] is ConstantExpression { Value: int skip }
                => new SkipColumn(skip),
            MethodCallExpression {
                Method: { Name: nameof(RowKey), ReturnType: var retType },
                Arguments: { Count: 2 } args
            } when retType == typeof(DParams) && args[1] is MemberExpression member
                => set.Add(member.Member)
                ? new RowKey(member.Member)
                : throw new NotSupportedException($"Duplicate entity member `{member.Member.Name}`"),
            _ => throw new NotSupportedException(unsupported_msg),
        }).ToList();

        return args;
    }

    public static IEnumerable<string> GetParams(
        this Expression<Func<DParamsConst<string>, DParams>> dParams,
        string? notSupportedExceptionMessage = null
    ) {
        notSupportedExceptionMessage ??= $"Unsupported expressions, valid examples: `f => f(\"Id\", \"Name\")`";
        if (dParams.Body is not InvocationExpression invocationE
        || invocationE.Arguments.Count < 1
        || invocationE.Arguments[0] is not NewArrayExpression newArrayE) {
            throw new NotSupportedException(notSupportedExceptionMessage);
        }

        var args = newArrayE.Expressions.Select(arg => arg switch {
            ConstantExpression { Value: string name } => name,
            _ => throw new NotSupportedException(notSupportedExceptionMessage),
        });
        return args;
    }

    public static DParams RowKey<T>(this DParams self, T key)
        => throw new InvalidOperationException();

    internal static DParams SubTable<T>(
        this DParams self,
        ICollection<T> subEntity,
        Expression<Func<T, DParams>> subColumn
    ) {
        throw new InvalidOperationException();
    }

    //public static DParams ReadAction<T>(this DParams s, Func<T, IterAction?> _) {
    //    throw new InvalidOperationException();
    //}

    //public static void OnStartReadingRow(this DParams s, Func<OnStartReadingRow> _) {
    //    throw new InvalidOperationException();
    //}

}

class RowKey(MemberInfo member) { public readonly MemberInfo Member = member; }
