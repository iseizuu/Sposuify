using DiscordRPC;
using Osu.Music.Common.Models;
using Osu.Music.Services.UItility;
using Prism.Mvvm;
using System;

namespace Osu.Music.Services.IO
{
    public class DiscordManager : BindableBase, IDisposable
    {
        private static readonly string APPLICATION_ID = "1034672656408125500";

        private DiscordRpcClient _client;
        public DiscordRpcClient Client
        {
            get => _client;
            set => SetProperty(ref _client, value);
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public DiscordManager()
        {
            Client = new DiscordRpcClient(APPLICATION_ID);
        }

        public void Initialize()
        {
            if (Client == null)
                Client = new DiscordRpcClient(APPLICATION_ID);

            if (!Client.IsInitialized)
                Client.Initialize();
        }

        public void Deinitialize()
        {
            if (Client == null)
                return;

            Client.ClearPresence();

            if (Client.IsInitialized)
                Client.Deinitialize();
        }

        public void ClearPresence()
        {
            if (Client == null || !Client.IsInitialized)
                return;

            try
            {
                Client.ClearPresence();
            }
            catch
            {
                // For some reasons ClearPresence throws NullException
            }
        }

        public void Update(Beatmap beatmap)
        {
            if (!Enabled || Client == null || !Client.IsInitialized)
                return;

            if (beatmap != null)
            {
                // TODO: Imaplement some fun stuff like status with small image
                Client.SetPresence(new RichPresence()
                {
                    State = beatmap.Artist,
                    Details = beatmap.Title,
                    Timestamps = new Timestamps()
                    {
                        StartUnixMilliseconds = DateTime.Now.ToUniversalTime().ToUnix()
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "logo",
                        SmallImageKey = "play",
                        LargeImageText = $"{beatmap.Title} - {beatmap.Artist}",
                        SmallImageText = "Playing"
                    }
                });
            }
            else
            {
                Client.ClearPresence();
            }
        }

        public void Pause()
        {
            if (Client != null)
                Client.UpdateLargeAsset("logo");
                Client.UpdateClearTime();
                Client.UpdateSmallAsset("pause", "Paused");
        }

        public void Resume(TimeSpan resumeFrom)
        {
            if (Client != null)
                Client.UpdateLargeAsset("logo");
                Client.UpdateStartTime(DateTime.Now.Subtract(resumeFrom).ToUniversalTime());
                Client.UpdateSmallAsset("play", "Playing");
        }

        public void Dispose()
        {
            Deinitialize();
        }
    }
}
