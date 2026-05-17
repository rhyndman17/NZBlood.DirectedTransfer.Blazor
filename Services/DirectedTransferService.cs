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

    public async Task<IReadOnlyList<DirectedTransferSite>> GetPouSitesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select distinct
                   rtrim(i.LOCNCODE) LocationCode,
                   rtrim(isnull(pou.LOCNDSCR, '')) LocationName,
                   rtrim(isnull(s.eKanBanPickFromSite, '')) PickFromSite,
                   rtrim(isnull(pick.LOCNDSCR, '')) PickFromSiteName,
                   rtrim(isnull(s.SiteTransferEmailAddress, '')) SiteTransferEmailAddress
            from nzbDirectedTransferItems i
            left join IV40700 pou on pou.LOCNCODE=i.LOCNCODE
            left join nzbSiteOptions s on s.LocationCode=i.LOCNCODE
            left join IV40700 pick on pick.LOCNCODE=s.eKanBanPickFromSite
            where i.DirectedTransferItem=1 and i.LOCNCODE not like '%LG'
            order by rtrim(i.LOCNCODE)
            """;

        var sites = new List<DirectedTransferSite>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            sites.Add(new DirectedTransferSite
            {
                LocationCode = reader.GetTrimmedString("LocationCode"),
                LocationName = reader.GetTrimmedString("LocationName"),
                PickFromSite = reader.GetTrimmedString("PickFromSite"),
                PickFromSiteName = reader.GetTrimmedString("PickFromSiteName"),
                SiteTransferEmailAddress = reader.GetTrimmedString("SiteTransferEmailAddress")
            });
        }

        return sites;
    }

    public async Task<DirectedTransferSite?> GetPouSiteAsync(string pouSiteId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select top 1
                   rtrim(@pouSiteId) LocationCode,
                   rtrim(isnull(pou.LOCNDSCR, '')) LocationName,
                   rtrim(isnull(s.eKanBanPickFromSite, '')) PickFromSite,
                   rtrim(isnull(pick.LOCNDSCR, '')) PickFromSiteName,
                   rtrim(isnull(s.SiteTransferEmailAddress, '')) SiteTransferEmailAddress
            from nzbSiteOptions s
            left join IV40700 pou on rtrim(pou.LOCNCODE)=rtrim(s.LocationCode)
            left join IV40700 pick on rtrim(pick.LOCNCODE)=rtrim(s.eKanBanPickFromSite)
            where rtrim(s.LocationCode)=rtrim(@pouSiteId)
            """;

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@pouSiteId", pouSiteId);
        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DirectedTransferSite
        {
            LocationCode = reader.GetTrimmedString("LocationCode"),
            LocationName = reader.GetTrimmedString("LocationName"),
            PickFromSite = reader.GetTrimmedString("PickFromSite"),
            PickFromSiteName = reader.GetTrimmedString("PickFromSiteName"),
            SiteTransferEmailAddress = reader.GetTrimmedString("SiteTransferEmailAddress")
        };
    }

    public async Task<IReadOnlyList<DirectedTransferItem>> GetItemsAsync(string pickFromSiteId, string pouSiteId, CancellationToken cancellationToken = default)
    {
        var items = new List<DirectedTransferItem>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("nzbCalculateDirectedTransferWI", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 240
        };
        command.Parameters.AddWithValue("@pickFromSiteId", pickFromSiteId);
        command.Parameters.AddWithValue("@pouSiteId", pouSiteId);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var itemNumber = reader.GetTrimmedString("ITEMNMBR");
            items.Add(new DirectedTransferItem
            {
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
            .OrderBy(item => GetPrioritySortValue(item.Priority))
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
