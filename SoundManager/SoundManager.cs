using Silk.NET.OpenAL;

public unsafe class SoundManager
{
    // Whoever made OpenAL, please never do this again.
    private readonly AL _al;
    private readonly ALContext _alc;
    private readonly Device* _device;
    private readonly Context* _context;

    private readonly uint _musicSource;
    private readonly Dictionary<string, uint> _musicBuffers = new();

    public SoundManager()
    {
        _alc = ALContext.GetApi();
        _device = _alc.OpenDevice(null);
        if (_device == null)
            throw new Exception("Failed to open OpenAL device.");

        _context = _alc.CreateContext(_device, null);
        if (_context == null)
            throw new Exception("Failed to create OpenAL context.");

        _alc.MakeContextCurrent(_context);
        _al = AL.GetApi();

        _musicSource = _al.GenSource();
        _al.SetSourceProperty(_musicSource, SourceBoolean.Looping, true);
    }

    public void LoadMusic(string key, string wavPath)
    {
        if (_musicBuffers.ContainsKey(key))
            throw new ArgumentException($"Music with key '{key}' already loaded.");

        var buffer = BuildBuffer(wavPath);
        _musicBuffers[key] = buffer;
        // _al.SourceQueueBuffers(_musicSource, 1, &buffer);
    }

    private uint BuildBuffer(string wavPath)
    {
        var wav = WavLoader.LoadWav(wavPath);
        var buf = _al.GenBuffer();

        var fmt = wav.Channels switch
        {
            1 when wav.BitsPerSample == 8 => BufferFormat.Mono8,
            1 when wav.BitsPerSample == 16 => BufferFormat.Mono16,
            2 when wav.BitsPerSample == 8 => BufferFormat.Stereo8,
            2 when wav.BitsPerSample == 16 => BufferFormat.Stereo16,
            _ => throw new NotSupportedException("Unsupported WAV format")
        };

        fixed (byte* dataPtr = wav.Data)
        {
            _al.BufferData(buf, fmt, dataPtr, wav.Data.Length, wav.SampleRate);
        }
        return buf;
    }

    public void PlayMusic(string key)
    {
        var buffer = _musicBuffers.GetValueOrDefault(key);
        if (buffer == 0)
            throw new ArgumentException($"Music with key '{key}' not found.");

        _al.SourceStop(_musicSource);
        // _al.SourceUnqueueBuffers(_musicSource, 1, &buffer);
        _al.SetSourceProperty(_musicSource, SourceInteger.Buffer, (int)buffer);
        _al.SourcePlay(_musicSource);
    }

    public void StopMusic() => _al.SourceStop(_musicSource);

    public void Dispose()
    {
        StopMusic();

        _al.DeleteSource(_musicSource);
        foreach (var buffer in _musicBuffers.Values) _al.DeleteBuffer(buffer);
        _musicBuffers.Clear();

        _alc.MakeContextCurrent(null);
        _alc.DestroyContext(_context);
        _alc.CloseDevice(_device);
    }
}
