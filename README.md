# RePlays [![Downloads][download-badge]][download-link] [![Discord][discord-badge]][discord-link]

[download-badge]: https://img.shields.io/github/downloads/lulzsun/RePlays/latest/RePlaysSetup.exe
[download-link]: https://github.com/lulzsun/RePlays/releases/latest
[discord-badge]: https://img.shields.io/discord/654698116917886986?label=Discord&logo=discord
[discord-link]: https://discordapp.com/invite/Qj2BmZX

RePlays is a free and open source program that automatically manages recording of detected running games, with a clip editor that allows for quick video sharing.

![Preview](/Resources/preview.png)

## Installation

### Windows

1. Download RePlaysSetup.exe from [latest release](https://github.com/lulzsun/RePlays/releases/latest), or [nightly release](https://github.com/lulzsun/RePlays/releases/tag/nightly)
2. Open RePlaysSetup.exe
3. ...profit!

Note: Depending on your Windows version, you may need to manually download and install [Microsoft Edge WebView2 runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section) if not prompted to.

### Linux

1. Download the `RePlays-linux-x64` artifact from a [successful Linux Build run](https://github.com/rowkav09/RePlays/actions/workflows/linux-build.yml?query=branch%3Alinux-build+is%3Asuccess).
2. Unzip the artifact, then extract `RePlays-linux-x64.tar.gz`.
3. Install the runtime libraries listed in `README-linux.txt` and run `./RePlays` from the extracted folder.

The archive includes the interface and clip tools. Recording still needs a compatible Linux `libobs.so`, which is not bundled. See the [Linux roadmap](https://github.com/lulzsun/RePlays/issues/162) for the status of Linux support.

## Development

Get started with contributing to the project with this quick [getting started guide](https://github.com/lulzsun/RePlays/wiki/Development-Windows)!

## Technology Stack

RePlays is powered by C# (.NET 8) and Typescript (React). Typescript is used for the interface (frontend), while C# is the core of the program (backend).

The interface is displayed using Microsoft Edge WebView2 on Windows, and WebKit2GTK on Linux.

Recording functionality is currently powered using libobs, thanks to the [OBS](https://obsproject.com/) Project!

## End Notes

The project came together in homage to [Plays.tv](https://en.wikipedia.org/wiki/Plays.tv).

This software is not sponsored by or affiliated with Plays.tv or its affiliates.

This project is licensed under the terms of the GNU license.
