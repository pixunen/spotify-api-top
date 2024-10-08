# spotify-api-top

Using spotify API to create TOP20 song playlis out of massive original playlist

## How to use the .exe

Download the .exe from the Releases tab and launch it. After booting up, new browser window open and requires the user to authenticate with their spotify account.
After that the user needs to provide the source playlist name and the target playlist name. Finally the app adds the 20 latest songs from source to target.

## How to use the source code

If you don't download the release .exe then follow these instructions to use this with your own Spotify API.
To use the console application just change the SpotifyAPIClient constructor values with correct values from spotify API console. Remember to add http://localhost:5000/callback to callback server in the spotify API console.

```cs
public SpotifyAPIClient() 
{
    _clientId = "CLIENT ID";
    _apiSecret = "CLIENT SECRET";
}
```

## Example

When run the console app requires the 5000 port to be open. Then it asks the user to login with their spotify account.
Then user selects the source and target playlist and can choose to abort if the songs are not to their liking
![](https://github.com/pixunen/spotify-api-top/blob/master/spotify-top.gif)

## Feedback

If you have any feedback, please reach out to us at otto.piskonen@gmail.com
