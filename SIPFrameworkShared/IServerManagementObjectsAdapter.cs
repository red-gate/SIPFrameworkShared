using System;
using System.Data.SqlClient;

namespace RedGate.SIPFrameworkShared
{
    public interface IServerManagementObjectsAdapter
    {
        [Obsolete("Pass in a connection string, not an open SqlConnection")]
        string ScriptAsAlter(SqlConnection openSqlConnection, string databaseName, string schemaName, string scriptableObjectName);
    }

    public interface IServerManagementObjectsAdapter2 : IServerManagementObjectsAdapter
    {
        string ScriptAsAlter(string connectionString, string databaseName, string schemaName, string scriptableObjectName);
    }
}
