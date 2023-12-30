using System.Data;
using TableRW.Read;
using TableRW.Read.DataTableEx;

namespace TableRW.Tests.DataTableEx.Read;

public class BuildLambdaWithDataTest {

    class TableInfo {
        public string Name { get; set; } = "";
        public int RowCount { get; set; }
    }

    [Fact]
    public void ToListData() {
        var tbl = new DataTable("TblName") {
            Columns = {
                { "A", typeof(string) },
            },
            Rows = {
                { "ss" },
                { "33" },
            }
        };
        var reader = new DataTblReader<RecordA, TableInfo>()
            .InitData(src => new() { Name = src.TableName, RowCount = src.Rows.Count })
            .AddColumnRead((string s) => it => {
                it.Entity.Str = $"{s}-{it.Data.Name}({it.Data.RowCount})";
            });

        var readLmd = reader.Lambda(f => f.ReturnData());
        var readFn = readLmd.Compile();
        var (list, info) = readFn(tbl);
        Assert.Equal(tbl.TableName, info.Name);
        Assert.Equal(tbl.Rows.Count, info.RowCount);
        Assert.Equal(2, list.Count);
        Assert.Equal("ss-TblName(2)", list[0].Str);
        Assert.Equal("33-TblName(2)", list[1].Str);
    }

    [Fact]
    public void ToListData_WithStartRowStartColumn() {
        var tbl = new DataTable("TblName") {
            Columns = {
                { "A", typeof(string) },
            },
            Rows = {
                { "30" },
                { "ss" },
                { "33" },
            }
        };
        var reader = new DataTblReader<RecordA, TableInfo>()
            .InitData(src => new() { Name = src.TableName, RowCount = src.Rows.Count })
            .AddColumnRead((string s) => it => {
                it.Entity.Str = $"{s}-{it.Data.Name}({it.Data.RowCount})";
            });

        var readLmd = reader.Lambda(f => f.Start().ReturnData());
        var readFn = readLmd.Compile();
        var (list, info) = readFn(tbl, 1, 0);
        Assert.Equal(tbl.TableName, info.Name);
        Assert.Equal(tbl.Rows.Count, info.RowCount);
        Assert.Equal(2, list.Count);
        Assert.Equal("ss-TblName(3)", list[0].Str);
        Assert.Equal("33-TblName(3)", list[1].Str);
    }

    [Fact]
    public void ToCollectionData() {
        var tbl = new DataTable("TblName") {
            Columns = {
                { "A", typeof(string) },
                { "B", typeof(int) },
            },
            Rows = {
                { "30", 21 },
                { "ss", 22 },
            }
        };
        var reader = new DataTblReader<RecordA, TableInfo>()
            .InitData(src => new() { Name = src.TableName, RowCount = src.Rows.Count })
            .AddColumnRead((string val) => it => it.Entity.Str = $"{val}-{it.Data.Name}({it.Data.RowCount})")
            .AddColumns((s, e) => s(e.FieldInt));

        var readLmd = reader.Lambda(f => f.ToDictionary(e => e.FieldInt).ReturnData());
        var readFn = readLmd.Compile();
        var (dic, info) = readFn(tbl);
        Assert.Equal(tbl.TableName, info.Name);
        Assert.Equal(tbl.Rows.Count, info.RowCount);
        Assert.Equal(2, dic.Count);
        Assert.Equal("30-TblName(2)", dic[21].Str);
        Assert.Equal("ss-TblName(2)", dic[22].Str);
    }

    [Fact]
    public void ToCollectionData_WithStartRowStartColumn() {
        var tbl = new DataTable("TblName") {
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
        var reader = new DataTblReader<RecordA, TableInfo>()
            .InitData(src => new() { Name = src.TableName, RowCount = src.Rows.Count })
            .AddColumnRead((string val) => it => it.Entity.Str = $"{val}-{it.Data.Name}({it.Data.RowCount})")
            .AddColumns((s, e) => s(e.FieldInt));

        var readLmd = reader.Lambda(
            f => f.Start().ToDictionary(e => e.FieldInt).ReturnData());

        var readFn = readLmd.Compile();
        var (dic, info) = readFn(tbl, 1, 0);
        Assert.Equal(tbl.TableName, info.Name);
        Assert.Equal(tbl.Rows.Count, info.RowCount);
        Assert.Equal(2, dic.Count);
        Assert.Equal("31-TblName(3)", dic[21].Str);
        Assert.Equal("ss-TblName(3)", dic[22].Str);
    }

}
