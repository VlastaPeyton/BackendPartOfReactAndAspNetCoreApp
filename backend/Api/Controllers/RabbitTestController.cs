using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/rabbitmq")]
    public class RabbitTestController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public RabbitTestController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("send")] // https://localhost:port/api/rabbitmq/send
        public async Task<IActionResult> SendMessage()
        {
            await _publishEndpoint.Publish(new TestMessage { Text = "Hello RabbitMQ!" });
            return Ok("Message sent!");
        }
    }

    public class TestMessage
    {
        public string Text { get; set; } = string.Empty;
    }

}
