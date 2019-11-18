using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Web.Http.Results;

namespace WebSocketSharp.Owin.Sample
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        [Route(""), HttpGet, Authorize]
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
        [Route("file"), HttpGet]
        public IHttpActionResult Get()
        {
            string someTextToSendAsAFile = "Hello world";
            byte[] textAsBytes = Encoding.Unicode.GetBytes(someTextToSendAsAFile);
 
            MemoryStream stream = new MemoryStream(textAsBytes);
 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            httpResponseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "WebApi2GeneratedFile.txt"
            };
            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
 
            ResponseMessageResult responseMessageResult = ResponseMessage(httpResponseMessage);
            return responseMessageResult;
        }
    }
}