using System;
using System.Collections.Generic;
using System.Data;
using Dapper;

/*
 * from:  http://stackoverflow.com/questions/28678442/how-can-i-make-dapper-net-throw-when-result-set-has-unmapped-columns/39490419#39490419
 * solution by Richardissimo
 */

namespace SafeDapper
{
    public  static class DapperExtensions
    {
        private static readonly IDictionary<Type, object> TypesThatHaveMapper = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Extension to the Dapper methods, SafeQuery will throw an exception if any column in the query 
        /// is not mapped to a property of the type. This prevents silent failure/defaulted property values
        /// in the case that the names of columns/properties are misspelled, or changed in one place but not another.
        /// </summary>
        public static IEnumerable<T> SafeQuery<T>(this IDbConnection cnn, string sql, object param = null,
		    IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = default(int?),
		    CommandType? commandType = default(CommandType?))
        {
            lock (_lock)
            {
                if (TypesThatHaveMapper.ContainsKey(typeof(T)) == false)
                {
                    SqlMapper.SetTypeMap(typeof(T), new ThrowWhenNullTypeMap<T>());
                    TypesThatHaveMapper.Add(typeof(T), null);
                }
            }
            return cnn.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
        }


        /// <summary>
        /// Extension to the Dapper methods, SafeQuery will throw an exception if any column in the query 
        /// is not mapped to a property of the type. This prevents silent failure/defaulted property values
        /// in the case that the names of columns/properties are misspelled, or changed in one place but not another.
        /// </summary>
        public static T SafeQuerySingle<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
        {
            lock (_lock)
            {
                if (TypesThatHaveMapper.ContainsKey(typeof(T)) == false)
                {
                    SqlMapper.SetTypeMap(typeof(T), new ThrowWhenNullTypeMap<T>());
                    TypesThatHaveMapper.Add(typeof(T), null);
                }
            }

            return cnn.QuerySingle<T>(sql, param, transaction, commandTimeout, commandType);
        }

    }
}
