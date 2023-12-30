using System.Data;
using TableRW.Read;
using TableRW.Read.DataTableEx;

namespace TableRW.Tests.DataTableEx.Read;

public class DataTableExTest {

    class DataSrc_RecordA {
        public static DataTable GetDataTable() => new() {
            Columns = {
                { nameof(RecordA.FieldStr), typeof(string) },
                { nameof(RecordA.FieldInt), typeof(int) },
                { nameof(RecordA.Str), typeof(string) },
                { nameof(RecordA.StructInt), typeof(int) },
                { nameof(RecordA.NullableInt), typeof(int) },
            },
        };

        /// <summary>
        /// FieldStr, FieldInt, Str, StructInt, NullableInt
        /// </summary>
        public DataSrc_RecordA() {
            EntitySrc = new List<RecordA>() {
                new() { FieldStr = "fs1", FieldInt = 11, Str = "str1", StructInt = 12, NullableInt = null },
                new() { FieldStr = "fs2", FieldInt = 21, Str = "str2", StructInt = 22, NullableInt = 23 }
            };
            DataTable = GetDataTable();
            foreach (var row in EntitySrc) {
                DataTable.Rows.Add(row.FieldStr, row.FieldInt, row.Str, row.StructInt, row.NullableInt);
            }
        }

        public DataTable DataTable { get; }

        public IReadOnlyList<RecordA> EntitySrc { get; }

        public void CkeckDataCount(ICollection<RecordA> data) {
            Assert.Equal(EntitySrc.Count, data.Count);
        }

        public void CkeckData(IEnumerable<RecordA> data) {
            foreach (var (test, origin) in data.Zip(EntitySrc)) {
                test.TestIgnoreWrite();
                Assert.Equal(origin.FieldStr, test.FieldStr);
                Assert.Equal(origin.FieldInt, test.FieldInt);
                Assert.Equal(origin.Str, test.Str);
                Assert.Equal(origin.StructInt, test.StructInt);
                Assert.Equal(origin.NullableInt, test.NullableInt);
            }
        }

    }

    [Fact]
    public void ReadToList_UseHeader() {
        var src = new DataSrc_RecordA();
        var list = src.DataTable.ReadToList<RecordA>();
        src.CkeckDataCount(list);
        src.CkeckData(list);
    }

    [Fact]
    public void ReadToList_UseHeader_EmptyTable() {
        var tbl = DataSrc_RecordA.GetDataTable();
        var list = tbl.ReadToList<RecordA>();
        Assert.Empty(list);
    }

    [Fact]
    public void ReadToList_UseReader() {
        var src = new DataSrc_RecordA();
        var list = src.DataTable.ReadToList<RecordA>(cacheKey: 0, reader => {
            reader.AddColumns((s, e) =>
                s(e.FieldStr, e.FieldInt, e.Str, e.StructInt, e.NullableInt));

            var lmd = reader.Lambda();
            return lmd.Compile();
        });
        src.CkeckDataCount(list);
        src.CkeckData(list);
    }

    [Fact]
    public void ReadToList_UseReader_AnotherKey() {
        var src = new DataSrc_RecordA();
        var list1 = src.DataTable.ReadToList<RecordA>(cacheKey: 1, reader => {
            reader.AddColumns((s, e) =>
                s(e.FieldStr, e.FieldInt, e.Str, e.StructInt, e.NullableInt));

            var lmd = reader.Lambda();
            return lmd.Compile();
        });

        // 用另一个方式读取，缓存是不同的
        var list = src.DataTable.ReadToList<RecordA>(cacheKey: 2, reader => {
            reader.AddColumns((s, e) =>
                s(e.FieldStr, e.FieldInt, e.Str, e.StructInt, e.NullableInt))
                .OnEndReadingRow(it => {
                    it.Entity.FieldInt *= 1000;
                });

            var lmd = reader.Lambda();
            return lmd.Compile();
        });

        src.CkeckDataCount(list);

        foreach (var (test, origin) in list.Zip(src.EntitySrc)) {
            Assert.Equal(origin.FieldStr, test.FieldStr);
            Assert.Equal(origin.FieldInt * 1000, test.FieldInt);
            Assert.Equal(origin.Str, test.Str);
            Assert.Equal(origin.StructInt, test.StructInt);
            Assert.Equal(origin.NullableInt, test.NullableInt);
        }
    }

    [Fact]
    public void ReadToList_UseReader_WithData() {
        var src = new DataSrc_RecordA();
        var list = src.DataTable.ReadToList<RecordA, (int, int)>(cacheKey: 3, reader => {
            reader.AddColumns((s, e) =>
                s(e.FieldStr, e.FieldInt, e.Str, e.StructInt, e.NullableInt));

            var lmd = reader.Lambda();
            return lmd.Compile();
        });
        src.CkeckDataCount(list);
        src.CkeckData(list);
    }

}
