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
        public TimeSpan CurrentTime => fileStream != null ? fileStream.CurrentTime : TimeSpan.Zero;
        public TimeSpan TotalTime => fileStream != null ? fileStream.TotalTime : TimeSpan.Zero;
        public long Position { get => fileStream != null ? fileStream.Position : 0; set { if (fileStream != null) fileStream.Position = value; } }
        public long Length => fileStream != null ? fileStream.Length : 0;
        public PlaybackState PlaybackState => playbackDevice != null ? playbackDevice.PlaybackState : PlaybackState.Stopped;
        #endregion

        #region Events
        public event EventHandler<BeatmapEventArgs> BeatmapEnded;
        public event EventHandler<FftEventArgs> FftCalculated;
        #endregion

        private IWavePlayer playbackDevice;
        private WaveStream fileStream;

        public void Load()
        {
            Stop();
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(TimeSpan.Zero);
        }

        private void CloseFile()
        {
            if (fileStream != null)
            {
                fileStream.Dispose(); 
                fileStream = null;
            }
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error OpenFile: {ex.Message}");
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (fileStream == null || _isSeeking) return;

                double secondLeft = (fileStream.TotalTime - fileStream.CurrentTime).TotalSeconds;
                bool isAtEnd = secondLeft < 1.5 || fileStream.Position >= fileStream.Length;

                if (isAtEnd)
                {
                    Stop();
                    BeatmapEnded?.Invoke(this, new BeatmapEventArgs(Beatmap));
                }
            });
        }

        private void EnsureDeviceCreated()
        {
            if (playbackDevice != null)
            {
                playbackDevice.Stop();
                playbackDevice.Dispose();
                playbackDevice = null;
            }
            CreateDevice();
        }

        private void CreateDevice()
        {
            playbackDevice = new WaveOutEvent { DesiredLatency = 300 };
            playbackDevice.Volume = _mute ? 0 : _volume;
            playbackDevice.PlaybackStopped += OnPlaybackStopped;
        }

        private void SetVolume() { if (playbackDevice != null) playbackDevice.Volume = _mute ? 0 : _volume; }

        public void Play() { if (PlaybackState != PlaybackState.Playing) playbackDevice?.Play(); }
        public void Pause() => playbackDevice?.Pause();

        public void Stop()
        {
            playbackDevice?.Stop();
            if (fileStream != null) fileStream.Position = 0;
        }

        public void Seek(TimeSpan position)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _isSeeking = true;

                bool wasPlaying = PlaybackState == PlaybackState.Playing;

                Stop();
                CloseFile();
                EnsureDeviceCreated();
                OpenFile(position);

                if (wasPlaying) Play();

                _isSeeking = false;
            });
        }

        public void Dispose()
        {
            Stop();
            CloseFile();
            playbackDevice?.Dispose();
        }
    }
}