using Microsoft.Data.SqlClient;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace K.Extensions.Data
{
    /// <summary>
    /// A factory used to create <see cref="DbConnection"/> instances.
    /// </summary>
    public class DbConnectionFactory
    {
        /// <summary>
        /// Creates a profiled <see cref="DbConnection"/> based on the given connection string.
        /// </summary>
        /// <param name="connectionString">The connection string used to create <see cref="DbConnection"/>.</param>
        /// <returns>
        /// An initialized <see cref="DbConnection"/>.
        /// </returns>
        public static DbConnection CreateSqlConnection(string connectionString)
            => new SqlConnection(connectionString).AsProfiling();

    }

    /// <summary>
    /// Extension methods for <see cref="DbConnection"/>
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// Returns a new DbConnection that wraps connection, providing query execution profiling.
        /// <para>If profiler is null, no profiling will occur.</para>
        /// </summary>
        /// <param name="connection">The <see cref="DbConnection"/> to Profiled.</param>
        /// <returns></returns>
        public static DbConnection AsProfiling(this DbConnection connection)
        {
            if (connection is ProfiledDbConnection profiledDbConnection) 
            {
                return profiledDbConnection;
            }
            return new ProfiledDbConnection(connection, MiniProfiler.Current);
        }
    }
}
