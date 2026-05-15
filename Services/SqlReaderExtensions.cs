using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public static class SqlReaderExtensions
{
    public static string GetTrimmedString(this SqlDataReader reader, string name)
        => reader.HasColumn(name) && reader[name] is not DBNull ? Convert.ToString(reader[name])?.TrimEnd() ?? string.Empty : string.Empty;

    public static int GetInt32OrDefault(this SqlDataReader reader, string name)
    {
        if (!reader.HasColumn(name) || reader[name] is DBNull)
        {
            return 0;
        }

        var value = reader[name];
        if (value is int intValue)
        {
            return intValue;
        }

        if (value is decimal decimalValue)
        {
            return Convert.ToInt32(decimalValue);
        }

        return int.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    public static decimal GetDecimalOrDefault(this SqlDataReader reader, string name)
    {
        if (!reader.HasColumn(name) || reader[name] is DBNull)
        {
            return 0m;
        }

        var value = reader[name];
        if (value is decimal decimalValue)
        {
            return decimalValue;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        return decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }

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
