using DiscordRPC;
using Osu.Music.Common.Models;
using Prism.Mvvm;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Osu.Music.Services.IO
{
    public class DiscordManager : BindableBase, IDisposable
    {
        private static readonly string APPLICATION_ID = "1034672656408125500";
        private const string DEFAULT_IMAGE_KEY = "logo";
        private const string OSU_THUMBNAIL_URL = "https://assets.ppy.sh/beatmaps/{0}/covers/list.jpg";

        private DiscordRpcClient _client;
        private string _lastThumbnailUrl = string.Empty;

        private DateTimeOffset _trackStartUtc;
        private TimeSpan _trackDuration;

        private RichPresence _presence;

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

        public bool EnableBeatmapThumbnail { get; set; } = true;

        public DiscordManager()
        {
            Client = new DiscordRpcClient(APPLICATION_ID);
        }

        public void Initialize()
        {
            if (!Client.IsInitialized)
                Client.Initialize();
        }

        public void Deinitialize()
        {
            Client?.ClearPresence();
            Client?.Deinitialize();
        }

        public async void Update(Beatmap beatmap)
        {
            if (!Enabled || !Client.IsInitialized || beatmap == null)
                return;

            _trackDuration = beatmap.TotalTime;
            _trackStartUtc = DateTimeOffset.UtcNow;

            var assets = await GetAssetsAsync(beatmap);

            _presence = new RichPresence
            {
                Type = ActivityType.Listening,
                Details = beatmap.Title,
                State = beatmap.Artist,
                Assets = assets,
                Timestamps = CreateTimestamps(TimeSpan.Zero),
                Buttons = new[]
                {
                    new Button
                    {
                        Label = "Sposuify",
                        Url = "https://github.com/iseizuu/Sposuify/releases"
                    }
                }
            };

            Client.SetPresence(_presence);
        }


        public void Pause()
        {
            if (_presence == null) return;

            _presence.Timestamps = null;
            _presence.Assets.SmallImageKey = "pause";
            _presence.Assets.SmallImageText = "Paused";

            Client.SetPresence(_presence);
        }

        public void Resume(TimeSpan currentPosition)
        {
            if (_presence == null) return;

            _presence.Timestamps = CreateTimestamps(currentPosition);
            _presence.Assets.SmallImageKey = "play";
            _presence.Assets.SmallImageText = "Playing";

            Client.SetPresence(_presence);
        }

        // actually this is no need to be here
        public void Seek(TimeSpan currentPosition)
        {
            if (_presence == null) return;

            _presence.Timestamps = CreateTimestamps(currentPosition);
            Client.SetPresence(_presence);
        }

        private Timestamps CreateTimestamps(TimeSpan position)
        {
            _trackStartUtc = DateTimeOffset.UtcNow - position;
            var end = _trackStartUtc + _trackDuration;

            return new Timestamps
            {
                StartUnixMilliseconds = (ulong)_trackStartUtc.ToUnixTimeMilliseconds(),
                EndUnixMilliseconds = (ulong)end.ToUnixTimeMilliseconds()
            };
        }

        private async Task<Assets> GetAssetsAsync(Beatmap beatmap)
        {
            if (!EnableBeatmapThumbnail || beatmap.BeatmapSetId <= 0)
                return DefaultAssets(beatmap);

            string url = string.Format(OSU_THUMBNAIL_URL, beatmap.BeatmapSetId);
            if (Encoding.UTF8.GetByteCount(url) > 256)
                return DefaultAssets(beatmap);

            if (url != _lastThumbnailUrl)
            {
                if (!await UrlExists(url))
                    return DefaultAssets(beatmap);

                _lastThumbnailUrl = url;
            }

            return new Assets
            {
                LargeImageKey = url,
                //LargeImageText = $"{beatmap.Title} - {beatmap.Artist}",
                SmallImageKey = "play",
                SmallImageText = $"Sposuify {Assembly.GetEntryAssembly()?.GetName().Version}"
            };
        }

        private Assets DefaultAssets(Beatmap beatmap) => new Assets
        {
            LargeImageKey = DEFAULT_IMAGE_KEY,
            LargeImageText = $"{beatmap.Title} - {beatmap.Artist}",
            SmallImageKey = "play",
            SmallImageText = "Playing"
        };

        private async Task<bool> UrlExists(string url)
        {
            try
            {
                using var http = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var res = await http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public void Dispose() => Deinitialize();
    }
}
