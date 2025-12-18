using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyApiTopConsoleApp.Services
{
    public class SpotifyAuthService(string clientId, string clientSecret, string callbackUri)
    {
        private readonly Uri _callbackUri = new(callbackUri);
        private EmbedIOAuthServer? _server;
        private readonly TaskCompletionSource<SpotifyClient> _clientCompletionSource = new();

        public async Task<SpotifyClient> AuthenticateAsync()
        {
            _server = new EmbedIOAuthServer(_callbackUri, 6001);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = [Scopes.UserReadEmail, Scopes.PlaylistModifyPublic, Scopes.PlaylistModifyPrivate, Scopes.PlaylistReadPrivate]
            };

            try
            {
                BrowserUtil.Open(request.ToUri());
            }
            catch (Exception)
            {
                Console.WriteLine($"Unable to open URL, manually open: {request.ToUri()}");
                _clientCompletionSource.SetException(new Exception("Failed to open browser for authentication."));
            }

            Console.WriteLine("Waiting for authentication...");
            return await _clientCompletionSource.Task;
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            if (_server != null)
            {
                await _server.Stop();
            }

            var config = SpotifyClientConfig.CreateDefault();
            try
            {
                var tokenResponse = await new OAuthClient(config).RequestToken(
                  new AuthorizationCodeTokenRequest(clientId, clientSecret, response.Code, _callbackUri)
                );

                var spotifyClient = new SpotifyClient(tokenResponse.AccessToken);
                Console.Clear();
                _clientCompletionSource.SetResult(spotifyClient);
            }
            catch (Exception ex)
            {
                _clientCompletionSource.SetException(ex);
            }
        }

        private async Task OnErrorReceived(object sender, string error, string? state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            if (_server != null)
            {
                await _server.Stop();
            }
            _clientCompletionSource.SetException(new Exception($"Authorization error: {error}"));
        }
    }
}