using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;


namespace spotify_api_top_console_app
{
    public class Program
    {
        private static readonly string playlistId = "NEW_PLAYLIST_TOP20";
        private static readonly string originalPlaylistId = "ORIGINAL_PLAYLIST";
        private static readonly string clientId = "CLIENT_ID";
        private static readonly string apiSecret = "API_SECRET";

        private static List<string> songUris = new();
        public static SpotifyClient? spotifyClient;
        private static EmbedIOAuthServer? _server;

        public static async Task Main()
        {
            /*
             * 
             * This is the starting point for the console application
             * CHANGE THE GLOBAL VARIABLES BEFORE USE ABOVE
             * 
             */

            Console.WriteLine("Starting..");
            try
            {
                // Create OAUth token so we can use API
                if(await CreateAuthAsync())
                {
                    // Get 20 newest songs from given playlist
                    var playlist = await GetPlaylistItemsAsync(originalPlaylistId);
                    Console.WriteLine($" {playlist.Count} Songs will be added \n These song will be added:\n-------------------------");
                    foreach (var item in playlist)
                    {
                        // Check if the track is music
                        if (item.Track is FullTrack track)
                        {
                            Console.WriteLine($"{track.Name}");
                            songUris.Add(track.Uri);
                        }
                        // Check if the track is not music
                        if (item.Track is not FullTrack)
                        {
                            Console.WriteLine($"We encountered {item.Track.Type}: {item.Track} \n This WONT BE ADDED");
                        }
                    }
                    Console.WriteLine("-------------------------\nAdding the songs next..");

                    // Lets add songs to already created spotify playlist
                    if (!await AddSongsToPlaylistAsync(playlist))
                    {
                        Console.WriteLine($"*-** Something went from adding the songs **-*");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e}");
            }
            finally
            {
                Console.WriteLine("-------------------------\nAll Done \n Press any key to close");
                Console.ReadKey();
            }

        }

        /// <summary>
        /// Create AuthToken and Auth the app
        /// </summary>
        private static async Task<bool> CreateAuthAsync()
        {
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.PlaylistModifyPublic, Scopes.PlaylistModifyPrivate }
            };

            // Open browser window for authentication of the spotify account
            try
            {
                BrowserUtil.Open(request.ToUri());
                Console.ReadKey();
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to open URL, manually open: {0}", request.ToUri());
                return false; 
            }

        }
        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server!.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                clientId, apiSecret, response.Code, new Uri("http://localhost:5000/callback")
              )
            );

            spotifyClient = new SpotifyClient(tokenResponse.AccessToken);
            Console.WriteLine("------------------------- \n OAuth DONE Press a key to continue \n-------------------------");
        }
        private static async Task OnErrorReceived(object sender, string error, string? state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server!.Stop();
        }

        /// <summary>
        /// Main method to fetch playlist and 20 recently added songs from that
        /// </summary>
        /// <returns>Playlist class with 20 recently added songs</returns>
        private static async Task<List<PlaylistTrack<IPlayableItem>>> GetPlaylistItemsAsync(string playlistId)
        {
            var totalSongs = await spotifyClient!.Playlists.GetItems(playlistId);
            var offset = totalSongs.Total!.Value - 20;

            var playlist = await spotifyClient.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest() { Offset = offset, Limit = 20 });
            return playlist.Items!;
        }


        /// <summary>
        /// Main method to add TOP20 songs to the new playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns>True or false depending on if we succeeded or not</returns>
        private static async Task<bool> AddSongsToPlaylistAsync(List<PlaylistTrack<IPlayableItem>> playlist)
        {
            // Check if TOP20 playlist has songs already
            var totalSongs = await spotifyClient!.Playlists.GetItems(playlistId);

            // Delete songs from TOP20 playlist if any
            if (totalSongs.Total > 0)
            {
                Console.WriteLine("Found old songs in TOP20 playlist.. COMMENCING DELETE");

                if (await CreateHttpDeleteAsync())
                {
                    // When TOP20 is empty add the new songs to it
                    var done = await AddSongsAsync();
                    if (done != null)
                    {
                        Console.WriteLine($"Songs added to TOP20 playlist succesfully");
                    }
                    else
                    {
                        Console.WriteLine($"Something went wrong adding songs to TOP20");
                    }
                }
                else
                {
                    Console.WriteLine("Couldn't delete old songs from the TOP20 playlist");
                }
            }
            else
            {
                // Add songs to TOP20 playlist
                var done = await AddSongsAsync();
                if (done != null)
                {
                    Console.WriteLine($"Songs added to TOP20 playlist succesfully");
                }
                else
                {
                    Console.WriteLine($"Something went wrong adding songs to TOP20");
                }
            }

            return true;
        }

        /// <summary>
        /// DELETE for TOP20 playlist if there is already songs
        /// </summary>
        /// <param name="url">Endpoint for spotify API 'https://api.spotify.com/v1/playlists/{playlist_id}/tracks'</param>
        /// <returns></returns>
        private static async Task<bool> CreateHttpDeleteAsync()
        {
            IList<PlaylistRemoveItemsRequest.Item> tempList = new List<PlaylistRemoveItemsRequest.Item>();
            var playlistRemoveItemsRequest = new PlaylistRemoveItemsRequest();
            var songs = await GetPlaylistItemsAsync(playlistId);

            foreach (var item in songs)
            {
                if (item.Track is FullTrack track)
                {
                    var temp = new PlaylistRemoveItemsRequest.Item
                    {
                        Uri = track.Uri,
                    };
                    tempList.Add(temp);
                }
            }

            playlistRemoveItemsRequest.Tracks = tempList;
            var resp = await spotifyClient!.Playlists.RemoveItems(playlistId, playlistRemoveItemsRequest);
            if (resp != null)
            {
                Console.WriteLine("Just deleted all songs from TOP20 playlist..");
                return true;
            }
            else
            {
                Console.WriteLine("Something went wrong deleting TOP20 songs..");
                return false;
            }
        }

        /// <summary>
        /// Method to add songs from main method
        /// </summary>
        private static async Task<SnapshotResponse> AddSongsAsync()
        {
            IList<string> top;
            List<string> normalList = new();
            foreach (var item in songUris)
            {
                normalList.Add(item);
            }
            normalList.Reverse();
            top = normalList;

            var request = new PlaylistAddItemsRequest(top);
            return await spotifyClient!.Playlists.AddItems(playlistId, request);

        }
    }
}