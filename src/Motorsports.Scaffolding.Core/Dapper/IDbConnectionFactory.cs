using System.Data;

namespace Motorsports.Scaffolding.Core.Dapper {
  public interface IDbConnectionFactory {
    IDbConnection OpenConnection();
  }
}