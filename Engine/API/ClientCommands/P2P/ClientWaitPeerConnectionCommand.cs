﻿using Engine.Api.ServerCommands;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Net;
using System.Security;

namespace Engine.Api.ClientCommands
{
  [SecurityCritical]
  class ClientWaitPeerConnectionCommand :
    ClientCommand<ClientWaitPeerConnectionCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.WaitPeerConnection;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.RemoteInfo == null)
        throw new ArgumentNullException("info");

      if (content.RequestPoint == null)
        throw new ArgumentNullException("request point");

      if (content.SenderPoint == null)
        throw new ArgumentNullException("sender point");

      ClientModel.Peer.WaitConnection(content.SenderPoint);

      var sendingContent = new ServerP2PReadyAcceptCommand.MessageContent
      {
        PeerPoint = content.RequestPoint,
        ReceiverNick = content.RemoteInfo.Nick
      };

      using (var client = ClientModel.Get())
        sendingContent.RemoteInfo = client.User;

      ClientModel.Client.SendMessage(ServerP2PReadyAcceptCommand.CommandId, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      private User _remoteInfo;
      private IPEndPoint _senderPoint;
      private IPEndPoint _requestPoint;

      public User RemoteInfo
      {
        get { return _remoteInfo; }
        set { _remoteInfo = value; }
      }

      public IPEndPoint SenderPoint
      {
        get { return _senderPoint; }
        set { _senderPoint = value; }
      }

      public IPEndPoint RequestPoint
      {
        get { return _requestPoint; }
        set { _requestPoint = value; }
      }
    }
  }
}
