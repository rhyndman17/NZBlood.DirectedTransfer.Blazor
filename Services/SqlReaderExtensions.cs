using System.Data;
using System.Data.SqlClient;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public static class SqlReaderExtensions
{
    public static string GetTrimmedString(this SqlDataReader reader, string name)
        => reader.HasColumn(name) && reader[name] is not DBNull ? Convert.ToString(reader[name])?.TrimEnd() ?? string.Empty : string.Empty;

    public static int GetInt32OrDefault(this SqlDataReader reader, string name)
        => reader.HasColumn(name) && reader[name] is not DBNull ? Convert.ToInt32(reader[name]) : 0;

    public static decimal GetDecimalOrDefault(this SqlDataReader reader, string name)
        => reader.HasColumn(name) && reader[name] is not DBNull ? Convert.ToDecimal(reader[name]) : 0m;

    public static bool HasColumn(this IDataRecord reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
