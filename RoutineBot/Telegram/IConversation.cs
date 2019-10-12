using System.Threading;
using System.Threading.Tasks;
using RoutineBot.Repository;
using Telegram.Bot;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace RoutineBot.Telegram
{
    public interface IConversation
    {
        Message Initialize(long chatId);
        Message ProcessUpdate(Update update);
        bool Finished { get; }
    }
}