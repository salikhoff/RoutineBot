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
        Task Initialize(ITelegramBotClient client, Update update);
        Task ProcessUpdate(ITelegramBotClient client, Update update);
        bool Finished { get; }
    }
}