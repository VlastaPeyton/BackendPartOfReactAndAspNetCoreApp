namespace Api.Events.IntegrationEvents
{
    public record CommentCreatedIntegrationEvent : IntegrationEvent
    {
        public string Text { get; set; } = string.Empty;
    }
}
