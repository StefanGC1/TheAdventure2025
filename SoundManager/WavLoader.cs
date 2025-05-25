using Silk.NET.SDL;

public class WavData
{
    public byte[] Data;
    public int Channels;
    public int SampleRate;
    public int BitsPerSample;
}

public static class WavLoader
{
    public static WavData LoadWav(string wavPath)
    {
        using var stream = File.OpenRead(wavPath);
        using var reader = new BinaryReader(stream);

        // RIFF header
        if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException();
        reader.ReadInt32();             // file size
        if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException();

        // Because apparently, one section of optional info after fmt was not enough...
        // The next chunk after RIFF literrally reads JUNK
        while (new string(reader.ReadChars(4)) != "fmt ")
            reader.ReadBytes(reader.ReadInt32()); // Use the second field "size" to skip

        int fmtLen = reader.ReadInt32();
        var audioFormat = reader.ReadInt16();
        var channels = reader.ReadInt16();
        var sampleRate = reader.ReadInt32();
        reader.ReadInt32(); // byte rate
        reader.ReadInt16(); // block align
        var bitsPerSample = reader.ReadInt16();

        if (fmtLen > 16) reader.ReadBytes(fmtLen - 16);

        // Skip optional chunks until data chunk
        while (new string(reader.ReadChars(4)) != "data")
            reader.ReadBytes(reader.ReadInt32()); // Use the second field "size" to skip

        // data subchunk
        var dataLen = reader.ReadInt32();
        var data = reader.ReadBytes(dataLen);

        return new WavData
        {
            Data = data,
            Channels = channels,
            SampleRate = sampleRate,
            BitsPerSample = bitsPerSample
        };
    }
}