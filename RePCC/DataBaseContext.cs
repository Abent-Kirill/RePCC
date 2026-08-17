using RePCC.Models;
using SQLite;

namespace RePCC;

public sealed class DataBaseContext
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _semaphore;
    private bool _isInit;

    public DataBaseContext()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "computers.db3");
        _database = new SQLiteAsyncConnection(databasePath);
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (!_isInit)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_isInit)
                {
                    await _database.CreateTableAsync<ComputerRecord>();
                    _isInit = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return _database;
    }
}
