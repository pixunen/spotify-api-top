using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;

namespace spotify_api_top_console_app
{
    internal class SpotifyAPIClient
    {
        private readonly string _callbackUri = "http://localhost:6001/callback";
        private SimplePlaylist? _sourcePlaylist;
        private SimplePlaylist? _targetPlaylist;
        private readonly string _clientId; // From spotify developer dashboard
        private readonly string _apiSecret; // From spotify developer dashboard
        private readonly List<string> songUris = new();
        private SpotifyClient spotifyClient;
        private EmbedIOAuthServer? _server;

        public SpotifyAPIClient() 
        {
            _clientId = "";
            _apiSecret = "";
        }

        public async Task<bool> ProcessPlaylists()
        {
            string? sourcePlaylistName = null;
            string? targetPlaylistName = null;

            while (string.IsNullOrWhiteSpace(sourcePlaylistName))
            {
                Console.WriteLine($"Provide the source playlist name:");
                sourcePlaylistName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(sourcePlaylistName))
                {
                    Console.WriteLine("You must provide a valid source playlist name. Please try again.");
                }
            }

            while (string.IsNullOrWhiteSpace(targetPlaylistName))
            {
                Console.WriteLine($"Provide the target playlist name:");
                targetPlaylistName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(targetPlaylistName))
                {
                    Console.WriteLine("You must provide a valid target playlist name. Please try again.");
                }
            }

            var user = await spotifyClient.UserProfile.Current();
            var playlists = await spotifyClient.Playlists.GetUsers(user.Id);

            _sourcePlaylist = playlists.Items.FirstOrDefault(x => x.Name == sourcePlaylistName);
            _targetPlaylist = playlists.Items.FirstOrDefault(x => x.Name == targetPlaylistName);

            if (_sourcePlaylist == null)
            {
                Console.WriteLine($"Could not find a playlist with the name \"{sourcePlaylistName}\".");
                return false;
            }

            if (_targetPlaylist == null)
            {
                Console.WriteLine($"Could not find a playlist with the name \"{targetPlaylistName}\".");
                return false;
            }

            return true;
        }

        public async Task ProcessSongsToPlaylists()
        {
            var playlist = await GetPlaylistItemsAsync(_sourcePlaylist.Id);
            Console.WriteLine($"{playlist.Count} songs will be added..");
            Thread.Sleep(1000);

            Console.WriteLine($"List of songs:\n-------------------------");
            foreach (var item in playlist)
            {
                // Check if the track is music
                if (item.Track is FullTrack track)
                {
                    Console.WriteLine($"{track.Name}");
                    Thread.Sleep(50);
                    songUris.Add(track.Uri);
                }
                // Check if the track is not music
                if (item.Track is not FullTrack)
                {
                    Console.WriteLine($"We encountered {item.Track.Type}: {item.Track} \nThis WONT BE ADDED");
                }
            }

            int number = 0;
            while (number != 1 || number != 2)
            {
                Console.WriteLine("-------------------------\nSelect option:");
                Console.WriteLine("1 - Continue to add the songs\n2 - Abort execution");
                var input = Console.ReadKey();
                Console.WriteLine();
                if (int.TryParse(input.KeyChar.ToString(), out number))
                {
                    switch (number)
                    {
                        case 1:
                            Console.Clear();
                            Console.WriteLine("-------------------------\nAdding the songs next..");
                            Thread.Sleep(2000);
                            // Lets add songs to already created spotify playlist
                            if (!await AddSongsToPlaylistAsync(playlist))
                            {
                                Console.WriteLine($"*-** Something went from adding the songs **-*");
                            }
                            return;
                        case 2:
                            Console.WriteLine("Aborting execution..");
                            Thread.Sleep(1000);
                            Console.Clear();
                            return;
                        default:
                            Console.WriteLine("Invalid input");
                            break;
                    }
                }
            }
        }

        #region Auth
        /// <summary>
        /// Create AuthToken and Auth the app
        /// </summary>
        public async Task<bool> CreateAuthAsync()
        {
            _server = new EmbedIOAuthServer(new Uri(_callbackUri), 6001);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.PlaylistModifyPublic, Scopes.PlaylistModifyPrivate, Scopes.PlaylistReadPrivate }
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
                Console.WriteLine($"Unable to open URL, manually open: {request.ToUri()}");
                return false;
            }
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server!.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                _clientId, _apiSecret, response.Code, new Uri(_callbackUri)
              )
            );

            spotifyClient = new SpotifyClient(tokenResponse.AccessToken);
            Console.Clear();
            Console.WriteLine("------------------------- \n OAuth DONE Press a key to continue \n-------------------------");
        }

        private async Task OnErrorReceived(object sender, string error, string? state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server!.Stop();
        }
        #endregion

        #region Playlist manupilation
        /// <summary>
        /// Main method to fetch playlist and 20 recently added songs from that
        /// </summary>
        /// <returns>Playlist class with 20 recently added songs</returns>
        private async Task<List<PlaylistTrack<IPlayableItem>>> GetPlaylistItemsAsync(string playlistId)
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
        private async Task<bool> AddSongsToPlaylistAsync(List<PlaylistTrack<IPlayableItem>> playlist)
        {
            // Check if TOP20 playlist has songs already
            var totalSongs = await spotifyClient!.Playlists.GetItems(_targetPlaylist.Id);

            // Delete songs from TOP20 playlist if any
            if (totalSongs.Total > 0)
            {
                Console.WriteLine("Found old songs in TOP20 playlist.. COMMENCING DELETE");
                Thread.Sleep(2000);
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
                    Thread.Sleep(1500);
                }
                else
                {
                    Console.WriteLine("Couldn't delete old songs from the TOP20 playlist");
                }
            }
            else
            {
                // Add songs to TOP20 playlist
                Thread.Sleep(500);
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
        private async Task<bool> CreateHttpDeleteAsync()
        {
            IList<PlaylistRemoveItemsRequest.Item> tempList = new List<PlaylistRemoveItemsRequest.Item>();
            var playlistRemoveItemsRequest = new PlaylistRemoveItemsRequest();
            var songs = await GetPlaylistItemsAsync(_targetPlaylist.Id);

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
            var resp = await spotifyClient!.Playlists.RemoveItems(_targetPlaylist.Id, playlistRemoveItemsRequest);
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
        private async Task<SnapshotResponse> AddSongsAsync()
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
            return await spotifyClient!.Playlists.AddItems(_targetPlaylist.Id, request);

        }
        #endregion
    }
}
