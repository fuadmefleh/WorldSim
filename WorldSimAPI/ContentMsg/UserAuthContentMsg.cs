using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimAPI.ContentMsg
{
    public class UserAuthContentMsg
    {
        public string Name { get; set; }
        public string UniqueKey { get; set; }
    }

    public class UserAuthQueryMsg
    {
        public string Name { get; set; }
        public string UniqueKey { get; set; }

        public UserAuthQueryMsg()
        {

        }
    }

    public class UserAuthReplyMsg
    {
        public UserAuthContentMsg Content { get; set; }

        public bool WasSuccessful { get; set; }

        public UserAuthReplyMsg(UserAuthContentMsg content)
        {
            Content = content;
        }
    }
}
