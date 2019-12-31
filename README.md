# TeamSpeak 3 integration for Elgato Stream Deck

Allows you to interact with the TeamSpeak 3 Client via the ClientQuery (Telnet) with your Stream Deck

## Requirements
You need to have the [ClientQuery plugin](https://www.myteamspeak.com/addons/943dd816-7ef2-48d7-82b8-d60c3b9b10b3) installed in your TeamSpeak 3 Client in order to use this plugin.

## Changelog
- Added 2 buttons to toggle mute for the Input and Output on the TS3 Client
- Automatically update the buttons states depending on the state on the TS3 Client

## Features
- Toggle mute Input on TS3 with a button press
- Toggle mute Output on TS3 with a button press
- Automatically update the status of the button even when you didn't use the Stream Deck to perform the action

### Download

* [Download plugin](https://github.com/ZerGo0/CHANGETHIS/releases/)

## Possible improvements
- 1 Telnet Client/Connection for all actions (definitely needed for more actions in the future, the TS3 Client can't handle too many connections)
- ClientQuery API-Key shared across all actions

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at https://github.com/ZerGo0

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at https://github.com/ZerGo0

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector)
* Uses Telnet by 9swampy: [![NuGet](https://img.shields.io/nuget/v/Telnet.svg?style=flat)](https://www.nuget.org/packages/Telnet)

**Special Thanks to [https://barraider.github.io](https://barraider.github.io) for providing and uploading his Stream Deck Plugins and SDK.**