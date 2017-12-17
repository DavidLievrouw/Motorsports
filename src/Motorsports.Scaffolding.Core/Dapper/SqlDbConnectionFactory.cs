using System;
using System.Data;
using System.Data.SqlClient;

namespace Motorsports.Scaffolding.Core.Dapper {
  public class SqlDbConnectionFactory : IDbConnectionFactory {
    readonly string _connectionString;

    public SqlDbConnectionFactory(string connectionString) {
      _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection OpenConnection() {
      var connection = new SqlConnection(_connectionString);
      connection.Open();
      return connection;
    }
  }
}