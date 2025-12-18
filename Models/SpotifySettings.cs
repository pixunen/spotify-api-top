namespace SpotifyApiTopConsoleApp.Models
{
    public class SpotifySettings
    {
        public required string ClientId { get; set; }
        public required string ApiSecret { get; set; }
        public required string CallbackUri { get; set; }
    }
}
