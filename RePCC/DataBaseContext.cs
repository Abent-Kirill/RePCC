using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using RePCC.Models;

namespace RePCC;

public sealed partial class DataBaseContext : DataConnection
{
    private static readonly string _databasePath = Path.Combine(FileSystem.AppDataDirectory, "computers.db3");
    private static readonly string _connectionString = $"Data Source={_databasePath}";
    private static bool _isTableCreated;
    private static readonly Lock _syncRoot = new();

    public ITable<ComputerRecord> Computers => this.GetTable<ComputerRecord>();

    public DataBaseContext() : base(new DataOptions().UseSQLite(connectionString: _connectionString, provider: SQLiteProvider.Microsoft)) => EnsureTablesCreated();

    private void EnsureTablesCreated()
    {
        if (_isTableCreated) return;

        lock (_syncRoot)
        {
            if (_isTableCreated) return;

            // tableOptions: TableOptions.CheckExistence предотвращает ошибки, если таблица уже создана.
            this.CreateTable<ComputerRecord>(tableOptions: TableOptions.CheckExistence);
            _isTableCreated = true;
        }
    }
}
