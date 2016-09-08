﻿using Engine.Api.ServerCommands;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Linq;
using System.Security;

namespace Engine.Api.ClientCommands
{
  [SecurityCritical]
  class ClientReadFilePartCommand :
    ClientCommand<ClientReadFilePartCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.ReadFilePart;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    protected override bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return true; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (content.File == null)
        throw new ArgumentNullException("File");

      if (content.Length <= 0)
        throw new ArgumentException("Length <= 0");

      if (content.StartPartPosition < 0)
        throw new ArgumentException("StartPartPosition < 0");

      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("roomName");

      using(var client = ClientModel.Get())
      {
        if (!client.PostedFiles.Exists(c => c.File.Equals(content.File)))
        {
          var fileNotPostContent = new ServerRemoveFileFromRoomCommand.MessageContent
          {
            FileId = content.File.Id,
            RoomName = content.RoomName,
          };

          ClientModel.Client.SendMessage(ServerRemoveFileFromRoomCommand.CommandId, fileNotPostContent);
          return;
        }
      }

      var sendingContent = new ClientWriteFilePartCommand.MessageContent
      {
        File = content.File,
        StartPartPosition = content.StartPartPosition,
        RoomName = content.RoomName,
      };

      var partSize = content.File.Size < content.StartPartPosition + content.Length
        ? content.File.Size - content.StartPartPosition
        : content.Length;

      var part = new byte[partSize];

      using (var client = ClientModel.Get())
      {
        var sendingFileStream = client.PostedFiles.First(c => c.File.Equals(content.File)).ReadStream;
        sendingFileStream.Position = content.StartPartPosition;
        sendingFileStream.Read(part, 0, part.Length);
      }

      ClientModel.Peer.SendMessage(args.PeerConnectionId, ClientWriteFilePartCommand.CommandId, sendingContent, part);
    }

    [Serializable]
    public class MessageContent
    {
      private string _roomName;
      private FileDescription _file;
      private long _startPartPosition;
      private long _length;

      public string RoomName
      {
        get { return _roomName; }
        set { _roomName = value; }
      }

      public FileDescription File
      {
        get { return _file; }
        set { _file = value; }
      }

      public long StartPartPosition
      {
        get { return _startPartPosition; }
        set { _startPartPosition = value; }
      }

      public long Length
      {
        get { return _length; }
        set { _length = value; }
      }
    }
  }
}
