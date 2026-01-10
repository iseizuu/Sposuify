using NAudio.Wave;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.IO;

public class FFmpegWaveStream : WaveStream
{
    private readonly Process _ffmpeg;
    private readonly BufferedWaveProvider _buffer;
    private readonly WaveFormat _format;
    private bool _isDisposed;
    private long _position;
    private bool _isEndOfStream;
    private readonly long _length;
    private readonly TimeSpan _totalTime;
    private readonly TimeSpan _startOffset;
    private bool _ffmpegCompleted = false;

    public FFmpegWaveStream(string file, TimeSpan? start = null)
    {
        _startOffset = start ?? TimeSpan.Zero;
        _format = new WaveFormat(44100, 16, 2);

        _buffer = new BufferedWaveProvider(_format)
        {
            BufferDuration = TimeSpan.FromMinutes(5),
            DiscardOnBufferOverflow = true
        };

        _totalTime = GetDuration(file);
        var remaining = _totalTime - _startOffset;
        _length = (long)((remaining.TotalSeconds + 2) * _format.AverageBytesPerSecond);

        _ffmpeg = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
                Arguments = $"-hide_banner -loglevel warning {(_startOffset > TimeSpan.Zero ? $"-ss {_startOffset.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} " : "")}-i \"{file}\" -f s16le -ac 2 -ar 44100 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (_ffmpeg.Start())
        {
            Task.Run(ReadLoop);

            int timeout = 0;
            while (_buffer.BufferedBytes < 65536 && !_ffmpeg.HasExited && timeout < 100)
            {
                Thread.Sleep(10);
                timeout++;
            }
        }
    }

    private void ReadLoop()
    {
        byte[] tempBuf = new byte[32768];
        try
        {
            while (!_isDisposed)
            {
                int read = _ffmpeg.StandardOutput.BaseStream.Read(tempBuf, 0, tempBuf.Length);

                if (read > 0)
                {
                    _buffer.AddSamples(tempBuf, 0, read);
                }
                else
                {
                    _ffmpegCompleted = true;
                    break;
                }
            }
        }
        catch { _ffmpegCompleted = true; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_isDisposed) return 0;

        if (_position >= _length)
        {
            return 0;
        }

        int bytesRequired = (int)Math.Min(count, _length - _position);

        // Tunggu data dari FFmpeg jika buffer kosong
        while (_buffer.BufferedBytes < bytesRequired && !_ffmpegCompleted && !_ffmpeg.HasExited)
        {
            Thread.Sleep(5);
        }

        int read = _buffer.Read(buffer, offset, bytesRequired);
        _position += read;

        if (read == 0 && (_ffmpegCompleted || _position >= _length))
        {
            return 0;
        }

        return read;
    }

    public override TimeSpan CurrentTime
    {
        get
        {
            if (_position >= _length) return _totalTime;
            var time = _startOffset + TimeSpan.FromSeconds((double)_position / _format.AverageBytesPerSecond);
            return time > _totalTime ? _totalTime : time;
        }
    }

    public override long Length => (long)((_totalTime - _startOffset).TotalSeconds * _format.AverageBytesPerSecond);
    public override long Position { get => _position; set => _position = value; }
    public override WaveFormat WaveFormat => _format;
    public override TimeSpan TotalTime => _totalTime;
    private static TimeSpan GetDuration(string file)
    {
        try
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffprobe.exe"),
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{file}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sec))
                return TimeSpan.FromSeconds(sec);
        }
        catch (Exception ex) { Debug.WriteLine($"FFprobe Error: {ex.Message}"); }
        return TimeSpan.FromMinutes(5);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            try { if (!_ffmpeg.HasExited) _ffmpeg.Kill(); } catch { }
            _ffmpeg.Dispose();
        }
        base.Dispose(disposing);
    }
}