using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Interface
{
    public abstract class RepositoryBase
    {
        protected readonly IDatabaseService _db;

        protected RepositoryBase(IDatabaseService db)
        {
            _db = db;
        }

        // Execute (Insert, Update, Delete)
        protected Task ExecAsync(
            string sp,
            MySqlParameter[]? parameters = null,
            CancellationToken ct = default)
        {
            return _db.ExecuteAsync(
                sp,
                parameters,
                CommandType.StoredProcedure,
                ct);
        }

        // List Query
        protected async Task<IReadOnlyList<T>> QueryAsync<T>(
            string sp,
            MySqlParameter[]? parameters = null,
            CancellationToken ct = default)
            where T : class, new()
        {
            var dt = await _db.QueryAsync<DataTable>(
                sp,
                parameters,
                CommandType.StoredProcedure,
                ct);

            return MapTable<T>(dt);
        }

        // DataTable → Model List
        protected List<T> MapTable<T>(DataTable table)
            where T : class, new()
        {
            var list = new List<T>(table.Rows.Count);

            foreach (DataRow row in table.Rows)
                list.Add(MapRow<T>(row));

            return list;
        }

        // Reflection 기반 자동 매핑 + 캐싱
        private static readonly Dictionary<Type, PropertyInfo[]> _propCache = new();

        protected T MapRow<T>(DataRow row) where T : class, new()
        {
            var obj = new T();
            var props = typeof(T).GetProperties();

            foreach (DataColumn col in row.Table.Columns)
            {
                // Attribute 기반 매핑 먼저
                var prop = props.FirstOrDefault(p =>
                {
                    var attr = p.GetCustomAttribute<ColumnAttribute>();
                    if (attr != null)
                        return string.Equals(attr.Name, col.ColumnName, StringComparison.OrdinalIgnoreCase);

                    // Attribute 없으면 이름 비교
                    return string.Equals(p.Name, col.ColumnName, StringComparison.OrdinalIgnoreCase);
                });

                if (prop == null) continue;  // 매핑할 프로퍼티 없음

                var value = row[col];

                if (value == DBNull.Value)
                {
                    prop.SetValue(obj, null);
                    continue;
                }

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                // Enum 지원
                if (targetType.IsEnum)
                {
                    prop.SetValue(obj, Enum.Parse(targetType, value.ToString()!));
                    continue;
                }

                // 타입 변환
                prop.SetValue(obj, Convert.ChangeType(value, targetType));
            }

            return obj;
        }

        // Shortcut Parameter Builder
        protected static MySqlParameter P(string name, object? value, MySqlDbType type, int? size = null)
            => DatabaseService.Param(name, type, value, size);
    }
}
