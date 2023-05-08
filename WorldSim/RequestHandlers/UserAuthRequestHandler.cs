using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using WorldSimAPI;
using WorldSimAPI.ContentMsg;
using WorldSimLib;

namespace WorldSimService.RequestHandlers
{
    public class UserAuthRequestHandler : RequestHandler
    {
        Dictionary<string, string> UsersAndKeys = new Dictionary<string, string>();

        public delegate void OnNewUserRequestHandler(string name);
        public delegate void OnAuthUserRequestHandler(string name);

        public OnNewUserRequestHandler OnNewUserRequest;

        public override WorldSimMsg HandleMsg(WorldSimMsg requestMsg)
        {
            UserAuthQueryMsg msgContent = JsonConvert.DeserializeObject<UserAuthQueryMsg>(requestMsg.Content);
            UserAuthContentMsg contentMsg = new UserAuthContentMsg();
            contentMsg.Name = msgContent.Name;
            contentMsg.UniqueKey = msgContent.UniqueKey;

            UserAuthReplyMsg replyMsg = new UserAuthReplyMsg(contentMsg);
            replyMsg.WasSuccessful = false;

            // If we have a matching user, check the key
            if ( UsersAndKeys.ContainsKey( msgContent.Name ) )
            {
                if( UsersAndKeys[msgContent.Name] == msgContent.UniqueKey )
                {
                    replyMsg.WasSuccessful = true;
                }
            } else
            {
                UsersAndKeys.Add(msgContent.Name, msgContent.UniqueKey);
                replyMsg.WasSuccessful = true;
            }

            string json = JsonConvert.SerializeObject(replyMsg);

            requestMsg.Content = json;

            return requestMsg;
        }
    }
}
