using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using TodoApi.Application.Common.Constants;
using Unleash;

namespace TodoApi.Infrastructure.Data.Interceptors;

public class ShadowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger _logger;
    private readonly IUnleash _unleash;
    private readonly ShadowDbContext _shadowDbContext;    

    public ShadowQueryInterceptor(
        IUnleash unleash,
        ShadowDbContext shadowDbContext,
        ILogger<ShadowDbContext> logger)
    {
        _logger = logger;
        _unleash = unleash;
        _shadowDbContext = shadowDbContext;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (_unleash.IsEnabled(UnleashConstants.UseShadowOperations))
        {
            LogQueryDetails(command, eventData, result);
        }            

        return new ValueTask<DbDataReader>(result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        return new(result);
    }

    private void LogQueryDetails(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        if (command.CommandText.StartsWith("SELECT"))
        {
            try
            {
                // Create the command.
                var newCommand = _shadowDbContext.Database.GetDbConnection().CreateCommand();

                foreach (DbParameter parameter in command.Parameters)
                {
                    var newParameter = newCommand.CreateParameter();
                    newParameter.ParameterName = parameter.ParameterName;
                    newParameter.Direction = parameter.Direction;
                    newParameter.DbType = parameter.DbType;
                    newParameter.Value = parameter.Value;
                    newCommand.Parameters.Add(newParameter);
                }

                newCommand.CommandText = command.CommandText;
                newCommand.CommandType = command.CommandType;

                // Open the connection.
                bool wasOpen = newCommand.Connection?.State == ConnectionState.Open;
                if (!wasOpen) newCommand.Connection?.Open();

                // Retrieve the data.
                using (var reader = newCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {                            
                            _logger.LogInformation("{0}: {1:C}, {2:C}, {3:C}", reader[0], reader[1], reader[2], reader[3]);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No rows found.");
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception.Message: {0}", ex.Message);
            }
        }
    }
}
