namespace AiAgents.Core.Client;

/// <summary>
/// Wraps a response stream so every byte read from the network is also
/// appended to a log file. Used for streaming DeepSeek responses where the
/// body arrives as a sequence of SSE chunks.
/// </summary>
internal sealed class LoggingStream : Stream
{
    private readonly Stream _inner;
    private readonly FileStream _logStream;
    private bool _disposed;

    public LoggingStream(Stream inner, string logFilePath)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _inner.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _inner.Read(buffer, offset, count);
        if (read > 0)
        {
            _logStream.Write(buffer, offset, read);
        }
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int read = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        if (read > 0)
        {
            await _logStream.WriteAsync(buffer, offset, read, cancellationToken).ConfigureAwait(false);
        }
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (read > 0)
        {
            await _logStream.WriteAsync(buffer.Slice(0, read), cancellationToken).ConfigureAwait(false);
        }
        return read;
    }

    public override void Flush() => _inner.Flush();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _logStream.Dispose();
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
