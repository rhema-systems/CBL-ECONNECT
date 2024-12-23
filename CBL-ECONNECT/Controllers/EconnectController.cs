using CBL_ECONNECT.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace CBL_ECONNECT.Controllers
{
    public class Econnect
    {
        public int ReqId { get; set; }
        public string ReqType { get; set; }
    }
    public class EconnectController : ApiController
    {
        // GET api/values
        public IEnumerable<Econnect> Get()
        {
            return new List<Econnect> {

                new Econnect {ReqId=0,ReqType="Leave"},
                new Econnect {ReqId=1,ReqType="PR"},
                new Econnect {ReqId=2,ReqType="PO"},
                new Econnect {ReqId=3,ReqType="PI"},
                new Econnect {ReqId=4,ReqType="PP"},
                new Econnect {ReqId=5,ReqType="VEND"}

            };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        [Route("api/econnect/PostPO")]
        public IHttpActionResult PostPO([FromBody] Econnect value)
        {
            ModFunctions funct = new ModFunctions();
            string result = funct.CreatePO(value.ReqId);
            return Ok(content: result);
        }

        [HttpPost]
        [Route("api/econnect/PostVendor")]
        public IHttpActionResult PostVendor([FromBody] Econnect value)
        {
            ModFunctions funct = new ModFunctions();
            string result = funct.CreateVendor(value.ReqId);
            return Ok(content: result);
        }


        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
