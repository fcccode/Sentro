﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Sentro.Traffic;
using Sentro.Utilities;

namespace Sentro.Cache
{
    public class CacheResponse
    {
        private readonly FileStream _fileStream;
        private FileLogger fileLogger;
        private bool _closed;
        private SemaphoreSlim nextPacketSem;
        public CacheResponse(FileStream fs)
        {
            _fileStream = fs;            
            fileLogger = FileLogger.GetInstance();
            nextPacketSem = new SemaphoreSlim(1, 1);
        }

        public void Close()
        {
            _closed = true;
            _fileStream.Dispose();            
        }
        
        /*
            return cached content as packets with empty 40 bytes at start to set
            ip and tcp headers
            
             - used yield to make it lazy function
        */
        public IEnumerable<Packet> NetworkPackets
        {
            get
            {                
                long length = _fileStream.Length;
                long read = 0;
                var headersPacket = HeadersPacket();
                yield return headersPacket;
                read += headersPacket.DataLength;
                while (read < length && !_closed)
                {
                    byte[] rawPacket = new byte[1500];
                    var stepRead = _fileStream.Read(rawPacket, 40, 1420);
                    read += stepRead;
                    fileLogger.Debug("netowrkPacket", "read : " + read);
                    yield return new Packet(rawPacket, (uint) stepRead + 40);
                }
            }
        }

        /*
            read only the headers of the response and send them in seperate packet
        */
        private Packet HeadersPacket()
        {
            try
            {
                var firstByte = _fileStream.ReadByte();
                var secondByte = _fileStream.ReadByte();
                var headersLength = (secondByte << 8) | firstByte;

                fileLogger.Debug("cache resp", $"{headersLength}");
                byte[] headersPacket = new byte[1500];
                _fileStream.Read(headersPacket, 40, headersLength);
                return new Packet(headersPacket, (uint) headersLength + 40);
            }
            catch (Exception e)
            {
                fileLogger.Debug("cache resp",e.ToString());
                return null;                
            }
        }

        /*
            return the next packet in the cache response            
        */
        private IEnumerator<Packet> enumerator;      
        public Packet NextPacket()
        {
            try
            {
                fileLogger.Debug("cache resp", "in next packet");
                nextPacketSem.Wait();
                fileLogger.Debug("cache resp", "in next packet after wait");
                if (enumerator == null)
                    enumerator = NetworkPackets.GetEnumerator();
                Packet nextPacket = null;
                if (enumerator.MoveNext())
                {
                    fileLogger.Debug("netowrkPacket", "i found next packet");
                    nextPacket = enumerator.Current;
                }
                nextPacketSem.Release();
                return nextPacket;
            }
            catch (Exception e)
            {
                fileLogger.Debug("cache resp",e.ToString());
                return null;
            }
        }
      
    }
}
