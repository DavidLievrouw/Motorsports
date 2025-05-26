using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Motorsports.Scaffolding.Core.Dapper;

public class SqlDbConnectionFactory : IDbConnectionFactory {
  readonly string _connectionString;

  public SqlDbConnectionFactory(string connectionString) {
    _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
  }

  public IDbConnection CreateConnection() {
    return new SqlConnection(_connectionString);
  }
}