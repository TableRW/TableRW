using System.Data;
using TableRW.Read;
using TableRW.Read.DataTableEx;

namespace TableRW.Tests.DataTableEx.Read;
public static class EventTest {

    public class StartReadingRow {

        [Fact]
        public void Action() {
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
                .AddColumns((s, e) => s(e.FieldStr))
                .OnStartReadingRow(it => {
                    Assert.True(it.PreEntity == null || it.PreEntity.FieldStr != null);
                    Assert.Null(it.Entity.FieldStr);
                    it.Entity.Str = it.Src.Rows[it.iRow][0] + "-";
                });

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
        public void SkipRow() {
            var tbl = new DataTable() {
                Columns = {
                    { "strValue", typeof(string) },
                },
                Rows = {
                    { "10" },
                    { "ss" },
                    { "30" },
                }
            };
            var reader = new DataTblReader<RecordA>()
                .AddColumns((s, e) => s(e.FieldStr))
                .OnStartReadingRow(it => {
                    Assert.Null(it.Entity.FieldStr);
                    it.Entity.Str = it.Src.Rows[it.iRow][0] + "-";
                    return it.SkipRow(it.iRow % 2 == 1);
                });

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Equal(2, list.Count);
            Assert.Equal("10", list[0].FieldStr);
            Assert.Equal("30", list[1].FieldStr);
        }

        [Fact]
        public void EndRow() {
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
                .OnStartReadingRow(it => {
                    Assert.Equal(0, it.Entity.FieldInt);
                    it.Entity.FieldStr = it.Src.Rows[it.iRow][1] + "-";
                    return it.EndRow(it.iRow % 2 == 1);
                })
                .AddColumns((s, e) => s(e.Str));

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Equal(3, list.Count);
            Assert.Equal("30-", list[0].FieldStr);
            Assert.Equal("ss-", list[1].FieldStr);
            Assert.Equal("33-", list[2].FieldStr);
            Assert.Equal(1, list[0].FieldInt);
            Assert.Equal(0, list[1].FieldInt);
            Assert.Equal(3, list[2].FieldInt);
            Assert.Equal("30", list[0].Str);
            Assert.Null(list[1].Str);
            Assert.Equal("33", list[2].Str);
        }

        [Fact]
        public void EndTable() {
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
                .OnStartReadingRow(it => {
                    Assert.Equal(0, it.Entity.FieldInt);
                    it.Entity.FieldStr = it.Src.Rows[it.iRow][1] + "-";
                    return it.EndTable(it.iRow >= 1);
                })
                .AddColumns((s, e) => s(e.Str));

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Single(list);
            Assert.Equal("30-", list[0].FieldStr);
            Assert.Equal(1, list[0].FieldInt);
            Assert.Equal("30", list[0].Str);
        }

        [Fact]
        public void SkipRow_OnEndReadingRow() {
            var tbl = new DataTable() {
                Columns = {
                    { "strValue", typeof(string) },
                },
                Rows = {
                    { "30" },
                }
            };
            var reader = new DataTblReader<RecordA>()
                .OnStartReadingRow(it => it.SkipRow())
                .AddColumns((s, e) => s(e.FieldStr))
                .OnEndReadingRow(it => {
                    throw new InvalidOperationException("SkipRow should not execute OnEndReadingRow event");
                });

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Empty(list);
        }

        [Fact]
        public void EndRow_OnEndReadingRow() {
            var tbl = new DataTable() {
                Columns = {
                    { "strValue", typeof(string) },
                },
                Rows = {
                    { "30" },
                }
            };
            var reader = new DataTblReader<RecordA>()
                .OnStartReadingRow(it => it.EndRow())
                .AddColumns((s, e) => s(e.FieldStr))
                .OnEndReadingRow(it => {
                    it.Entity.Str = "--";
                });

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Single(list);
            Assert.Null(list[0].FieldStr);
            Assert.Equal("--", list[0].Str);
        }

        [Fact]
        public void EndTable_OnEndReadingRow() {
            var tbl = new DataTable() {
                Columns = {
                    { "strValue", typeof(string) },
                },
                Rows = {
                    { "30" },
                }
            };
            var reader = new DataTblReader<RecordA>()
                .AddColumns((s, e) => s(e.FieldStr))
                .OnStartReadingRow(it => it.EndTable())
                .OnEndReadingRow(it => {
                    throw new InvalidOperationException("EndTable should not execute OnEndReadingRow event");
                });
            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Empty(list);
        }

    }

    public class EndReadingRow {

        [Fact]
        public void Action() {
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
                .OnEndReadingRow(it => {
                    Assert.NotNull(it.Entity.FieldStr);
                })
                .AddColumns((s, e) => s(e.FieldStr));
            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Equal(2, list.Count);
            Assert.Equal("30", list[0].FieldStr);
            Assert.Equal("ss", list[1].FieldStr);
        }

        [Fact]
        public void SkipRow() {
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
                .OnEndReadingRow(it => {
                    Assert.NotNull(it.Entity.FieldStr);
                    return it.SkipRow(it.iRow % 2 == 1);
                })
                .AddColumns((s, e) => s(e.FieldStr));

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Single(list);
            Assert.Equal("30", list[0].FieldStr);
        }

        [Fact]
        public void EndRow() {
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
                .OnEndReadingRow(it => {
                    Assert.NotEqual(0, it.Entity.FieldInt);
                    Assert.NotNull(it.Entity.Str);
                    return it.EndRow(); // nothing will happen
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
            Assert.Equal("ss", list[1].Str);
            Assert.Equal("33", list[2].Str);
        }

        [Fact]
        public void EndTable() {
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
                .OnEndReadingRow(it => {
                    Assert.NotEqual(0, it.Entity.FieldInt);
                    Assert.NotNull(it.Entity.Str);
                    return it.EndTable(it.iRow >= 1);
                })
                .AddColumns((s, e) => s(e.Str));

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Single(list);
            Assert.Equal(1, list[0].FieldInt);
            Assert.Equal("30", list[0].Str);
        }
    }

    public class StartReadingTable {

        [Fact]
        public void ContinueReading() {
            var tbl = new DataTable() {
                Columns = {
                    { "strValue", typeof(string) },
                },
                Rows = {
                    { "30" },
                    { "ss" },
                }
            };
            var reader = new DataTblReader<RecordA, bool>()
                .OnStartReadingTable(it => it.Data = true)
                .AddColumns((s, e) => s(e.FieldStr));

            var readLmd = reader.Lambda(f => f.ReturnData());
            var readFn = readLmd.Compile();
            var (list, data) = readFn(tbl);
            Assert.True(data);
            Assert.Equal(2, list.Count);
            Assert.Equal("30", list[0].FieldStr);
            Assert.Equal("ss", list[1].FieldStr);
        }

        [Fact]
        public void EndReading() {
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
                .OnStartReadingTable(it => false)
                .AddColumns((s, e) => s(e.FieldStr));

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Empty(list);
        }

    }

    public class EndReadingTable {

        [Fact]
        public void Action() {
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
                .AddColumns((s, e) => s(e.FieldStr))
                .OnEndReadingRow(it => {
                    it.Entity.FieldStr += "-";
                });

            var readLmd = reader.Lambda();
            var readFn = readLmd.Compile();
            var list = readFn(tbl);
            Assert.Equal(2, list.Count);
            Assert.Equal("30-", list[0].FieldStr);
            Assert.Equal("ss-", list[1].FieldStr);
        }
    }



}
