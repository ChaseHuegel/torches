using Library.Util;

namespace Networking.Services;

public sealed class FrameStream(Stream stream) : IDisposable
{
    private readonly Stream _stream = stream;

    public void Dispose()
    {
        _stream.Dispose();
    }

    public Result WriteFrame(byte[] data)
    {
        try
        {
            byte[] buffer = new byte[4 + data.Length];
            byte[] lengthDelimiter = BitConverter.GetBytes(buffer.Length);
            lengthDelimiter.CopyTo(buffer, 0);
            data.CopyTo(buffer, lengthDelimiter.Length);

            _stream.Write(buffer);
            return new Result(true);
        }
        catch (Exception ex)
        {
            return new Result(false, ex.ToString());
        }
    }

    public Result<byte[]> ReadFrame()
    {
        try
        {
            //  Read the frame's length
            byte[] lengthBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (!TryReadNextByte(out byte value))
                {
                    return new Result<byte[]>(false, null!, "Reached end of stream.");
                }

                lengthBytes[i] = value;
            }

            int length = BitConverter.ToInt32(lengthBytes, 0) - 4;

            //  Read the frame's body
            byte[] buffer = new byte[length];
            for (int i = 0; i < buffer.Length; i++)
            {
                if (!TryReadNextByte(out byte value))
                {
                    return new Result<byte[]>(false, null!, "Reached end of stream.");
                }

                buffer[i] = value;
            }

            return new Result<byte[]>(true, buffer);
        }
        catch (Exception ex)
        {
            return new Result<byte[]>(false, null!, ex.ToString());
        }
    }

    private bool TryReadNextByte(out byte value)
    {
        int read = _stream.ReadByte();
        if (read == -1)
        {
            value = 0;
            return false;
        }

        value = (byte)read;
        return true;
    }
}