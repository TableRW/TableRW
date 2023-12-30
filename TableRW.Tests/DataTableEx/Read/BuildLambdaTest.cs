using System.Data;
using TableRW.Read;
using TableRW.Read.DataTableEx;

namespace TableRW.Tests.DataTableEx.Read;
public class BuildLambdaTest {

    [Fact]
    public void ToList() {
        var tbl = new DataTable() {
            Columns = {
                { "A", typeof(string) },
            },
            Rows = {
                { "ss" },
                { "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldStr));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal("ss", list[0].FieldStr);
        Assert.Equal("33", list[1].FieldStr);
    }

    [Fact]
    public void ToList_WithStartRowStartColumn() {
        var tbl = new DataTable() {
            Columns = {
                { "A", typeof(string) },
            },
            Rows = {
                { "10" },
                { "20" },
                { "30" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldStr));

        var readLmd = reader.Lambda(b => b.Start());
        var readFn = readLmd.Compile();
        var list = readFn(tbl, 1, 0);
        Assert.Equal(2, list.Count);
        Assert.Equal("20", list[0].FieldStr);
        Assert.Equal("30", list[1].FieldStr);
    }

    [Fact]
    public void ToDictionary() {
        var tbl = new DataTable() {
            Columns = {
                { "A", typeof(string) },
                { "B", typeof(int) },
            },
            Rows = {
                { "30", 21 },
                { "ss", 22 },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.Str, e.FieldInt));

        var readLmd = reader.Lambda(f => f.ToDictionary(e => e.FieldInt));
        var readFn = readLmd.Compile();
        var dic = readFn(tbl);
        Assert.Equal(2, dic.Count);
        Assert.Equal("30", dic[21].Str);
        Assert.Equal("ss", dic[22].Str);
    }

    [Fact]
    public void ToDictionary_WithStartRowStartColumn() {
        var tbl = new DataTable() {
            Columns = {
                { "A", typeof(string) },
                { "B", typeof(int) },
            },
            Rows = {
                { "30", 20 },
                { "31", 21 },
                { "ss", 22 },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.Str, e.FieldInt));

        var readLmd = reader.Lambda(f => f.Start().ToDictionary(e => e.FieldInt));
        var readFn = readLmd.Compile();
        var dic = readFn(tbl, 1, 0);
        Assert.Equal(2, dic.Count);
        Assert.Equal("31", dic[21].Str);
        Assert.Equal("ss", dic[22].Str);
    }

}
