using System.Data;
using System.Data.SqlClient;
using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public sealed class DirectedTransferService : IDirectedTransferService
{
    private readonly string _connectionString;

    public DirectedTransferService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DirectedTransfer")
            ?? throw new InvalidOperationException("Connection string 'DirectedTransfer' is not configured.");
    }

    public async Task<IReadOnlyList<DirectedTransferOrderForm>> GetOrderFormsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select rtrim(f.OrderFormID) OrderFormID,
                   rtrim(isnull(f.Description, '')) Description,
                   rtrim(f.PointOfUseSite) LocationCode,
                   rtrim(isnull(pou.LOCNDSCR, '')) LocationName,
                   rtrim(f.PickFromSite) PickFromSite,
                   rtrim(isnull(pick.LOCNDSCR, '')) PickFromSiteName,
                   rtrim(isnull(s.SiteTransferEmailAddress, '')) SiteTransferEmailAddress
            from nzbDirectedTransferOrderForms f
            left join IV40700 pou on rtrim(pou.LOCNCODE)=rtrim(f.PointOfUseSite)
            left join nzbSiteOptions s on rtrim(s.LocationCode)=rtrim(f.PointOfUseSite)
            left join IV40700 pick on rtrim(pick.LOCNCODE)=rtrim(f.PickFromSite)
            where isnull(f.Inactive, 0)=0
            order by rtrim(f.OrderFormID)
            """;

        var forms = new List<DirectedTransferOrderForm>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            forms.Add(new DirectedTransferOrderForm
            {
                OrderFormId = reader.GetTrimmedString("OrderFormID"),
                Description = reader.GetTrimmedString("Description"),
                Site = new DirectedTransferSite
                {
                    LocationCode = reader.GetTrimmedString("LocationCode"),
                    LocationName = reader.GetTrimmedString("LocationName"),
                    PickFromSite = reader.GetTrimmedString("PickFromSite"),
                    PickFromSiteName = reader.GetTrimmedString("PickFromSiteName"),
                    SiteTransferEmailAddress = reader.GetTrimmedString("SiteTransferEmailAddress")
                }
            });
        }

        return forms;
    }

    public async Task<IReadOnlyList<DirectedTransferItem>> GetItemsAsync(string orderFormId, string pickFromSiteId, string pouSiteId, CancellationToken cancellationToken = default)
    {
        var items = new List<DirectedTransferItem>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("nzbCalculateDirectedTransferWI", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 240
        };
        command.Parameters.AddWithValue("@orderFormId", orderFormId);
        command.Parameters.AddWithValue("@pickFromSiteId", pickFromSiteId);
        command.Parameters.AddWithValue("@pouSiteId", pouSiteId);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var itemNumber = reader.GetTrimmedString("ITEMNMBR");
            items.Add(new DirectedTransferItem
            {
                Zone = reader.GetTrimmedString("ZONE"),
                Priority = reader.GetInt32OrDefault("DirectedTransferPriority"),
                ItemNumber = itemNumber,
                ItemDescription = reader.GetTrimmedString("ITEMDESC"),
                UnitOfMeasure = reader.GetTrimmedString("BASEUOFM"),
                QtyBaseUom = Convert.ToInt32(reader.GetDecimalOrDefault("QTYBSUOM")),
                UomLongDescription = reader.GetTrimmedString("UOFMLONGDESC"),
                UomSchedule = reader.GetTrimmedString("UOMSCHDL"),
                BaseUom = reader.GetTrimmedString("BASEUOFM"),
                QtyPending = Convert.ToInt32(reader.GetDecimalOrDefault("PanaPickingPOUQty")),
                QtyAvailable = Convert.ToInt32(reader.GetDecimalOrDefault("QtyAvailable")),
                OrderUpToLevel = Convert.ToInt32(reader.GetDecimalOrDefault("ORDRUPTOLVL")),
                QtyToOrder = 0
            });
        }

        foreach (var item in items)
        {
            item.VendorItemNumber = await GetPrimaryVendorItemNumberAsync(item.ItemNumber, cancellationToken);
        }

        return items
            .OrderBy(item => item.Zone, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => GetPrioritySortValue(item.Priority))
            .ThenBy(item => item.ItemNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<string> CreateTransferAsync(UserContext user, DirectedTransferSite site, string orderReference, IReadOnlyList<DirectedTransferItem> items, CancellationToken cancellationToken = default)
    {
        var orderLines = items.Where(item => item.QtyToOrder != 0).ToList();
        if (orderLines.Count == 0)
        {
            throw new InvalidOperationException("No quantities have been ordered.");
        }

        var id = Guid.NewGuid().ToString();
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            await ExecuteNonQueryAsync(connection, transaction, """
                insert into nzbDirectedTransferHdr
                (TransOrderCode, PickSiteID, POUSiteID, DocDate, DueDate, UserID, PrintOnly, Id, Reference)
                values ('***pending***', @pickSite, @pouSite, @docDate, @docDate, @userId, 0, @id, @reference)
                """, cancellationToken,
                new SqlParameter("@pickSite", site.PickFromSite),
                new SqlParameter("@pouSite", site.LocationCode),
                new SqlParameter("@docDate", DateTime.Today),
                new SqlParameter("@userId", user.UserId),
                new SqlParameter("@id", id),
                new SqlParameter("@reference", orderReference));

            var lineSeq = 1;
            foreach (var item in orderLines)
            {
                await ExecuteNonQueryAsync(connection, transaction, """
                    insert into nzbDirectedTransferLne
                    (TransOrderCode, LineItemSeq, ItemNumber, ItemDescription, QtyOrder, UnitOfMeasure, BaseUOFM, QTYBaseUOFM, Id)
                    values ('***pending***', @lineSeq, @itemNumber, @itemDescription, @qtyOrder, @unitOfMeasure, @baseUom, @qtyBaseUom, @id)
                    """, cancellationToken,
                    new SqlParameter("@lineSeq", lineSeq),
                    new SqlParameter("@itemNumber", item.ItemNumber),
                    new SqlParameter("@itemDescription", item.ItemDescription),
                    new SqlParameter("@qtyOrder", item.QtyToOrder),
                    new SqlParameter("@unitOfMeasure", item.UnitOfMeasure),
                    new SqlParameter("@baseUom", item.BaseUom),
                    new SqlParameter("@qtyBaseUom", item.QtyBaseUom),
                    new SqlParameter("@id", id));

                await ExecuteNonQueryAsync(connection, transaction, """
                    insert into nzbDirectedTransferEmailLne
                    (TransOrderCode, LineStatus, ItemNumber, ItemDescription, QtyOrder, UnitOfMeasure, Id)
                    values ('***pending***', 0, @itemNumber, @itemDescription, @qtyOrder, @unitOfMeasure, @id)
                    """, cancellationToken,
                    new SqlParameter("@itemNumber", item.ItemNumber),
                    new SqlParameter("@itemDescription", item.ItemDescription),
                    new SqlParameter("@qtyOrder", item.QtyToOrder),
                    new SqlParameter("@unitOfMeasure", item.UnitOfMeasure),
                    new SqlParameter("@id", id));

                lineSeq++;
            }

            transaction.Commit();
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Preserve the original SQL exception for the UI.
            }

            throw;
        }

        await ExecuteCreateTransferProcedureAsync(id, cancellationToken);
        return await GetTransferOrderCodeAsync(id, cancellationToken);
    }

    private async Task<string> GetPrimaryVendorItemNumberAsync(string itemNumber, CancellationToken cancellationToken)
    {
        const string sql = "select top 1 rtrim(VNDITNUM) from IV00103 where ITEMNMBR=@itemNumber and ITMVNDTY=1";
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@itemNumber", itemNumber);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? string.Empty : Convert.ToString(result) ?? string.Empty;
    }

    private static int GetPrioritySortValue(int priority)
        => priority <= 0 ? int.MaxValue : priority;

    private async Task ExecuteCreateTransferProcedureAsync(string id, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("execute nzbCreateDirectedTransfer @id", connection) { CommandTimeout = 600 };
        command.Parameters.AddWithValue("@id", id);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<string> GetTransferOrderCodeAsync(string id, CancellationToken cancellationToken)
    {
        const string sql = "select top 1 rtrim(TransOrderCode) from nzbDirectedTransferHdr where Id=@id";
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? string.Empty : Convert.ToString(result) ?? string.Empty;
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, params SqlParameter[] parameters)
    {
        using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 600 };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
