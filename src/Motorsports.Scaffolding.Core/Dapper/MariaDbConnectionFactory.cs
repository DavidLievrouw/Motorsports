using System;
using System.Data;
using MySqlConnector;

namespace Motorsports.Scaffolding.Core.Dapper;

public class MariaDbConnectionFactory : IDbConnectionFactory {
  readonly string _connectionString;

  public MariaDbConnectionFactory(string connectionString) {
    _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
  }

  public IDbConnection CreateConnection() {
    return new MySqlConnection(_connectionString);
  }
}