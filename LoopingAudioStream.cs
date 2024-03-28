using NAudio.Wave;

namespace OpNop.EnterTheSandstorm
{
    public class LoopingAudioStream : WaveStream
    {

        private readonly WaveStream _sourceStream;
        private bool _stopLoop = false;

        public LoopingAudioStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;
        public override long Length => _sourceStream.Length;
        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        public void StopLoop()
        {
            _stopLoop = true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    if (_sourceStream.Position == 0)
                    {
                        break;
                    }

                    if (_stopLoop)
                    {
                        break;
                    }
                    else
                    {
                        _sourceStream.Position = 0;
                    }
                }

                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}
