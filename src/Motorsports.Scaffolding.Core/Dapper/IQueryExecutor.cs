using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Dapper {
  public interface IQueryExecutor {
    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> using the given SQL statement.
    /// </summary>
    /// <param name="sql">A SQL query.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    IQueryExecutor NewQuery(string sql);

    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> that uses the given object to fill the SQL query
    ///   parameters.
    /// </summary>
    /// <param name="parameters">The parameters object.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    IQueryExecutor WithParameters(object parameters);

    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> that uses the given command timeout to execute
    ///   queries
    /// </summary>
    /// <param name="commandTimeout">The command timeout that will be used when executing the query.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    IQueryExecutor WithCommandTimeout(int commandTimeout);

    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> that uses the given collection to fill the SQL query
    ///   parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    IQueryExecutor WithParameters(IEnumerable<KeyValuePair<string, object>> parameters);

    /// <summary>
    ///   Creates a new <see cref="IQueryExecutor" /> that uses the given command type to execute queries
    /// </summary>
    /// <param name="commandType">The command type that will be used when executing the query.</param>
    /// <returns>A <see cref="IQueryExecutor" />.</returns>
    IQueryExecutor WithCommandType(CommandType commandType);

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    IEnumerable<TResult> Execute<TResult>();

    /// <summary>
    ///   Executes the query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>The <typeparamref name="TResult" />.</returns>
    TResult ExecuteScalar<TResult>();

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    IEnumerable<TResult> ExecuteWithAnonymousResult<TResult>(TResult anonymousPrototype);

    /// <summary>
    ///   Executes the query and does not return the result.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    int Execute();

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TData, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TData, TResult}" />.</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    IEnumerable<TResult> ExecuteWithAdapter<TData, TResult>(Func<TData, TResult> adapter);

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> adapter, string splitOn = "Id");

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
    IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, TResult> adapter, string splitOn = "Id");

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
    IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TResult> adapter, string splitOn = "Id");

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
    IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> adapter, string splitOn = "Id");

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
    IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> adapter, string splitOn = "Id");

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
    IEnumerable<TResult> ExecuteWithAdapter<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> adapter, string splitOn = "Id");

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    Task<IEnumerable<TResult>> ExecuteAsync<TResult>();

    /// <summary>
    ///   Executes the query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>The <typeparamref name="TResult" />.</returns>
    Task<TResult> ExecuteScalarAsync<TResult>();

    /// <summary>
    ///   Executes the query and returns the results.
    /// </summary>
    /// <typeparam name="TResult">The type of the data transfer object.</typeparam>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    Task<IEnumerable<TResult>> ExecuteWithAnonymousResultAsync<TResult>(TResult anonymousPrototype);

    /// <summary>
    ///   Executes the query and does not return the result.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    Task<int> ExecuteAsync();

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TData, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TData, TResult}" />.</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TData, TResult>(Func<TData, TResult> adapter);

    /// <summary>
    ///   Executes the query using a mapping <see cref="Func{TFirst, TSecond, TResult}" /> and returns the result.
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="adapter">The mapping <see cref="Func{TFirst, TSecond, TResult}" />.</param>
    /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
    /// <returns>A collection of <typeparamref name="TResult" />.</returns>
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TResult>(Func<TFirst, TSecond, TResult> adapter, string splitOn = "Id");

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
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TResult>(Func<TFirst, TSecond, TThird, TResult> adapter, string splitOn = "Id");

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
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TResult> adapter, string splitOn = "Id");

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
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> adapter, string splitOn = "Id");

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
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> adapter, string splitOn = "Id");

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
    Task<IEnumerable<TResult>> ExecuteWithAdapterAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TResult> adapter, string splitOn = "Id");
  }
}