using PostgresTestApi.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PostgresTestApi.Postgres
{
    public interface IPostgresClient
    {
        Task<DbResponse> GetData();
    }
}
