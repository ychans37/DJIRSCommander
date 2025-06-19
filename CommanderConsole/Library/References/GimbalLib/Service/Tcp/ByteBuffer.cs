using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommanderConsole.Service.Tcp
{
    public class ByteBuffer
    {
        private const int BUFFER_COUNT = 1024 * 1024; //1MB
        private byte[] _Buffer = new byte[BUFFER_COUNT];
        private int _Count = 0;
        private object _lock = new object();

        private int ByteSearch(byte[] buffer, byte[] pattern, bool withPattern = false)
        {
            int pos = -1;

            int length = buffer.Length;
            int start = 0;
            bool matched;

            if (length > 0 && pattern.Length > 0 && start <= (length - pattern.Length) && length >= pattern.Length)
            {
                for (int i = start; i <= length - pattern.Length; i++)
                {
                    if (buffer[i] == pattern[0])
                    {
                        if (length > 1)
                        {
                            matched = true;
                            for (int y = 1; y <= pattern.Length - 1; y++)
                            {
                                if (buffer[i + y] != pattern[y])
                                {
                                    matched = false;
                                    break;
                                }
                            }

                            if (matched)
                            {
                                pos = i;

                                if (withPattern)
                                {
                                    pos += pattern.Length;
                                }
                                break;
                            }
                        }
                        else
                        {
                            pos = i;

                            if (withPattern)
                            {
                                pos += pattern.Length;
                            }

                            break;
                        }
                    }
                }
            }

            return pos;
        }

        public int GetLength()
        {
            return _Count;
        }

        public int PatternSearch(byte[] pattern, bool withPattern)
        {
            return ByteSearch(_Buffer, pattern, withPattern);
        }

        public void PutByteBlock(byte[] bytes, int length)
        {
            lock (_lock)
            {
                Buffer.BlockCopy(bytes, 0, _Buffer, _Count, length);
                _Count += length;
            }
        }

        public bool PopByteBlock(ref byte[] bytes, int length)
        {
            bool copyComplete = false;

            lock (_lock)
            {
                if (length <= _Count)
                {
                    //정해진 양만큼 복사 한다.
                    Buffer.BlockCopy(_Buffer, 0, bytes, 0, length);

                    //남은 데이터를 버퍼의 첫 부분으로 이동 한다.
                    //이부분에 대해서 부하가 걸리기 때문에 추후 이 로직을 하지 않을 수 있도록 조정 한다.
                    Buffer.BlockCopy(_Buffer, length, _Buffer, 0, _Count - length);

                    //뒷 부분을 클리어 한다.
                    Array.Clear(_Buffer, _Count - length, length);

                    _Count -= length;

                    copyComplete = true;
                }
            }

            return copyComplete;
        }

        /*
        public bool PopByteBlock(ref byte[] bytes, int pos, int length)
        {
            bool copyComplete = false;

            lock (_lock)
            {
                if (length <= _Count && pos >= length)
                {
                    //정해진 양만큼 복사 한다.
                    Buffer.BlockCopy(_Buffer, pos - length, bytes, 0, length);

                    //남은 데이터를 버퍼의 첫 부분으로 이동 한다.
                    //이부분에 대해서 부하가 걸리기 때문에 추후 이 로직을 하지 않을 수 있도록 조정 한다.
                    Buffer.BlockCopy(_Buffer, pos, _Buffer, pos - length, _Count - pos);

                    _Count -= length;

                    //뒷 부분을 클리어 한다.
                    Array.Clear(_Buffer, _Count, length);

                    copyComplete = true;
                }
            }

            return copyComplete;
        }*/

        public bool PopByteBlock(ref byte[] bytes, int pos, int length)
        {
            bool copyComplete = false;

            lock (_lock)
            {
                //if (length <= _Count && pos >= length)
                if (_Count - pos >= length)
                {
                    //정해진 양만큼 복사 한다.
                    Buffer.BlockCopy(_Buffer, pos, bytes, 0, length);

                    //남은 데이터를 버퍼의 첫 부분으로 이동 한다.
                    //이부분에 대해서 부하가 걸리기 때문에 추후 이 로직을 하지 않을 수 있도록 조정 한다.
                    Buffer.BlockCopy(_Buffer, pos + length, _Buffer, pos, _Count - length);

                    _Count -= length;

                    //뒷 부분을 클리어 한다.
                    //??????
                    Array.Clear(_Buffer, _Count, length);

                    copyComplete = true;
                }
            }

            return copyComplete;
        }

        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_Buffer, 0, BUFFER_COUNT);
                _Count = 0;
            }
        }
    }
}
