using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Motorsports.Scaffolding.Core.Dapper {
  public interface ITransactionalQueryExecutor : IQueryExecutor, IDisposable {
    void Commit();
    void Rollback();
  }

  public class TransactionalQueryExecutor : ITransactionalQueryExecutor {
    readonly IDbTransaction _transaction;
    readonly Query _query;

    public TransactionalQueryExecutor(IDbTransaction transaction) {
      _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    TransactionalQueryExecutor(IDbTransaction transaction, Query query): this(transaction) {
      _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public ITransactionalQueryExecutor BeginTransaction() {
      // will fail if the connection is already disposed, but that's not our responsibility here.
      var connection = _transaction.Connection;
      var newTransaction = connection.BeginTransaction();
      return new TransactionalQueryExecutor(newTransaction);
    }

    public IQueryExecutor NewQuery(string sql) {
      return new TransactionalQueryExecutor(_transaction, new Query(sql));
    }

    public IQueryExecutor WithParameters(object parameters) {
      return new TransactionalQueryExecutor(_transaction, _query.WithParameters(parameters));
    }

    public IQueryExecutor WithParameters(IEnumerable<KeyValuePair<string, object>> parameters) {
      return new TransactionalQueryExecutor(_transaction, _query.WithParameters(parameters));
    }

    public IQueryExecutor WithCommandType(CommandType commandType) {
      return new TransactionalQueryExecutor(_transaction, _query.WithCommandType(commandType));
    }

    #region Execute

    public IEnumerable<TResult> Execute<TResult>() {
      return ExecuteOnConnection(c => c.Query<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }

    public IEnumerable<TResult> ExecuteWithAnonymousResult<TResult>(TResult anonymousPrototype) {
      return ExecuteOnConnection(c => c.Query<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }

    public TResult ExecuteScalar<TResult>() {
      return ExecuteOnConnection(c => c.ExecuteScalar<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }

    public int Execute() {
      return ExecuteOnConnection(c => c.Execute(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }

    public IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    public IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    public IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    public IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    public IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    public IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    #endregion

    #region ExecuteAsync

    public Task<IEnumerable<TResult>> ExecuteAsync<TResult>() {
      return ExecuteOnConnectionAsync(c => c.QueryAsync<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType, transaction: _transaction));
    }

    public Task<IEnumerable<dynamic>> ExecuteDynamicAsync() {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, _query.Parameters, commandType: _query.CommandType, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithAnonymousResultAsync<TResult>(TResult anonymousPrototype) {
      return ExecuteOnConnectionAsync(c => c.QueryAsync<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType, transaction: _transaction));
    }

    public Task<TResult> ExecuteScalarAsync<TResult>() {
      return ExecuteOnConnectionAsync(c => c.ExecuteScalarAsync<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType, transaction: _transaction));
    }

    public Task<int> ExecuteAsync() {
      return ExecuteOnConnectionAsync(c => c.ExecuteAsync(_query.Sql, _query.Parameters, commandType: _query.CommandType, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TResult>(
      Func<TFirst, TSecond, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TResult>(
      Func<TFirst, TSecond, TThird, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn, transaction: _transaction));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh,
      TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> mapper,
      string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn, transaction: _transaction));
    }

    #endregion

    #region ExecuteOnConnection

    IEnumerable<TResult> ExecuteOnConnection<TResult>(Func<IDbConnection, IEnumerable<TResult>> execute) {
      return execute(_transaction.Connection);
    }

    TResult ExecuteOnConnection<TResult>(Func<IDbConnection, TResult> execute) {
      return execute(_transaction.Connection);
    }

    async Task<IEnumerable<TResult>> ExecuteOnConnectionAsync<TResult>(Func<IDbConnection, Task<IEnumerable<TResult>>> execute) {
      return await execute(_transaction.Connection);
    }

    async Task<TResult> ExecuteOnConnectionAsync<TResult>(Func<IDbConnection, Task<TResult>> execute) {
      return await execute(_transaction.Connection);
    }

    #endregion

    public void Dispose() {
      _transaction?.Dispose();
      _transaction?.Connection?.Dispose();
    }

    public void Commit() {
      _transaction.Commit();
    }

    public void Rollback() {
      _transaction.Rollback();
    }
  }
}