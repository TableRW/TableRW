using E = System.Linq.Expressions.Expression;
using TableRW.Utils;

namespace TableRW.Tests.Utils;
public class ExprTest {

    [Fact]
    public void ExtractBody_Func1_Constant() {
        var body = Expr.ExtractBody((string a) => a.Length, "aaa");

        Assert.Equal("\"aaa\".Length", body.ToString());
    }

    [Fact]
    public void ExtractBody_Func1_Expression() {
        var e_aaa = E.Parameter(typeof(string), "aaa");
        var body = Expr.ExtractBody((string a) => a.Length, e_aaa);

        Assert.Equal("aaa.Length", body.ToString());
    }

    [Fact]
    public void ExtractBody_Func2_PartialParams() {
        var body = Expr.ExtractBody((string a, int b) => a.Length + b, "aaa");

        Assert.Equal("(\"aaa\".Length + b)", body.ToString());
    }

    [Fact]
    public void ExtractBody_Func2_Constants() {
        var body = Expr.ExtractBody((string a, int b) => a.Length + b, "aaa", 3);

        Assert.Equal("(\"aaa\".Length + 3)", body.ToString());
    }

    [Fact]
    public void ExtractBody_Func2_ExpressionAndConstants() {
        var e_aaa = E.Parameter(typeof(string), "aaa");
        var body = Expr.ExtractBody((string a, int b) => a.Length + b, e_aaa, 3);

        Assert.Equal("(aaa.Length + 3)", body.ToString());
    }

    [Fact]
    public void ExtractBody_Action2_PartialParams() {
        var body = Expr.ExtractBody((int a, List<int> ls) => ls.Add(a), 111);

        Assert.Equal("ls.Add(111)", body.ToString());
    }


    [Fact]
    public void ExtractBody_Action2_ExpressionAndConstants() {
        var e_list = E.Parameter(typeof(List<int>), "list");
        var body = Expr.ExtractBody((int a, List<int> ls) => ls.Add(a), 111, e_list);

        Assert.Equal("list.Add(111)", body.ToString());
    }

    [Fact]
    public void GetNewExpression_Params() {
        var e_new_list = Expr.GetNewExpression<List<int>>(E.Constant(16));
        var fn = E.Lambda<Func<List<int>>>(e_new_list).Compile();
        Assert.Equal(16, fn().Capacity);
    }

    [Fact]
    public void GetNewExpression_Parameterless() {
        var e_new_list = Expr.GetNewExpression<List<int>>();
        var fn = E.Lambda<Func<List<int>>>(e_new_list).Compile();
        Assert.Equal(new List<int>().Capacity, fn().Capacity);
    }

    //[Fact]
    //public void NewValueType() {
    //    {
    //        var e_tuple = Expr.NewValueType(E.Constant(11));
    //        var fn = E.Lambda<Func<ValueTuple<int>>>(e_tuple).Compile();
    //        Assert.Equal(ValueTuple.Create(11), fn());
    //    }
    //    {
    //        var e_tuple = Expr.NewValueType(E.Constant(11), E.Constant("ss"));
    //        var fn = E.Lambda<Func<(int, string)>>(e_tuple).Compile();
    //        Assert.Equal((11, "ss"), fn());
    //    }
    //}


    //[Fact]
    //public void NewValueType_EmptyElement() {
    //    Assert.Throws<InvalidOperationException>(() => {
    //        _ = Expr.NewValueType();
    //    });
    //}

}
