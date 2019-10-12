using System.Threading;
using System.Threading.Tasks;
using RoutineBot.Repository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace RoutineBot.Telegram
{
    public class Message
    {
        public int? UpdateMessageId;
        public string Text;
        public IReplyMarkup Keyboard;
    }
}