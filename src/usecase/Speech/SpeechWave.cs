namespace Application.Speech;

using System.Buffers.Binary;

public static class SpeechWave
{
    public const int MaxBytes = 3 * 1024 * 1024;
    private const int MaxSeconds = 60;
    private const int HeaderSize = 44;
    private const int FrameBytes = 2;

    /// <returns>The validated sample rate in hertz.</returns>
    public static int Validate(ReadOnlySpan<byte> wav)
    {
        if (wav.Length > MaxBytes) throw TooLong();
        if (wav.Length < HeaderSize || !wav[..4].SequenceEqual("RIFF"u8)
            || UInt32(wav[4..]) != wav.Length - 8 || !wav[8..12].SequenceEqual("WAVE"u8))
            throw InvalidAudio();

        var sampleRate = 0;
        var dataSeen = false;
        var chunks = wav[12..];
        while (!chunks.IsEmpty)
        {
            if (chunks.Length < 8) throw InvalidAudio();
            var id = chunks[..4];
            var size = UInt32(chunks[4..]);
            if (size > chunks.Length - 8) throw InvalidAudio();
            var paddedSize = (int)size + (int)(size % 2);
            if (paddedSize > chunks.Length - 8) throw InvalidAudio();

            var chunk = chunks.Slice(8, (int)size);
            if (id.SequenceEqual("fmt "u8))
            {
                if (sampleRate != 0 || chunk.Length < 16 || UInt16(chunk) != 1
                    || UInt16(chunk[2..]) != 1 || UInt16(chunk[12..]) != FrameBytes || UInt16(chunk[14..]) != 16)
                    throw InvalidAudio();
                var rate = UInt32(chunk[4..]);
                if (rate is < 8000 or > 48000 || UInt32(chunk[8..]) != rate * FrameBytes)
                    throw InvalidAudio();
                sampleRate = (int)rate;
            }
            else if (id.SequenceEqual("data"u8))
            {
                if (sampleRate == 0 || dataSeen) throw InvalidAudio();
                ValidateFrames(chunk.Length, sampleRate);
                dataSeen = true;
            }
            chunks = chunks[(8 + paddedSize)..];
        }
        return dataSeen ? sampleRate : throw InvalidAudio();
    }

    public static byte[] WrapPcm16Mono(ReadOnlySpan<byte> pcm, int sampleRate)
    {
        if (sampleRate is < 8000 or > 48000) throw InvalidAudio();
        if (pcm.Length > MaxBytes - HeaderSize) throw TooLong();
        ValidateFrames(pcm.Length, sampleRate);

        var wav = new byte[HeaderSize + pcm.Length];
        "RIFF"u8.CopyTo(wav);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(4), wav.Length - 8);
        "WAVEfmt "u8.CopyTo(wav.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(24), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(28), sampleRate * FrameBytes);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(32), FrameBytes);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(34), 16);
        "data"u8.CopyTo(wav.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(40), pcm.Length);
        pcm.CopyTo(wav.AsSpan(HeaderSize));
        return wav;
    }

    private static void ValidateFrames(int length, int sampleRate)
    {
        if (length == 0 || length % FrameBytes != 0) throw InvalidAudio();
        if (length > sampleRate * FrameBytes * MaxSeconds) throw TooLong();
    }

    private static uint UInt32(ReadOnlySpan<byte> data) => BinaryPrimitives.ReadUInt32LittleEndian(data);
    private static ushort UInt16(ReadOnlySpan<byte> data) => BinaryPrimitives.ReadUInt16LittleEndian(data);
    private static SpeechProviderException TooLong() => new("SERVICE_UNAVAILABLE", false, audioTooLong: true);
    private static SpeechProviderException InvalidAudio() => new("SERVICE_UNAVAILABLE", true);
}
