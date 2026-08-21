using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using RePCC.Models;

namespace RePCC;

public sealed class ComputersRepository
{
    // Вспомогательный метод для создания свежего контекста на каждый запрос
    private static DataBaseContext CreateContext() => new();

    public async Task<IReadOnlyCollection<ComputerRecord>> GetComputerRecordsAsync(CancellationToken cancellationToken = default)
    {
        // ИСПРАВЛЕНО: Оборачиваем в using, чтобы подключение закрывалось вовремя
        using var db = CreateContext();
        var result = await db.Computers.ToArrayAsync(cancellationToken);
        return result;
    }

    public async Task<int> AddAsync(ComputerRecord computerRecord, CancellationToken cancellationToken = default)
    {
        using var db = CreateContext();

        // ИСПРАВЛЕНО: В LinqToDB метод называется InsertOrUpdateAsync.
        // Он смотрит на атрибут [PrimaryKey] у MacAddress и делает UPSERT.
        var result = await db.InsertOrReplaceAsync(computerRecord, token: cancellationToken);
        return result;
    }

    public async Task<int> AddAsync(IEnumerable<ComputerRecord> computerRecords, CancellationToken cancellationToken = default)
    {
        using var db = CreateContext();

        var options = new BulkCopyOptions
        {
            BulkCopyType = BulkCopyType.MultipleRows
        };

        // Работает идеально: BulkCopyAsync атомарно и быстро запишет коллекцию.
        var result = await db.BulkCopyAsync(options, computerRecords, cancellationToken: cancellationToken);
        return (int)result.RowsCopied;
    }

    public async Task<int> DeleteAsync(ComputerRecord computerRecord, CancellationToken cancellationToken = default)
    {
        using var db = CreateContext();

        var result = await db.DeleteAsync(computerRecord, token: cancellationToken);
        return result;
    }
}
