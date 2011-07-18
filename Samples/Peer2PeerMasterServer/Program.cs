﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using System.Net;

namespace Peer2PeerMasterServer
{
    

    class Program
    {
       

        static void Main(string[] args)
        {
            Dictionary<IPEndPoint, AvailableGame> registeredHosts = new Dictionary<IPEndPoint, AvailableGame>();

            NetPeerConfiguration config = new NetPeerConfiguration("masterserver");
            config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
            config.Port = 6000;

            NetPeer peer = new NetPeer(config);
            peer.Start();

            // keep going until ESCAPE is pressed
            Console.WriteLine("Press ESC to quit");
            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
            {
                NetIncomingMessage msg;
                while ((msg = peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.UnconnectedData:
                            //
                            // We've received a message from a client or a host
                            //

                            // by design, the first byte always indicates action
                            switch (msg.ReadByte())
                            {
                                case 0: // register a new game

                                    AvailableGame game = new AvailableGame();
                                    game.Count = msg.ReadInt32();
                                    game.GamerTag = msg.ReadString();
                                    game.PrivateGamerSlots =   msg.ReadInt32();
                                    game.MaxGamers = msg.ReadInt32();
                                    game.IsHost = msg.ReadBoolean();                                    
                                    game.InternalIP = msg.ReadIPEndpoint();
                                    game.ExternalIP = msg.SenderEndpoint;
                                    
                                                                       
                                    Console.WriteLine("Got registration for host " + game.ExternalIP.ToString());
                                    registeredHosts.Add(game.ExternalIP,game);
                                    break;

                                case 1:
                                    // It's a client wanting a list of registered hosts
                                    Console.WriteLine("Sending list of " + registeredHosts.Count + " hosts to client " + msg.SenderEndpoint);
                                    foreach (AvailableGame g1 in registeredHosts.Values)
                                    {
                                        // send registered host to client
                                        NetOutgoingMessage om = peer.CreateMessage();
                                        om.Write(g1.Count);
                                        om.Write(g1.GamerTag);
                                        om.Write(g1.PrivateGamerSlots);
                                        om.Write(g1.MaxGamers);
                                        om.Write(g1.IsHost);                                        
                                        om.Write(g1.InternalIP);
                                        om.Write(g1.ExternalIP);
                                        peer.SendUnconnectedMessage(om, msg.SenderEndpoint);
                                    }

                                    break;
                                case 2:
                                    // It's a client wanting to connect to a specific (external) host
                                    IPEndPoint clientInternal = msg.ReadIPEndpoint();
                                    IPEndPoint hostExternal = msg.ReadIPEndpoint();
                                    string token = msg.ReadString();

                                    Console.WriteLine(msg.SenderEndpoint + " requesting introduction to " + hostExternal + " (token " + token + ")");

                                    // find in list
                                    foreach (AvailableGame elist in registeredHosts.Values)
                                    {
                                        if (elist.ExternalIP.Equals(hostExternal))
                                        {
                                            // found in list - introduce client and host to eachother
                                            Console.WriteLine("Sending introduction...");
                                            peer.Introduce(
                                                    elist.InternalIP, // host internal
                                                    elist.ExternalIP, // host external
                                                    clientInternal, // client internal
                                                    msg.SenderEndpoint, // client external
                                                    token // request token
                                            );
                                            break;
                                        }
                                    }
                                    break;
                            }
                            break;

                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            // print diagnostics message
                            Console.WriteLine(msg.ReadString());
                            break;
                    }
                }
            }

            peer.Shutdown("shutting down");

        }
    }


    class AvailableGame
    {
        public IPEndPoint ExternalIP { get; set; }
        public IPEndPoint InternalIP { get; set; }
        public int Count { get; set; }
        public string GamerTag { get; set; }
        public int PrivateGamerSlots { get; set; }
        public int MaxGamers { get; set; }
        public bool IsHost { get; set; }
    }
}