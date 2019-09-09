using System.Collections.Generic;
using System.Web.Http;

namespace WebSocketSharp.Owin.Sample
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        [Route(""), HttpGet]
        public IHttpActionResult GetUsers()
        {
            return Ok(new List<User>
            {
                new User
                {
                    Name = "Peter",
                    LastName = "Petersen"
                },
                new User
                {
                    Name = "Flemming",
                    LastName = "Flemmingsen"
                }
            });
        }
    }
}