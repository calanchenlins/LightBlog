using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CoreWeb.Util.Infrastruct
{
    public class DbKernalHelp
    {
        public DataTable ReadDataTable(string sql, string conStr)
        {
            DataTable dt = new DataTable();
            using (var conn = new SqlConnection(conStr))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
                adapter.Dispose();

            }
            return dt;
        }
        public void UpdateTable(DataSet ds, string conStr)
        {
            using (var connection = new SqlConnection(conStr))
            {
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
                    builders.Add(commandBuilder);// SqlCommandBuilder 释放的同时生成的命令也会同样释放
                    adapters.Add(adapter);
                }

                var tran = connection.BeginTransaction();
                try
                {
                    for (int i = 0; i < adapters.Count; i++)
                    {
                        adapters[i].InsertCommand.Transaction = tran;
                        adapters[i].DeleteCommand.Transaction = tran;
                        adapters[i].UpdateCommand.Transaction = tran;
                        // SqlDataAdapter 一次只能更新一张表
                        adapters[i].Update(ds.Tables[i]);
                    }
                    tran.Commit();
                }
                catch (Exception e)
                {
                    tran.Rollback();
                    throw e;
                }
            }
        }

        public int ExecuteTSqlInTran(string connectionString, string cmdText, List<SqlParameter> parameters)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                // 开启事务前需要手动开启连接
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                var transaction = connection.BeginTransaction("ExecuteNonQuery");
                try
                {
                    var command = new SqlCommand(cmdText, connection, transaction);
                    parameters.ForEach(param => { command.Parameters.Add(param); });
                    var count = command.ExecuteNonQuery();
                    transaction.Commit();
                    command.Dispose();
                    return count;
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw e;
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
    }
}
