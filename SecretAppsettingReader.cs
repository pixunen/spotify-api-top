using Microsoft.Extensions.Configuration;
using SpotifyApiTopConsoleApp.Models;

namespace SpotifyApiTopConsoleApp
{
    public static class SecretAppsettingReader
    {
        public static SpotifySettings ReadSection()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var settings = config.GetSection("Spotify").Get<SpotifySettings>();

            return settings ?? throw new InvalidOperationException("Spotify settings not found in user secrets.");
        }
    }
}
