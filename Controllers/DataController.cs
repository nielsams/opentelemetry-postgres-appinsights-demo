using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PostgresTestApi.Models;
using PostgresTestApi.Postgres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgresTestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        private IPostgresClient client;

        public DataController(ILogger<DataController> logger, IPostgresClient postgresClient)
        {
            client = postgresClient;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            DbResponse resp = await client.GetData();
            return Ok(JsonConvert.SerializeObject(resp));
        }
    }
}
