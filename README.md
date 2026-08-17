# React Player

React Player is a free media request player for OBS, powered by Streamer.bot.

It takes supported video links from chat and Streamer.bot events, checks them, adds them to one queue, and gives you control from a dock inside OBS.

**Supported media:** YouTube videos and Shorts, Twitch Clips, Medal clips, and TikTok videos.

## Get started

The easiest way to install React Player is the setup guide:

**https://react.melkepakken.tv/#setup**

You can download the Streamer.bot package directly from the website and follow the core setup from there.

React Player requires **OBS Studio** and **Streamer.bot**.

## Features

- One request queue for all supported media
- Dedicated OBS control dock
- Manual requests from the dock
- Viewer request command with `!sr`
- Chat Scanner for supported links in Twitch, Kick, and YouTube chat
- StreamElements tip message integration
- Customizable vote skip with one vote per viewer per playback
- Ready-made streamer and moderator controls
- Queue reordering, history, replay, and optional autoplay
- Play, pause, seek, volume, mute, skip, stop, and refresh where supported
- Audio-only mode and fullscreen projector
- Automatic OBS Browser Source detection
- English and Norwegian dock language

## How requests work

1. A viewer sends a link through chat or another Streamer.bot event.
2. React Player finds and checks the first supported link.
3. Valid requests wait in the queue.
4. You or your moderators decide what plays.

Text around supported links is ignored automatically, so messages like this work:

```text
check this out https://youtu.be/example
```

## Optional extras

React Player ships with optional features ready to enable in Streamer.bot:

- **Chat Scanner** - find supported links inside normal chat messages
- **StreamElements Tips** - accept supported links inside tip messages
- **Vote Skip** - set your own vote threshold and chat message
- **Viewer commands** - enable `!sr` and the included aliases
- **Moderator commands** - enable only the controls you want your moderators to use
- **Custom events** - connect Bits, subscriptions, redeems, Power-ups, or other Streamer.bot events to the normal Add Link flow

These are disabled by default where appropriate so you can use only what fits your stream.

## Hosted player

React Player is hosted at:

**https://react.melkepakken.tv/**

OBS uses:

```text
Player: https://react.melkepakken.tv/v1/player/
Dock:   https://react.melkepakken.tv/v1/dock/
```

The hosted player and dock connect back to Streamer.bot running on your PC. Your request queue and controls stay in Streamer.bot.

## Repository

React Player is intentionally simple.

```text
public/       Website, OBS player, dock, shared frontend files
functions/    Cloudflare Pages Functions
streamerbot/  Streamer.bot import and C# source
```

There is no framework, package manager, or build step for the frontend.

## Support

Found a bug or have an idea? Open an issue on GitHub.

For setup help, check the **Help** section on the React Player website first.

## Support the project

React Player will always be free.

If it helped your stream and you want to support development, you can donate through the React Player website.

## License

React Player is licensed under the **GNU General Public License v3.0**.

You can use, modify, and redistribute React Player under the terms of the GPL. If you distribute a modified version, the GPL-covered source must remain available under the GPL.

Copyright © 2026 Stian Kvalvik / Melkepakken.

## Disclaimer

React Player is an independent project and is not affiliated with Streamer.bot, OBS Studio, Twitch, YouTube, Medal, TikTok, or StreamElements.
