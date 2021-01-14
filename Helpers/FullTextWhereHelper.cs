using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.Helpers
{
    public enum WhereClauseJoiner
    {
        AND,
        OR
    }
    public class FullTextWhereHelper
    {
        private readonly static string SqlCheckCommand = @"
                SELECT Count(*)                 
                FROM sys.columns c 
                INNER JOIN sys.fulltext_index_columns fic 
                    ON c.object_id = fic.object_id 
                    AND c.column_id = fic.column_id
                WHERE 
	                c.object_id = OBJECT_ID('dbo.tbPreComputedLocalizedProperty')
	                AND c.name = '{0}'
            ";

        public static string GetWhereClause(SqlConnection connection, WhereClauseJoiner andOr, string columnName, params string[] searchText)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = string.Format(SqlCheckCommand,columnName);
            var indxCount = Convert.ToInt32(cmd.ExecuteScalar());

            List<string> whereParts;
            if (indxCount > 1)
            {
                // 
                whereParts = searchText.Select(s => string.Format("Contains([{0}],'{1}')", columnName, s)).ToList();
            }
            else
            {
                whereParts = searchText.Select(s => string.Format("[{0}] like '%{1}%'", columnName, s)).ToList();
            }

            var joiner = andOr == WhereClauseJoiner.AND ? " AND " : " OR ";

            var joinedString = string.Join(joiner, whereParts);
            var sqlOrClause = "(" + joinedString + ")";
            return sqlOrClause;
        }
    }
}
