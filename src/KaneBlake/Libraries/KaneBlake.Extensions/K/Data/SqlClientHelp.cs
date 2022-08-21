using K.Hangfire;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using K.Data;

[assembly: JobTemplate(typeof(SqlClientHelp))]
namespace K.Data
{
    public class SqlClientHelp
    {
        /// <summary>
        /// 必须明确指定 SqlDbType ，否则 CONVERT_IMPLICIT 隐式转换可能会严重影响性能(INNER JOIN gl_invm05 aa LEFT JOIN (SELECT * FROM sw_SCPG UNION  ALL SELECT * FROM sw_SCPG_b) b)
        /// SET ARITHABORT ON 数据库默认连接必须和 SQL Server Management Studio 使用相同的配置，否则会执行不同的查询计划，导致性能差异极大
        /// 服务器属性——连接-选中 '算术终止'
        /// 查看当前连接设置:
        /// DECLARE @ARITHABORT VARCHAR(3) = 'OFF';  
        /// IF((64 & @@OPTIONS) = 64 ) SET @ARITHABORT = 'ON';
        /// SELECT @ARITHABORT AS ARITHABORT;
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="conStr"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [JobTemplate]
        public static DataTable ReadDataTable(string sql, string conStr, List<SqlParameter>? parameters = null)
        {
            using var conn = new SqlConnection(conStr);
            DataTable dt = new DataTable();
            var cmd = conn.CreateCommand();
            // https://docs.microsoft.com/zh-cn/sql/t-sql/statements/set-arithabort-transact-sql?view=sql-server-ver15
            cmd.CommandText = "SET ARITHABORT ON";
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            cmd.ExecuteNonQuery();
            cmd.CommandText = sql;
            parameters?.ForEach(parm => cmd.Parameters.Add(parm));
            var adapter = new SqlDataAdapter(cmd)
            {
                ReturnProviderSpecificTypes = false,
                MissingSchemaAction = MissingSchemaAction.AddWithKey,
                MissingMappingAction = MissingMappingAction.Passthrough
            };
            adapter.Fill(dt);
            adapter.Dispose();
            cmd.Parameters.Clear();
            return dt;
        }

        [JobTemplate]
        public static void UpdateTable(DataSet ds, string conStr, int batchSize = 1)
        {
            using var connection = new SqlConnection(conStr);
            connection.Open();
            var adapters = new List<SqlDataAdapter>(ds.Tables.Count);
            var builders = new List<SqlCommandBuilder>(ds.Tables.Count);
            foreach (DataTable dt in ds.Tables)
            {
                var strBuilder = new StringBuilder();
                foreach (DataColumn col in dt.Columns)
                {
                    strBuilder.Append(col.ColumnName + ",");
                }
                var sortStr = strBuilder.ToString().Trim();
                if (string.IsNullOrEmpty(sortStr))
                {
                    sortStr = "*";
                }
                sortStr = sortStr.Substring(0, sortStr.Length - 1);
                SqlDataAdapter adapter = new SqlDataAdapter();
                var sqlCommand = new SqlCommand
                {
                    Connection = connection,
                    // SelectCommand 返回的列会作为 SqlCommandBuilder '自动生成命令' 的@参数条件
                    // InsertCommand : 插入值，其他列为默认值
                    // DeleteCommand : 条件值(必须包括主键列、数据库表必须含有主键)
                    // UpdateCommand : 更新字段、条件值(必须包括主键列、数据库表必须含有主键)
                    // 必须返回主键列信息，否则无法生成 DeleteCommand、UpdateCommand
                    // 传入DataTable的列 必须包含 SelectCommand 列，否则因为缺少参数不会匹配命令
                    CommandText = $@"select {sortStr} from {dt.TableName} where 1=2"
                };
                adapter.SelectCommand = sqlCommand;
                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);
                adapter.InsertCommand = commandBuilder.GetInsertCommand();
                adapter.DeleteCommand = commandBuilder.GetDeleteCommand();
                adapter.UpdateCommand = commandBuilder.GetUpdateCommand();
                adapter.UpdateBatchSize = batchSize;
                builders.Add(commandBuilder);// SqlCommandBuilder 释放的同时生成的命令也会同样释放
                adapters.Add(adapter);
            }
            var tran = connection.BeginTransaction();
            try
            {
                for (int i = 0; i < adapters.Count; i++)
                {
                    adapters[i].SelectCommand.Transaction = tran;
                    adapters[i].InsertCommand.Transaction = tran;
                    adapters[i].DeleteCommand.Transaction = tran;
                    adapters[i].UpdateCommand.Transaction = tran;
                    // SqlDataAdapter 一次只能更新一张表
                    adapters[i].Update(ds.Tables[i]);
                }
                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
            finally
            {
                tran.Dispose();
            }
        }

        [JobTemplate]
        public static int ExecuteTSqlInTran(string connectionString, string cmdText, List<SqlParameter>? parameters = null)
        {
            using var connection = new SqlConnection(connectionString);
            // 开启事务前需要手动开启连接
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            var transaction = connection.BeginTransaction("ExecuteNonQuery");
            try
            {
                var command = new SqlCommand(cmdText, connection, transaction);
                parameters?.ForEach(param => { command.Parameters.Add(param); });
                var count = command.ExecuteNonQuery();
                transaction.Commit();
                command.Dispose();
                return count;
            }
            catch
            {
                transaction.Rollback();// transaction.Commit(); 发生异常 继续提交(会将发生错误之前的修改提交到数据库)
                throw;
            }
            finally
            {
                transaction.Dispose();
            }
        }

        public static void InsertTable(DataTable dt, string conStr)
        {
            using var connection = new SqlConnection(conStr);
            connection.Open();

            using SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.DestinationTableName = $"dbo.{dt.TableName}";
            bulkCopy.WriteToServer(dt);

        }


        public static void SyncTable(string sourceConStr, string destinationConStr, string tableName)
        {
            var source = SqlClientHelp.ReadDataTable($"select * from {tableName}", sourceConStr);

            var destination = SqlClientHelp.ReadDataTable($"select * from {tableName}", destinationConStr);
            destination.TableName = tableName;
            //表结构转换
            foreach (DataRow r in source.Rows)
            {
                DataRow newrow = destination.NewRow();

                foreach (DataColumn dc in destination.Columns)
                {
                    foreach (DataColumn sc in source.Columns)
                    {
                        if (sc.ColumnName == dc.ColumnName)
                        {
                            try { newrow[dc.ColumnName] = r[sc.ColumnName]; }
                            catch (Exception ex)
                            {
                                //throw new Exception($"字段");
                                Console.WriteLine(ex);
                            }
                        }
                    }
                    if (!dc.AllowDBNull && newrow[dc.ColumnName].Equals(DBNull.Value))
                    {
                        if (dc.DataType.Equals(typeof(string)))
                        {
                            newrow[dc.ColumnName] = "";
                        }
                        else
                        {
                            newrow[dc.ColumnName] = dc.DataType.IsValueType ? Activator.CreateInstance(dc.DataType) : null;
                        }
                    }
                    //if (!newrow.IsNull(dc.ColumnName) && dc.DataType.Equals(typeof(string)) && dc.MaxLength>0)
                    //{
                    //    var old = (newrow[dc.ColumnName] as string).Trim();
                    //    newrow[dc.ColumnName] = old.Length> dc.MaxLength? old .Substring(0, dc.MaxLength-1) : old;
                    //}


                }

                destination.Rows.Add(newrow);

            }

            InsertTable(destination, destinationConStr);

        }
    }
}
