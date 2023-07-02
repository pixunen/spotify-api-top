namespace spotify_api_top_console_app
{
    public class Program
    {
        public static async Task Main()
        {
            /*
             * 
             * This is the starting point for the console application
             * 
             */

            try
            {
                BootUpSequence();
                var spotifyAPIClient = new SpotifyAPIClient();

                // Create OAUth token so we can use API
                if (await spotifyAPIClient.CreateAuthAsync() && await spotifyAPIClient.ProcessPlaylists())
                {
                    // Get 20 newest songs from given playlist
                    Thread.Sleep(200);
                    Console.Clear();

                    await spotifyAPIClient.ProcessSongsToPlaylists();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e}");
            }
            finally
            {
                Console.WriteLine("All Done. Press any key to close.");
                Console.ReadKey();
            }


        }

        private static void BootUpSequence()
        {
            Console.WriteLine($" ________  ________  ________  _________  ___  ________ ___    ___ \r\n|\\   ____\\|\\   __  \\|\\   __  \\|\\___   ___\\\\  \\|\\  _____\\\\  \\  /  /|\r\n\\ \\  \\___|\\ \\  \\|\\  \\ \\  \\|\\  \\|___ \\  \\_\\ \\  \\ \\  \\__/\\ \\  \\/  / /\r\n \\ \\_____  \\ \\   ____\\ \\  \\\\\\  \\   \\ \\  \\ \\ \\  \\ \\   __\\\\ \\    / / \r\n  \\|____|\\  \\ \\  \\___|\\ \\  \\\\\\  \\   \\ \\  \\ \\ \\  \\ \\  \\_| \\/  /  /  \r\n    ____\\_\\  \\ \\__\\    \\ \\_______\\   \\ \\__\\ \\ \\__\\ \\__\\__/  / /    \r\n   |\\_________\\|__|     \\|_______|    \\|__|  \\|__|\\|__|\\___/ /     \r\n   \\|_________|                                       \\|___|/      \r\n                                                                   \r\n                                                                   \r\n _________  ________  ________         _______  ________           \r\n|\\___   ___\\\\   __  \\|\\   __  \\       /  ___  \\|\\   __  \\          \r\n\\|___ \\  \\_\\ \\  \\|\\  \\ \\  \\|\\  \\     /__/|_/  /\\ \\  \\|\\  \\         \r\n     \\ \\  \\ \\ \\  \\\\\\  \\ \\   ____\\    |__|//  / /\\ \\  \\\\\\  \\        \r\n      \\ \\  \\ \\ \\  \\\\\\  \\ \\  \\___|        /  /_/__\\ \\  \\\\\\  \\       \r\n       \\ \\__\\ \\ \\_______\\ \\__\\          |\\________\\ \\_______\\      \r\n        \\|__|  \\|_______|\\|__|           \\|_______|\\|_______|      \r\n                                                                   ");
            Console.WriteLine($"Welcome to Spotify TOP 20 - Your hassle-free solution to curate your own TOP 20 playlist from existing ones.");
            Console.Write("Booting up");

            for (int i = 0; i < 10; i++)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    break;
                }
                Console.Write(".");
                Thread.Sleep(500);

                if ((i + 1) % 3 == 0)
                {
                    Console.Write("\b\b\b   \b\b\b");
                    Thread.Sleep(500);
                }
            }

            Console.Clear();
        }
    }
}