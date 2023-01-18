using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI;

namespace WorldSimService.RequestHandlers
{
    public abstract class RequestHandler
    {
        public abstract WorldSimMsg HandleMsg(WorldSimMsg requestMsg);
    }
}
