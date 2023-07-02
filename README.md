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


## Feedback

If you have any feedback, please reach out to us at otto.piskonen@gmail.com
