using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace Routing.Npgsql
{
    public class GraphBuilder
    {
        public static Graph Create(NpgsqlConnection conn, string query)
        {
            var cmd = new NpgsqlCommand(query, conn);
            return Graph.Create(cmd.ExecuteReaderAndSelect(dr => new GraphDataItem()
            {
                EdgeId = dr.GetInt32(0),
                SourceVertexId = dr.GetInt32(1),
                TargetVertexId = dr.GetInt32(2),
                Cost = dr.GetDouble(3),
                ReverseCost = dr.GetDouble(4),
                Id = dr.FieldCount > 5 ? dr.GetString(5) : null
            }).ToList());
        }
    }

    internal static class NpgsqlCommandExtensions
    {
        public static IEnumerable<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand cmd, Func<NpgsqlDataReader, T> func)
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    yield return func(reader);
            }
        }
    }
}
