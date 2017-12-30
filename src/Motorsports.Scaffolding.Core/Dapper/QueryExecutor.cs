using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Motorsports.Scaffolding.Core.Dapper {
  /// <summary>
  /// Abstraction for database query execution.
  /// </summary>
  public interface IQueryExecutor {
    /// <summary>
    /// Begins a new database transaction and returns a <see cref="ITransactionalQueryExecutor"/> to execute queries within this transaction.
    /// </summary>
    /// <returns>A <see cref="ITransactionalQueryExecutor"/></returns>
    ITransactionalQueryExecutor BeginTransaction();

    /// <summary>
    /// Creates a new <see cref="IQueryExecutor"/> using the given SQL statement.
    /// </summary>
    /// <param name="sql">A SQL query.</param>
    /// <returns>A <see cref="IQueryExecutor"/>.</returns>
    IQueryExecutor NewQuery(string sql);

    /// <summary>
    /// Creates a new <see cref="IQueryExecutor"/> that uses the given object to fill the SQL query parameters.
    /// </summary>
    /// <param name="parameters">The parameters object.</param>
    /// <returns>A <see cref="IQueryExecutor"/>.</returns>
    IQueryExecutor WithParameters(object parameters);

    /// <summary>
    /// Creates a new <see cref="IQueryExecutor"/> that uses the given collection to fill the SQL query parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <returns>A <see cref="IQueryExecutor"/>.</returns>
    IQueryExecutor WithParameters(IEnumerable<KeyValuePair<string, object>> parameters);

    /// <summary>
    /// Creates a new <see cref="IQueryExecutor"/> that uses the given command type to execute queries
    /// </summary>
    /// <param name="commandType">The command type that will be used when executing the query.</param>
    /// <returns>A <see cref="IQueryExecutor"/>.</returns>
    IQueryExecutor WithCommandType(CommandType commandType);

    
    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> Execute<TResult>();

    /// <summary>
    /// Executes the query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>The <typeparamref name="TResult"/>.</returns>
    TResult ExecuteScalar<TResult>();

    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithAnonymousResult<TResult>(TResult anonymousPrototype);

    /// <summary>
    /// Executes the query and does not return the result.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    int Execute();

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TSeventh"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    IEnumerable<TResult> ExecuteWithMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteAsync<TResult>();

    /// <summary>
    /// Executes the query and returns the results as dynamic records.
    /// </summary>
    /// <returns>A collection of dynamic records.</returns>
    Task<IEnumerable<dynamic>> ExecuteDynamicAsync();

    /// <summary>
    /// Executes the query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>The <typeparamref name="TResult"/>.</returns>
    Task<TResult> ExecuteScalarAsync<TResult>();

    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithAnonymousResultAsync<TResult>(TResult anonymousPrototype);

    /// <summary>
    /// Executes the query and returns the number of affected rows
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    Task<int> ExecuteAsync();

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> mapper,
      string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TResult>(
      Func<TFirst, TSecond, TThird, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> mapper, string splitOn = "Id");

    /// <summary>
    /// Executes the query using a mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}"/> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    /// <typeparam name="TFourth"></typeparam>
    /// <typeparam name="TFifth"></typeparam>
    /// <typeparam name="TSixth"></typeparam>
    /// <typeparam name="TSeventh"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult}"/>.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult"/>.</returns>
    Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh,
      TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> mapper, string splitOn = "Id");
  }

  public class QueryExecutor : IQueryExecutor {
    readonly IDbConnectionFactory _connectionFactory;
    readonly Query _query;

    /// <summary>
    /// Creates a new <see cref="QueryExecutor"/>.
    /// </summary>
    /// <param name="connectionFactory">A <see cref="IDbConnectionFactory"/> to create database connections when necessary.</param>
    public QueryExecutor(IDbConnectionFactory connectionFactory) {
      _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    QueryExecutor(IDbConnectionFactory connectionFactory, Query query) : this(connectionFactory) {
      _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public ITransactionalQueryExecutor BeginTransaction() {
      var connection = _connectionFactory.CreateConnection();
      connection.Open();
      var transaction = connection.BeginTransaction();
      return new TransactionalQueryExecutor(transaction);;
    }
    
    public IQueryExecutor NewQuery(string sql) {
      return new QueryExecutor(_connectionFactory, new Query(sql));
    }

    public IQueryExecutor WithParameters(object parameters) {
      return new QueryExecutor(_connectionFactory, _query.WithParameters(parameters));
    }
    
    public IQueryExecutor WithParameters(IEnumerable<KeyValuePair<string, object>> parameters) {
      return new QueryExecutor(_connectionFactory, _query.WithParameters(parameters));
    }
    
    public IQueryExecutor WithCommandType(CommandType commandType) {
      return new QueryExecutor(_connectionFactory, _query.WithCommandType(commandType));
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
      return ExecuteOnConnectionAsync(c => c.QueryAsync<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }

    public Task<IEnumerable<dynamic>> ExecuteDynamicAsync() {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }

    public Task<IEnumerable<TResult>> ExecuteWithAnonymousResultAsync<TResult>(TResult anonymousPrototype) {
      return ExecuteOnConnectionAsync(c => c.QueryAsync<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }
    
    public Task<TResult> ExecuteScalarAsync<TResult>() {
      return ExecuteOnConnectionAsync(c => c.ExecuteScalarAsync<TResult>(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }
    
    public Task<int> ExecuteAsync() {
      return ExecuteOnConnectionAsync(c => c.ExecuteAsync(_query.Sql, _query.Parameters, commandType: _query.CommandType));
    }
    
    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TResult>(
      Func<TFirst, TSecond, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }
    
    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TResult>(
      Func<TFirst, TSecond, TThird, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }
    
    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }
    
    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }
    
    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(
      Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> mapper, string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    public Task<IEnumerable<TResult>> ExecuteWithMapperAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh,
      TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> mapper,
      string splitOn = "Id") {
      return ExecuteOnConnectionAsync(c => c.QueryAsync(_query.Sql, mapper, _query.Parameters, commandType: _query.CommandType, splitOn: splitOn));
    }

    #endregion

    #region ExecuteOnConnection

    IEnumerable<TResult> ExecuteOnConnection<TResult>(Func<IDbConnection, IEnumerable<TResult>> execute) {
      using (var connection = _connectionFactory.CreateConnection()) {
        return execute(connection);
      }
    }

    TResult ExecuteOnConnection<TResult>(Func<IDbConnection, TResult> execute) {
      using (var connection = _connectionFactory.CreateConnection()) {
        return execute(connection);
      }
    }

    async Task<IEnumerable<TResult>> ExecuteOnConnectionAsync<TResult>(
      Func<IDbConnection, Task<IEnumerable<TResult>>> execute) {
      using (var connection = _connectionFactory.CreateConnection()) {
        return await execute(connection);
      }
    }

    async Task<TResult> ExecuteOnConnectionAsync<TResult>(Func<IDbConnection, Task<TResult>> execute) {
      using (var connection = _connectionFactory.CreateConnection()) {
        return await execute(connection);
      }
    }

    #endregion
  }
}