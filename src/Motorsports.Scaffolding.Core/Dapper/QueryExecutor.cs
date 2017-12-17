using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Motorsports.Scaffolding.Core.Dapper {
  public class QueryExecutor : IQueryExecutor{
    readonly int? _commandTimeout;
    readonly CommandType? _commandType;
    readonly IDbConnectionFactory _connectionFactory;
    readonly object _parameters;
    readonly string _sql;

    /// <summary>
    ///   Creates a new <see cref="QueryExecutor" />.
    /// </summary>
    /// <param name="connectionFactory">A <see cref="IDbConnectionFactory" /> to create a sql connection.</param>
    public QueryExecutor(IDbConnectionFactory connectionFactory) {
      if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
      _connectionFactory = connectionFactory;
    }

    QueryExecutor(IDbConnectionFactory connectionFactory, string sql, object parameters, CommandType? commandType, int? commandTimeout) : this(connectionFactory) {
      if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
      _sql = sql;
      _parameters = parameters;
      _commandType = commandType;
      _commandTimeout = commandTimeout;
    }

    /// <summary>
    ///   Creates a new <see cref="QueryExecutor" /> using the given SQL statement.
    /// </summary>
    /// <param name="sql">A SQL query.</param>
    /// <returns>A <see cref="QueryExecutor" />.</returns>
    public IQueryExecutor NewQuery(string sql) {
      return new QueryExecutor(_connectionFactory, sql, null, null, null);
    }

    /// <summary>
    ///   Creates a new <see cref="QueryExecutor" /> that uses the given parameters object to fill the SQL
    ///   query parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    public IQueryExecutor WithParameters(object parameters) {
      return new QueryExecutor(_connectionFactory, _sql, parameters, _commandType, _commandTimeout);
    }

    /// <summary>
    ///   Creates a new <see cref="QueryExecutor" /> that uses the given collection to fill the SQL query
    ///   parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    public IQueryExecutor WithParameters(IEnumerable<KeyValuePair<string, object>> parameters) {
      var dynamicParameters = new DynamicParameters();
      foreach (var entry in parameters) {
        dynamicParameters.Add(entry.Key, entry.Value);
      }
      return WithParameters(dynamicParameters);
    }

    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> that uses the given command type to execute queries
    /// </summary>
    /// <param name="commandType">The command type that will be used when executing the query.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    public IQueryExecutor WithCommandType(CommandType commandType) {
      return new QueryExecutor(_connectionFactory, _sql, _parameters, commandType, _commandTimeout);
    }

    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> that uses the given command timeout to execute
    ///   queries
    /// </summary>
    /// <param name="commandTimeout">The command timeout that will be used when executing the query.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    public IQueryExecutor  WithCommandTimeout(int commandTimeout) {
      return new QueryExecutor(_connectionFactory, _sql, _parameters, _commandType, commandTimeout);
    }

    #region Execute

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> Execute<TResult>() {
      return ExecuteOnConnection(c => c.Query<TResult>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAnonymousResult<TResult>(TResult anonymousPrototype) {
      return ExecuteOnConnection(c => c.Query<TResult>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>The <typeparamref name="TResult" />.</returns>
    public TResult ExecuteScalar<TResult>() {
      return ExecuteOnConnection(c => c.ExecuteScalar<TResult>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query and does not return the result.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    public int Execute() {
      return ExecuteOnConnection(c => c.Execute(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TData, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TData, TResult}" />.</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TData, TResult>(Func<TData, TResult> adapter) {
      return ExecuteOnConnection(c => {
        var data = c.Query<TData>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout);
        return data.Select(adapter);
      });
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}" /> and returns the
    ///   result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}" /> and returns
    ///   the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}" /> and
    ///   returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping
    ///   <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TSeventh"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">
    ///   The mapping
    ///   <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}" />.
    /// </param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnection(c => c.Query(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    #endregion

    #region ExecuteAsync

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteAsync<TResult>() {
      return ExecuteOnConnectionAsync(c => c.QueryAsync<TResult>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAnonymousResultAsync<TResult>(TResult anonymousPrototype) {
      return ExecuteOnConnectionAsync(c => c.QueryAsync<TResult>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>The <typeparamref name="TResult" />.</returns>
    public Task<TResult> ExecuteScalarAsync<TResult>() {
      return ExecuteOnConnectionAsync(c => c.ExecuteScalarAsync<TResult>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query and does not return the result.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    public Task<int> ExecuteAsync() {
      return ExecuteOnConnectionAsync(c => c.ExecuteAsync(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TData, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TData, TResult}" />.</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TData, TResult>(Func<TData, TResult> adapter) {
      return ExecuteOnConnectionAsync(async c => {
        var data = await c.QueryAsync<TData>(_sql, _parameters, commandType: _commandType, commandTimeout: _commandTimeout);
        return data.Select(adapter);
      });
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}" /> and returns the
    ///   result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}" /> and returns
    ///   the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}" /> and
    ///   returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> adapter, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    /// <summary>
    ///   Executes the query using a mapping
    ///   <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TSeventh"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">
    ///   The mapping
    ///   <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}" />.
    /// </param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    public Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> adapter,
      string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_sql, adapter, _parameters, commandType: _commandType, splitOn: splitOn, commandTimeout: _commandTimeout));
    }

    #endregion

    #region ExecuteOnConnection

    IEnumerable<TResult> ExecuteOnConnection<TResult>(Func<IDbConnection, IEnumerable<TResult>> execute) {
      using (var connection = _connectionFactory.OpenConnection()) {
        return execute(connection);
      }
    }

    TResult ExecuteOnConnection<TResult>(Func<IDbConnection, TResult> execute) {
      using (var connection = _connectionFactory.OpenConnection()) {
        return execute(connection);
      }
    }

    async Task<IEnumerable<TResult>> ExecuteOnConnectionAsync<TResult>(Func<IDbConnection, Task<IEnumerable<TResult>>> execute) {
      using (var connection = _connectionFactory.OpenConnection()) {
        return await execute(connection);
      }
    }

    async Task<TResult> ExecuteOnConnectionAsync<TResult>(Func<IDbConnection, Task<TResult>> execute) {
      using (var connection = _connectionFactory.OpenConnection()) {
        return await execute(connection);
      }
    }

    #endregion
  }
}