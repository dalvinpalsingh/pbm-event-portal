using System.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;

namespace PbmEventPortal.Web.Pages;

public class DbCheckModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DbCheckModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string StatusMessage { get; private set; } = "Database connection has not been checked yet.";
    public string? DatabaseName { get; private set; }
    public string? DatabaseUser { get; private set; }
    public int? RegionCount { get; private set; }
    public int? StateCount { get; private set; }
    public int? AuditLogCount { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                StatusMessage = "Database connection failed. The app could not connect to PostgreSQL.";
                return;
            }

            var connection = _dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            DatabaseName = await ExecuteScalarStringAsync("SELECT current_database();");
            DatabaseUser = await ExecuteScalarStringAsync("SELECT current_user;");
            RegionCount = await ExecuteScalarIntAsync("SELECT COUNT(*) FROM app.regions;");
            StateCount = await ExecuteScalarIntAsync("SELECT COUNT(*) FROM app.states;");
            AuditLogCount = await ExecuteScalarIntAsync("SELECT COUNT(*) FROM app.audit_logs;");

            StatusMessage = "Connected successfully and queried real PBM tables.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Database connection error: {ex.Message}";
        }
    }

    private async Task<string?> ExecuteScalarStringAsync(string sql)
    {
        var connection = _dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync();

        return result?.ToString();
    }

    private async Task<int> ExecuteScalarIntAsync(string sql)
    {
        var connection = _dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }
}