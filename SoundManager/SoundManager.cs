using Silk.NET.OpenAL;

public unsafe class SoundManager
{
    private static SoundManager? _instance;
    public static SoundManager Instance => _instance ??= new SoundManager();
    // Whoever made OpenAL, please never do this again.
    private readonly AL _al;
    private readonly ALContext _alc;
    private readonly Device* _device;
    private readonly Context* _context;

    private readonly uint _musicSource;
    private readonly Dictionary<string, uint> _musicBuffers = new();

    private readonly int _maxSfxSources = 8;
    private readonly List<uint> _sfxSources = new();
    private readonly Dictionary<string, uint> _sfxBuffers = new();

    //  Bandaid fix
    private readonly Dictionary<string, bool> _sfxPlaying = new();
    private readonly Dictionary<uint,string> _sourceToKey = new();

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

        for (int i = 0; i < _maxSfxSources; i++)
        {
            _sfxSources.Add(_al.GenSource());
        }
    }

    private uint BuildBuffer(string wavPath)
    {
        var wav = WavLoader.LoadWav(wavPath);
        var buf = _al.GenBuffer();
        Console.WriteLine($"Loaded WAV: {wavPath}, Channels: {wav.Channels}, SampleRate: {wav.SampleRate}, BitsPerSample: {wav.BitsPerSample}");
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

    public void LoadMusic(string key, string wavPath)
    {
        if (_musicBuffers.ContainsKey(key))
            throw new ArgumentException($"Music with key '{key}' already loaded.");

        var buffer = BuildBuffer(wavPath);
        _musicBuffers[key] = buffer;
        // _al.SourceQueueBuffers(_musicSource, 1, &buffer);
    }

    public void LoadEffect(string key, string wavPath)
    {
        if (_sfxBuffers.ContainsKey(key))
            throw new ArgumentException($"Sound effect with key '{key}' already loaded.");

        var buffer = BuildBuffer(wavPath);
        _sfxBuffers[key] = buffer;
        _sfxPlaying[key] = false;
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

    public void PlayEffect(string key)
    {
        if (_sfxPlaying.TryGetValue(key, out var isPlaying) && isPlaying)
            return;


        var buffer = _sfxBuffers.GetValueOrDefault(key);
        if (buffer == 0)
        {
            Console.WriteLine($"Sound effect with key '{key}' not found.");
            return;
        }

        uint source = 0;
        foreach (var sfxSource in _sfxSources)
        {
            _al.GetSourceProperty(sfxSource, GetSourceInteger.SourceState, out var state);
            if (state != (int)SourceState.Playing)
            {
                source = sfxSource;
                break;
            }
        }

        if (source == 0)
            return;
        // throw new InvalidOperationException("No available sound effect sources.");

        _sfxPlaying[key] = true;
        _sourceToKey[source] = key;
        _al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);
        _al.SourcePlay(source);
    }

    public void Update()
    {
        foreach (var kv in _sourceToKey)
        {
            uint source = kv.Key;
            string key = kv.Value;

            _al.GetSourceProperty(source, GetSourceInteger.SourceState, out var state);
            if ((SourceState)state != SourceState.Playing)
            {
                _sfxPlaying[key] = false;
                _sourceToKey.Remove(source);
            }
        }
    }

    public void StopMusic() => _al.SourceStop(_musicSource);

    public void StopEffect(string key)
    {
        if (!_sfxBuffers.ContainsKey(key))
        {
            Console.WriteLine($"Sound effect with key '{key}' not found.");
            return;
        }

        foreach (var source in _sfxSources)
        {
            if (_sourceToKey.TryGetValue(source, out var sourceKey) && sourceKey == key)
            {
                _al.SourceStop(source);
                _sfxPlaying[key] = false;
                _sourceToKey.Remove(source);
                return;
            }
        }
    }

    public void StopAllEffects()
    {
        foreach (var source in _sfxSources) _al.SourceStop(source);
    }

    public void Dispose()
    {
        StopMusic();
        StopAllEffects();

        _al.DeleteSource(_musicSource);
        foreach (var buffer in _musicBuffers.Values) _al.DeleteBuffer(buffer);
        _musicBuffers.Clear();

        _alc.MakeContextCurrent(null);
        _alc.DestroyContext(_context);
        _alc.CloseDevice(_device);
    }
}
