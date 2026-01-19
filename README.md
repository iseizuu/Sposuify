# Sposuify

**Sposuify** is a simple music player for osu! modded from [Osu.Music](https://github.com/Laritello/osu-music) and supports Discord Rich Presence.

---

## Download

Head over to the **Release Page**  

- Download: [UPDATE V.2.0.2](https://github.com/iseizuu/Sposuify/releases)  

---

## Changelog v2.0.2

- Fixed multiple Discord RPC issues not appearing in various playback states.
- Fixed crash when toggling RPC settings.
- Fixed crash when switching to the next song.
- Improved next-song logic to prevent unexpected crashes.
- Improved playback handling and safety checks.
- Song thumbnail preview in song list.

---

## Requirements

- Windows 7 or higher  
- .NET Core 3.1  

---

## Features

- Integrated with Discord Rich Presence (Listening Activity)  
- Songs cover image thumbnails  
- Uses **FFmpeg** for audio decoding  
- Play, pause, seek, repeat, shuffle  
- Playlist support  

---

## License

Distributed under the **MIT License**. See `LICENSE` for more information.

---

## ⚠ Disclaimer

This project is not mine.  
All resources and original copyrights belong to **[Laritello](https://github.com/Laritello)**.  
Original project: https://github.com/Laritello/osu-music  

---

## Known Bugs

- In the original repository, `Mp3FileReader` did not support variable bitrate (VBR) MP3 files.  
  This has been fixed in Sposuify by switching to **FFmpeg** as the audio engine.

---

## Screenshots
<a href="https://ibb.co.com/m5mn228n"><img src="https://i.ibb.co.com/xqynBBHn/image.png" alt="image" border="2"></a>

<a href="https://imgbb.com/"><img src="https://i.ibb.co.com/Hp4z7L5h/image.png" alt="image" border="2"></a>
