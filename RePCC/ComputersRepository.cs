using RePCC.Models;

namespace RePCC;

public sealed class ComputersRepository(DataBaseContext dataBaseContext)
{
    public async Task<IReadOnlyCollection<ComputerRecord>> GetComputerRecordsAsync()
    {
        var db = await dataBaseContext.GetDatabaseAsync();
        return await db.Table<ComputerRecord>().ToArrayAsync();
    }

    public async Task<int> AddAsync(ComputerRecord computerRecord)
    {
        var db = await dataBaseContext.GetDatabaseAsync();
        return await db.InsertOrReplaceAsync(computerRecord);
    }

    public async Task<int> AddAsync(IEnumerable<ComputerRecord> computerRecords)
    {
        var db = await dataBaseContext.GetDatabaseAsync();
        var count = 0;
        await db.RunInTransactionAsync(tran =>
        {
            foreach (var record in computerRecords)
            {
                count += tran.InsertOrReplace(record);
            }
        });
        return count;
    }
    public async Task<int> DeleteAsync(ComputerRecord computerRecord)
    {
        var db = await dataBaseContext.GetDatabaseAsync();
        return await db.DeleteAsync(computerRecord);
    }

}
