using Microsoft.Extensions.Configuration;
using Npgsql;
using PostgresTestApi.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PostgresTestApi.Postgres
{
    // This is the singleton postgres client that is dependency-injected into the controller.
    // It is where we maintain the single connection object to Postgres.
    class PostgresClient : IPostgresClient
    {
        private NpgsqlConnection pgConn;
        private List<string> queries;

        public PostgresClient(IConfiguration config)
        {
            // For testing, let's add a few queries from which we'll pick one at random with every call
            queries = new List<string>
            {
                // Return three columns of the first row
                "SELECT \"STN\",\"DATESTR\",\"TG\" FROM weatherdata LIMIT 1",

                // Return all cols for all rows between 2000 and 2005 where the temperature contains a 2. 
                "SELECT * FROM weatherdata WHERE \"DATESTR\" > 20000101 AND \"DATESTR\" < 20050101 AND \"TG\" LIKE '2'"
            };

            string connectionString = config.GetValue<string>("Postgres_ConnectionString");
            PostgresInit(connectionString).GetAwaiter().GetResult();
        }

        // One-time init of the postgres connection
        public async Task PostgresInit(string connectionString)
        {    
            pgConn = new NpgsqlConnection(connectionString);
            await pgConn.OpenAsync();
        }

        // This method is called from the controller with every new API request. 
        public async Task<DbResponse> GetData()
        {
            // Pick a random query
            int nextQuery = new Random().Next(queries.Count);

            // For this PoC, we don't really care about content. 
            // We've not built any request/response models, only need to show that the query was executed. 
            // For now, we'll just append the first column value of each row to a string and return that. 
            // DbReponse only has one field ('data') that we can use for this.
            StringBuilder res = new StringBuilder();

            await using (var cmd = new NpgsqlCommand(queries[nextQuery], pgConn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    res.Append(String.Format("{0} ", reader.GetValue(0)));
            }

            return new DbResponse() { Data = res.ToString() };
        }
    }
}
