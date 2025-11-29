using System.ComponentModel.DataAnnotations;

namespace Api.MessageBroker
{   
    // Pogledaj Options pattern.txt
    public class MessageBrokerSettings
    {
        [Required]
        public string Host { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
