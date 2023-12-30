using System.Data;
using TableRW.Read;
using TableRW.Read.DataTableEx;

namespace TableRW.Tests.DataTableEx.Read;

public class DataTableReaderTest {

    [Fact]
    public void AddColumns() {
        var tbl = new DataTable() {
            Columns = {
                { "A", typeof(string) },
                { "B", typeof(int) },
                { "C", typeof(string) },
                { "D", typeof(int) },
                { "E", typeof(int) },
            },
            Rows = {
                { "A1", 21, "C1", 41, null },
                { "A2", 22, "C2", 42, 52 },
            }
        };

        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldStr, s.Skip(2), e.StructInt, e.NullableInt));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);

        Assert.Equal(2, list.Count);
        Assert.Equal("A1", list[0].FieldStr);
        Assert.Equal(41, list[0].StructInt);
        Assert.Null(list[0].NullableInt);
        Assert.Equal("A2", list[1].FieldStr);
        Assert.Equal(42, list[1].StructInt);
        Assert.Equal(52, list[1].NullableInt);
    }

    [Fact]
    public void AddMapHeaderColumns() {
        var tbl = new DataTable() {
            Columns = {
                { "A", typeof(string) },
                { "B", typeof(int) },
                { "C", typeof(string) },
            },
            Rows = {
                { "A1", 21, "C1" },
                { "A2", 22, "C2" },
            }
        };

        var reader = new DataTblReader<RecordA>()
            .AddMapHeaderColumns(
                header => header("C", "B", "A"),
                (s, e) => s(e.FieldStr, e.StructInt, e.Str)
            );

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);

        Assert.Equal(2, list.Count);
        Assert.Equal("A1", list[0].Str);
        Assert.Equal(21, list[0].StructInt);
        Assert.Equal("C1", list[0].FieldStr);
        Assert.Equal("A2", list[1].Str);
        Assert.Equal(22, list[1].StructInt);
        Assert.Equal("C2", list[1].FieldStr);
    }

    [Fact]
    public void AddColumns_Empty() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
            },
            Rows = {
                { 1, "30" },
                { 2, "ss" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s())
            .AddColumns((s, e) => s(e.StructInt));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].StructInt);
        Assert.Equal(2, list[1].StructInt);
    }

    [Fact]
    public void AddColumns_OtherExpression() {
        Assert.Throws<NotSupportedException>(() => {
            var reader = new DataTblReader<RecordA>()
                .AddColumns((s, e) => s(e.Str!.ToString() + "dsd"));
        });
        Assert.Throws<NotSupportedException>(() => {
            var reader = new DataTblReader<RecordA>()
                .AddColumns((s, e) => s("dsd"));
        });
    }

    [Fact]
    public void AddColumns_OtherMember() {
        var ex = Assert.Throws<NotSupportedException>(() => {
            var date = DateTime.Now;
            var reader = new DataTblReader<RecordA>()
                .AddColumns((s, e) => s(date.Year));
        });
        Assert.Contains("Must be a member of ", ex.Message);
    }

    [Fact]
    public void AddColumns_SkipColumn() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
            },
            Rows = {
                { 1, "30" },
                { 2, "ss" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(s.Skip(1), e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal("30", list[0].Str);
        Assert.Equal("ss", list[1].Str);
    }

    [Fact]
    public void AddActionRead_Action() {
        var tbl = new DataTable() {
            Columns = {
                { "strValue", typeof(string) },
            },
            Rows = {
                { "30" },
                { "ss" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddActionRead(it => it.Entity.Str = it.Src.Rows[it.iRow][0].ToString() + "-")
            .AddColumns((s, e) => s(e.FieldStr));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal("30-", list[0].Str);
        Assert.Equal("ss-", list[1].Str);
        Assert.Equal("30", list[0].FieldStr);
        Assert.Equal("ss", list[1].FieldStr);
    }

    [Fact]
    public void AddActionRead_SkipRow() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
            },
            Rows = {
                { 1, "30" },
                { 2, "ss" },
                { 3, "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldInt))
            .AddActionRead(it => {
                var val = (string)it.Src.Rows[it.iRow][1];
                return it.SkipRow(!int.TryParse(val, out var _));
            })
            .AddColumns((s, e) => s(e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].FieldInt);
        Assert.Equal(3, list[1].FieldInt);
        Assert.Equal("30", list[0].Str);
        Assert.Equal("33", list[1].Str);
    }

    [Fact]
    public void AddActionRead_EndRow() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
            },
            Rows = {
                { 1, "30" },
                { 2, "ss" },
                { 3, "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldInt))
            .AddActionRead(it => {
                var val = (string)it.Src.Rows[it.iRow][1];
                return it.EndRow(!int.TryParse(val, out var _));
            })
            .AddColumns((s, e) => s(e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0].FieldInt);
        Assert.Equal(2, list[1].FieldInt);
        Assert.Equal(3, list[2].FieldInt);
        Assert.Equal("30", list[0].Str);
        Assert.Null(list[1].Str);
        Assert.Equal("33", list[2].Str);
    }

    [Fact]
    public void AddActionRead_EndTable() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
            },
            Rows = {
                { 1, "30" },
                { 2, "ss" },
                { 3, "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldInt))
            .AddActionRead(it => {
                var val = (string)it.Src.Rows[it.iRow][1];
                return it.EndTable(!int.TryParse(val, out var _));
            })
            .AddColumns((s, e) => s(e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Single(list);
        Assert.Equal(1, list[0].FieldInt);
        Assert.Equal("30", list[0].Str);
    }

    [Fact]
    public void AddColumnRead_Action() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
            },
            Rows = {
                { 1, "30" },
                { 2, "ss" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumnRead((int val) => it => it.Entity.StructInt = val + 10)
            .AddColumns((s, e) => s(e.FieldStr));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal(11, list[0].StructInt);
        Assert.Equal(12, list[1].StructInt);
        Assert.Equal("30", list[0].FieldStr);
        Assert.Equal("ss", list[1].FieldStr);
    }

    [Fact]
    public void AddColumnRead_SkipRow() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
                { "column3", typeof(string) },
            },
            Rows = {
                { 11, "21", "31" },
                { 12, "ss", "32" },
                { 13, "23", "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldInt))
            .AddColumnRead((string val) => it => {
                if (int.TryParse(val, out var cellVal)) {
                    it.Entity.StructInt = cellVal;
                    return null;
                }
                return it.SkipRow();
            })
            .AddColumns((s, e) => s(e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(2, list.Count);
        Assert.Equal(11, list[0].FieldInt);
        Assert.Equal(13, list[1].FieldInt);
        Assert.Equal(21, list[0].StructInt);
        Assert.Equal(23, list[1].StructInt);
        Assert.Equal("31", list[0].Str);
        Assert.Equal("33", list[1].Str);
    }

    [Fact]
    public void AddColumnRead_EndRow() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
                { "column3", typeof(string) },
            },
            Rows = {
                { 11, "21", "31" },
                { 12, "ss", "32" },
                { 13, "23", "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldInt))
            .AddColumnRead((string val) => it => {
                if (int.TryParse(val, out var intVal)) {
                    it.Entity.StructInt = intVal;
                    return null;
                }
                return it.EndRow();
            })
            .AddColumns((s, e) => s(e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Equal(3, list.Count);
        Assert.Equal(11, list[0].FieldInt);
        Assert.Equal(12, list[1].FieldInt);
        Assert.Equal(13, list[2].FieldInt);
        Assert.Equal(21, list[0].StructInt);
        Assert.Equal(00, list[1].StructInt);
        Assert.Equal(23, list[2].StructInt);
        Assert.Equal("31", list[0].Str);
        Assert.Null(list[1].Str);
        Assert.Equal("33", list[2].Str);
    }

    [Fact]
    public void AddColumnRead_EndTable() {
        var tbl = new DataTable() {
            Columns = {
                { "column1", typeof(int) },
                { "strValue", typeof(string) },
                { "column3", typeof(string) },
            },
            Rows = {
                { 11, "21", "31" },
                { 12, "ss", "32" },
                { 13, "23", "33" },
            }
        };
        var reader = new DataTblReader<RecordA>()
            .AddColumns((s, e) => s(e.FieldInt))
            .AddColumnRead((string val) => it => {
                if (int.TryParse(val, out var cellVal)) {
                    it.Entity.StructInt = cellVal;
                    return null;
                }
                return it.EndTable();
            })
            .AddColumns((s, e) => s(e.Str));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(tbl);
        Assert.Single(list);
        Assert.Equal(11, list[0].FieldInt);
        Assert.Equal(21, list[0].StructInt);
        Assert.Equal("31", list[0].Str);
    }

}
