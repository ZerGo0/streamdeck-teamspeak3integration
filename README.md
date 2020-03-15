# TeamSpeak 3 Integration for Elgato Stream Deck

Allows you to interact with the TeamSpeak 3 Client via the ClientQuery (Telnet) with your Stream Deck

## Requirements
You need to have the [ClientQuery plugin](https://www.myteamspeak.com/addons/943dd816-7ef2-48d7-82b8-d60c3b9b10b3) installed in your TeamSpeak 3 Client in order to use this plugin.

## Changelog
[v1.5](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/tag/v1.5):
- Fixed: Spaces in `Change Nickname`, `Away Status Message` and so on not working
- Added `Toggle INPUT (Local) Mute` Action which allows you to toggle the input mute status locally so that no one on the server can see it (Currently there is no dynamic way to update this status (couldn't find any public information about this), it will use the button state for now)

[v1.4](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/tag/v1.4):
- All actions use 1 Telnet Client now (This should fix a lot of issues and hopefully it shouldn't use that much resources anymore when it fails to create a new Telnet connection)

[v1.3](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/tag/v1.3):
- Added `Toggle AFK Status` Action which allows you to toggle the away status, input and output mute when you press the button

[v1.2](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/tag/v1.2):
- Added `Change Nickname` Action which allows you to change your name when you press the button
- Added `Toggle Away Status` Action which allows you to toggle the away status when you press the button

[v1.1](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/tag/v1.1):
- It should now mute you on the currently selected server tab
- Renamed Plugin Category and Action Names
- Added Detailed Guide for the API-Key Setup: [Here](https://github.com/ZerGo0/streamdeck-teamspeak3integration/blob/master/Docs/API%20Key%20Guide.md)

[v1.0](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/tag/v1.0):
- Added 2 buttons to toggle the mute status for the Input and Output on the TS3 Client
- Automatically update the buttons states depending on the state on the TS3 Client

## Features
- Toggle mute Input on TS3 with a button press
- Toggle mute Output on TS3 with a button press
- Toggle Away Status on TS3 with a button press (supports `Away Status Message`)
- Toggle AFK Status on TS3 with a button press (supports `Away Status Message`)
- Automatically update the status of the button even when you didn't use the Stream Deck to perform the action
- Change your Nickname on TS3 with a button press

### Download

* [Download plugin](https://github.com/ZerGo0/streamdeck-teamspeak3integration/releases/)

## Possible improvements
- ~~1 Telnet Client/Connection for all actions (definitely needed for more actions in the future, the TS3 Client can't handle too many connections)~~ ✔️ Done
- ClientQuery API-Key shared across all actions

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at <https://github.com/ZerGo0>

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at <https://github.com/ZerGo0>

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector)
* Uses Telnet by 9swampy: [![NuGet](https://img.shields.io/nuget/v/Telnet.svg?style=flat)](https://www.nuget.org/packages/Telnet)

**Special Thanks to [https://barraider.github.io](https://barraider.github.io) for providing and uploading his Stream Deck Plugins and SDK.**
