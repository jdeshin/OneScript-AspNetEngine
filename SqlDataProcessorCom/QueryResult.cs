using System.Globalization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data.Common;

using System.Runtime.InteropServices;

namespace OScriptSql.Com
{
 
    [ComVisible(true)]
    [Guid("82D6CCDD-3907-4173-AC2F-0C9B63B642A5")]
    public interface QueryResultInterface
    {
        // Свойства
        int ColumnsCount { get; }
        int RowsCount { get; }


        // Методы
        [DispId(1)]
        void Unload();
        [DispId(2)]
        void ClearResults();
        [DispId(3)]
        string GetColumnName(int index);
        [DispId(4)]
        object GetCellValue(int rowIndex, int columnIndex);
        [DispId(5)]
        bool IsBinaryData(int rowIndex, int columnIndex);
    }

    [ComVisible(true)]
    [Guid("82D6CCDD-3907-4173-AC2F-0C9B63B642A5"),
        InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface QueryResultEvents
    {
    }

    /// <summary>
    /// Содержит результат выполнения запроса. Предназначен для хранения и обработки полученных данных.
    /// </summary>
    //[ContextClass("РезультатЗапроса", "QueryResult")]
    //class QueryResult : AutoContext<QueryResult>
    [ComVisible(true)]
    [Guid("C716F8DB-C299-4BBD-B090-623657B83182"),
    ClassInterface(ClassInterfaceType.None),
    ComSourceInterfaces(typeof(QueryResultEvents))]
    public class QueryResult : QueryResultInterface
    {
        private DbDataReader _reader;
        System.Data.DataTable _resultTable;

        public QueryResult()
        {
        }


        public QueryResult(DbDataReader reader)
        {
            _reader = reader;
            _resultTable = new System.Data.DataTable();
            
        }

        /// <summary>
        /// Создает таблицу значений и копирует в нее все записи набора.
        /// </summary>
        /// <returns>ТаблицаЗначений</returns>
        //[ContextMethod("Выгрузить", "Unload")]
        //public ValueTable Unload()
        // Заполняет таблицу значений из reader
        public void Unload()
        {
            _resultTable.Clear();
            _resultTable.Load(_reader);
            /*
                        ValueTable resultTable = new ValueTable();

                        for (int ColIdx = 0; ColIdx < _reader.FieldCount; ColIdx++)
                        {
                            resultTable.Columns.Add(_reader.GetName(ColIdx));
                        }

                        foreach (DbDataRecord record in _reader)
                        {
                            ValueTableRow row = resultTable.Add();

                            for (int ColIdx = 0; ColIdx < _reader.FieldCount; ColIdx++)
                            {
                                if (record.IsDBNull(ColIdx))
                                {
                                    row.Set(ColIdx, ValueFactory.Create());
                                    continue;
                                }

                                //Console.WriteLine("queryresult-col-type:" + record.GetFieldType(ColIdx).ToString() + "::" + record.GetDataTypeName(ColIdx));

                                if (record.GetFieldType(ColIdx) == typeof(Int32))
                                {
                                    row.Set(ColIdx, ValueFactory.Create((int)record.GetValue(ColIdx)));
                                }
                                if (record.GetFieldType(ColIdx) == typeof(Int64))
                                {
                                    row.Set(ColIdx, ValueFactory.Create(record.GetInt64(ColIdx)));
                                }
                                if (record.GetFieldType(ColIdx) == typeof(Boolean))
                                {
                                    row.Set(ColIdx, ValueFactory.Create(record.GetBoolean(ColIdx)));
                                }
                                if (record.GetFieldType(ColIdx) == typeof(UInt64))
                                {
                                    row.Set(ColIdx, ValueFactory.Create(record.GetValue(ColIdx).ToString()));
                                }

                                if (record.GetFieldType(ColIdx).ToString() == "System.Double")
                                {
                                    double val = record.GetDouble(ColIdx);
                                    row.Set(ColIdx, ValueFactory.Create(val.ToString()));
                                }
                                if (record.GetFieldType(ColIdx) == typeof(Single))
                                {
                                    float val = record.GetFloat(ColIdx);
                                    row.Set(ColIdx, ValueFactory.Create(val.ToString()));
                                }
                                if (record.GetFieldType(ColIdx) == typeof(Decimal))
                                {
                                    row.Set(ColIdx, ValueFactory.Create(record.GetDecimal(ColIdx)));
                                }
                                if (record.GetFieldType(ColIdx).ToString() == "System.String")
                                {
                                    row.Set(ColIdx, ValueFactory.Create(record.GetString(ColIdx)));
                                }
                                if (record.GetFieldType(ColIdx).ToString() == "System.DateTime")
                                {
                                    row.Set(ColIdx, ValueFactory.Create(record.GetDateTime(ColIdx)));
                                }
                                if (record.GetFieldType(ColIdx).ToString() == "System.Byte[]")
                                {
                                    var data = (byte[])record[ColIdx];
                                    var newData = new BinaryDataContext(data);
                                    row.Set(ColIdx, ValueFactory.Create(newData));
                                }
                            }
                        }*/
            _reader.Close();
        }

        // Очищает таблицу с данными
        public void ClearResults()
        {
            _resultTable.Clear();
        }

        public string GetColumnName(int index)
        {
            return _resultTable.Columns[index].ColumnName;
        }

        public object GetCellValue(int rowIndex, int columnIndex)
        {
            if (IsBinaryData(rowIndex, columnIndex))
            {
                return Convert.ToBase64String((byte [])_resultTable.Rows[rowIndex][columnIndex]);
            }
            else
                return _resultTable.Rows[rowIndex][columnIndex];
        }

        public bool IsBinaryData(int rowIndex, int columnIndex)
        {
            return (_resultTable.Rows[rowIndex][columnIndex].GetType() == typeof(byte [])) ||
                (_resultTable.Rows[rowIndex][columnIndex].GetType() == typeof(System.Byte[]));
        }

        public int RowsCount
        {
            get
            {
                return _resultTable.Rows.Count;
            }
        }

        public int ColumnsCount
        {
            get
            {
                return _resultTable.Columns.Count;
            }
        }

    }
}
