using SpotifyAPI.Web;

namespace SpotifyApiTopConsoleApp
{
    internal class SpotifyAPIClient(SpotifyClient spotifyClient)
    {
        private SimplePlaylist? _sourcePlaylist;
        private SimplePlaylist? _targetPlaylist;
        private readonly List<string> songUris = [];

        public async Task<bool> ProcessPlaylists()
        {
            Console.WriteLine($"Loading data...");
            var user = await spotifyClient.UserProfile.Current();
            var playlists = await spotifyClient.Playlists.GetUsers(user.Id);
            Console.Clear();

            if (playlists?.Items == null || playlists.Items.Count == 0)
            {
                Console.WriteLine($"Could not find a playlist for user id {user.Id}\".");
                return false;
            }

            var playlistItems = playlists.Items.ToList();

            _sourcePlaylist = SelectPlaylist(playlistItems, "source");
            _targetPlaylist = SelectPlaylist(playlistItems, "target");

            return true;
        }

        private static SimplePlaylist SelectPlaylist(List<SimplePlaylist> playlists, string playlistType)
        {
            while (true)
            {
                Console.WriteLine($"Provide the {playlistType} playlist name (or a part of it):");
                var playlistName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(playlistName))
                {
                    Console.WriteLine("Playlist name cannot be empty. Please try again.");
                    Thread.Sleep(1000);
                    Console.Clear();
                    continue;
                }

                var matchingPlaylists = playlists
                    .Where(p => p.Name != null && p.Name.Contains(playlistName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingPlaylists.Count == 0)
                {
                    Console.WriteLine("No playlists found with that name. Please try again.");
                    Thread.Sleep(1000);
                    Console.Clear();
                }
                else if (matchingPlaylists.Count == 1)
                {
                    var selectedPlaylist = matchingPlaylists[0];
                    Console.WriteLine($"Selected {playlistType} playlist: {selectedPlaylist.Name}");
                    Thread.Sleep(1000);
                    Console.Clear();
                    return selectedPlaylist;
                }
                else
                {
                    Console.WriteLine($"Multiple playlists found. Please select one:");
                    for (var i = 0; i < matchingPlaylists.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {matchingPlaylists[i].Name}");
                    }

                    Console.Write("Enter the number of the playlist: ");
                    if (int.TryParse(Console.ReadLine(), out var selection) && selection > 0 && selection <= matchingPlaylists.Count)
                    {
                        var selectedPlaylist = matchingPlaylists[selection - 1];
                        Console.WriteLine($"Selected {playlistType} playlist: {selectedPlaylist.Name}");
                        Thread.Sleep(1000);
                        Console.Clear();
                        return selectedPlaylist;
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection. Please try again.");
                        Thread.Sleep(1000);
                        Console.Clear();
                    }
                }
            }
        }

        public async Task ProcessSongsToPlaylists()
        {
            if (_sourcePlaylist == null)
            {
                Console.WriteLine("Source playlist is not set. Please call ProcessPlaylists first.");
                return;
            }

            var playlist = await GetPlaylistItemsAsync(_sourcePlaylist.Id);
            Console.WriteLine($"{playlist.Count} songs will be added..");
            Thread.Sleep(1000);

            Console.WriteLine($"List of songs:\n-------------------------");
            foreach (var item in playlist)
            {
                if (item.Track is FullTrack track)
                {
                    Console.WriteLine($"{track.Name}");
                    Thread.Sleep(50);
                    songUris.Add(track.Uri);
                }
                if (item.Track is not FullTrack)
                {
                    Console.WriteLine($"We encountered {item.Track.Type}: {item.Track} \nThis WONT BE ADDED");
                }
            }

            int number = 0;
            while (number != 1 && number != 2)
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
                            if (!await AddSongsToPlaylistAsync())
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
                else
                {
                    Console.WriteLine("Invalid input, please enter 1 or 2.");
                }
            }
        }

        private async Task<List<PlaylistTrack<IPlayableItem>>> GetPlaylistItemsAsync(string playlistId)
        {
            var playlistItems = new List<PlaylistTrack<IPlayableItem>>();
            var firstPage = await spotifyClient.Playlists.GetItems(playlistId);
            if (firstPage?.Items == null) return playlistItems;

            var total = firstPage.Total;
            if (total.HasValue && total.Value > 20)
            {
                var offset = total.Value - 20;
                var itemsPage = await spotifyClient.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Offset = offset, Limit = 20 });
                if (itemsPage?.Items != null)
                {
                    playlistItems.AddRange(itemsPage.Items);
                }
            }
            else
            {
                if (firstPage.Items != null)
                {
                    playlistItems.AddRange(firstPage.Items);
                }
            }

            return playlistItems;
        }

        private async Task<bool> AddSongsToPlaylistAsync()
        {
            if (_targetPlaylist == null) return false;

            var totalSongs = await spotifyClient.Playlists.GetItems(_targetPlaylist.Id);

            if (totalSongs.Total > 0)
            {
                Console.WriteLine("Found old songs in TOP20 playlist.. COMMENCING DELETE");
                Thread.Sleep(2000);
                if (await CreateHttpDeleteAsync())
                {
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

        private async Task<bool> CreateHttpDeleteAsync()
        {
            if (_targetPlaylist == null) return false;

            var itemsToRemove = new List<PlaylistRemoveItemsRequest.Item>();
            var songs = await spotifyClient.Playlists.GetItems(_targetPlaylist.Id);

            if (songs?.Items == null) return true; // Already empty

            foreach (var item in songs.Items)
            {
                if (item.Track is FullTrack track)
                {
                    itemsToRemove.Add(new PlaylistRemoveItemsRequest.Item { Uri = track.Uri });
                }
            }

            if (itemsToRemove.Count == 0) return true;

            var playlistRemoveItemsRequest = new PlaylistRemoveItemsRequest { Tracks = itemsToRemove };
            var response = await spotifyClient.Playlists.RemoveItems(_targetPlaylist.Id, playlistRemoveItemsRequest);

            if (!string.IsNullOrEmpty(response.SnapshotId))
            {
                Console.WriteLine("Just deleted all songs from TOP20 playlist..");
                return true;
            }

            Console.WriteLine("Something went wrong deleting TOP20 songs..");
            return false;
        }

        private async Task<SnapshotResponse?> AddSongsAsync()
        {
            if (_targetPlaylist == null) return null;

            var reversedUris = new List<string>(songUris);
            reversedUris.Reverse();

            var request = new PlaylistAddItemsRequest(reversedUris);

            return await spotifyClient.Playlists.AddItems(_targetPlaylist.Id, request);
        }
    }
}
