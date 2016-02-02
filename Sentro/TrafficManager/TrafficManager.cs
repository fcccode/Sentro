﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Divert.Net;

namespace Sentro.TrafficManager
{
    /*
        Responsipility : Sniff HTTP/S Request and Respons packets and take action
    */
    class TrafficManager
    {
        public const string Tag = "TrafficManager";
        private TrafficManager _trafficManager;
        private bool running = true;

        private TrafficManager()
        {
            var divertForward = Task.Run(() => Divert(true));
            var divertNetwork = Task.Run(() => Divert(false));

            Task.WaitAll(divertNetwork, divertForward);

        }

        public TrafficManager GetInstance()
        {
            return _trafficManager ?? (_trafficManager = new TrafficManager());
        }

        public void Stop()
        {
            running = false;
        }

        private void Divert(bool forwardMode)
        {
            Dictionary<Connection, TcpRecon> divertDict = new Dictionary<Connection, TcpRecon>();
            Diversion diversion;

            string filter = "tcp.PayloadLength > 0 and (tcp.DstPort == 80 or tcp.DstPort == 443 or tcp.SrcPort == 443 or tcp.SrcPort == 80)";

            try
            {
                diversion = Diversion.Open(filter, forwardMode ? DivertLayer.NetworkForward : DivertLayer.Network, 100, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            if (!diversion.Handle.Valid)
            {
                Console.WriteLine("Failed to open divert handle with error {0}", System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                return;
            }

            IPHeader ipHeader = new IPHeader();
            //IPv6Header ipv6Header = new IPv6Header();
            //ICMPHeader icmpHeader = new ICMPHeader();
            //ICMPv6Header icmpv6Header = new ICMPv6Header();
            TCPHeader tcpHeader = new TCPHeader();
            //UDPHeader udpHeader = new UDPHeader();

            Address address = new Address();

            byte[] buffer = new byte[65535];

            uint receiveLength = 0;
            uint sendLength = 0;

            while (running)
            {
                receiveLength = 0;
                sendLength = 0;

                if (!diversion.Receive(buffer, address, ref receiveLength))
                {
                    Console.WriteLine("Failed to receive packet with error {0}", System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    continue;
                }

                diversion.ParsePacket(buffer, receiveLength, ipHeader, null, null, null, tcpHeader, null);

                Connection c = new Connection(ipHeader.SourceAddress.ToString(), tcpHeader.SourcePort, ipHeader.DestinationAddress.ToString(), tcpHeader.DestinationPort);
                if (!divertDict.ContainsKey(c))
                {
                    TcpRecon tcpRecon = new TcpRecon();
                    divertDict.Add(c, tcpRecon);
                }

                divertDict[c].reassemble_tcp(tcpHeader.SequenceNumber, 0, buffer, (ulong)buffer.Length,
                    tcpHeader.Syn != 0,
                    Convert.ToUInt32(ipHeader.SourceAddress.GetAddressBytes()),
                    Convert.ToUInt32(ipHeader.DestinationAddress.GetAddressBytes()),
                    tcpHeader.SourcePort, tcpHeader.DestinationPort);



                //if (ipHeader.Valid && tcpHeader.Valid)
                //{
                //    Console.WriteLine(
                //        "{0} IPv4 TCP packet captured destined for {1}:{2} from {3}:{4}.",
                //        address.Direction == DivertDirection.Inbound ? "Inbound" : "Outbound",
                //        ipHeader.DestinationAddress, tcpHeader.DestinationPort,
                //        ipHeader.SourceAddress, tcpHeader.SourcePort
                //        );
                //}
                //else if (ipHeader.Valid && udpHeader.Valid)
                //{
                //    Console.WriteLine(
                //        "{0} IPv4 UDP packet captured destined for {1}:{2} from {3}:{4}.",
                //        address.Direction == DivertDirection.Inbound ? "Inbound" : "Outbound",
                //        ipHeader.DestinationAddress, tcpHeader.DestinationPort,
                //        ipHeader.SourceAddress, tcpHeader.SourcePort
                //        );
                //}
                //else if (ipv6Header.Valid && tcpHeader.Valid)
                //{
                //    Console.WriteLine(
                //        "{0} IPv6 TCP packet captured destined for {1}:{2} from {3}:{4}.",
                //        address.Direction == DivertDirection.Inbound ? "Inbound" : "Outbound",
                //        ipHeader.DestinationAddress, tcpHeader.DestinationPort,
                //        ipHeader.SourceAddress, tcpHeader.SourcePort
                //        );
                //}
                //else if (ipv6Header.Valid && udpHeader.Valid)
                //{
                //    Console.WriteLine(
                //        "{0} IPv6 UDP packet captured destined for {1}:{2} from {3}:{4}.",
                //        address.Direction == DivertDirection.Inbound ? "Inbound" : "Outbound",
                //        ipHeader.DestinationAddress, tcpHeader.DestinationPort,
                //        ipHeader.SourceAddress, tcpHeader.SourcePort
                //        );
                //}

                if (address.Direction == DivertDirection.Outbound)
                {
                    diversion.CalculateChecksums(buffer, receiveLength, 0);
                }

                diversion.SendAsync(buffer, receiveLength, address, ref sendLength);
            }

            diversion.Close();
        }
    }
}
