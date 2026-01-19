using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Osu.Music.Common.Models;
using Osu.Music.Services.Events;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Windows;

namespace Osu.Music.Services.Audio
{
    public class AudioPlayback : BindableBase, IDisposable
    {
        #region Properties
        public Beatmap Beatmap { get; set; }

        private bool _mute = false;
        public bool Mute { get => _mute; set { SetProperty(ref _mute, value); SetVolume(); } }

        private float _volume = 0.3f;
        public float Volume { get => _volume; set { SetProperty(ref _volume, Math.Min(Math.Max(value, 0), 1)); SetVolume(); } }

        public bool Repeat { get; set; }
        public bool Shuffle { get; set; }

        private bool _isSeeking = false;
        private bool _isInitialized = false;

        public TimeSpan CurrentTime => fileStream != null ? fileStream.CurrentTime : TimeSpan.Zero;
        public TimeSpan TotalTime => fileStream != null ? fileStream.TotalTime : TimeSpan.Zero;
        public long Position => fileStream != null ? fileStream.Position : 0;
        public long Length => fileStream != null ? fileStream.Length : 0;
        public PlaybackState PlaybackState => playbackDevice != null ? playbackDevice.PlaybackState : PlaybackState.Stopped;
        #endregion

        #region Events
        public event EventHandler<BeatmapEventArgs> BeatmapEnded;
        public event EventHandler<FftEventArgs> FftCalculated;
        #endregion

        private IWavePlayer playbackDevice;
        private WaveStream fileStream;

        #region Core

        public void Load()
        {
            Stop();
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(TimeSpan.Zero);
        }

        public void Load(TimeSpan startTime)
        {
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(startTime);
        }

        private void OpenFile(TimeSpan startTime)
        {
            try
            {
                if (Beatmap == null) return;

                fileStream = new FFmpegWaveStream(Beatmap.AudioFilePath, startTime);

                var resampled = new WdlResamplingSampleProvider(fileStream.ToSampleProvider(), 44100);
                var aggregator = new SampleAggregator(resampled) { PerformFFT = true };
                aggregator.FftCalculated += (s, a) => FftCalculated?.Invoke(this, a);

                playbackDevice.Init(aggregator);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                Debug.WriteLine($"OpenFile error: {ex.Message}");
            }
        }

        private void CloseFile()
        {
            try
            {
                fileStream?.Dispose();
                fileStream = null;
            }
            catch { }
        }

        private void EnsureDeviceCreated()
        {
            if (playbackDevice != null)
            {
                playbackDevice.Stop();
                playbackDevice.Dispose();
                playbackDevice = null;
            }

            _isInitialized = false;
            CreateDevice();
        }

        private void CreateDevice()
        {
            playbackDevice = new WaveOutEvent { DesiredLatency = 300 };
            playbackDevice.Volume = _mute ? 0 : _volume;
            playbackDevice.PlaybackStopped += OnPlaybackStopped;
        }

        private void SetVolume()
        {
            if (playbackDevice != null)
                playbackDevice.Volume = _mute ? 0 : _volume;
        }

        #endregion

        #region Playback

        public void Play()
        {
            if (!_isInitialized || playbackDevice == null)
                return;

            if (playbackDevice.PlaybackState != PlaybackState.Playing)
                playbackDevice.Play();
        }

        public void Pause()
        {
            playbackDevice?.Pause();
        }

        public void Stop() => playbackDevice?.Stop();

        public void Seek(TimeSpan position)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Beatmap == null) return;

                _isSeeking = true;
                bool wasPlaying = PlaybackState == PlaybackState.Playing;

                CloseFile();
                EnsureDeviceCreated();
                OpenFile(position);

                if (wasPlaying)
                    Play();

                _isSeeking = false;
            });
        }

        #endregion

        #region Events

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (fileStream == null || _isSeeking) return;

                double remaining = (fileStream.TotalTime - fileStream.CurrentTime).TotalSeconds;
                bool ended = remaining < 1.5 || fileStream.Position >= fileStream.Length;

                if (ended)
                {
                    CloseFile();
                    BeatmapEnded?.Invoke(this, new BeatmapEventArgs(Beatmap));
                }
            });
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            CloseFile();
            playbackDevice?.Dispose();
        }

        #endregion
    }
}
