using FxttMonitorNotifier.Droid.Services.Implementations;

using Xamarin.Forms;

[assembly: Dependency(typeof(MessagesApiProvider))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    using FxttMonitorNotifier.Droid.Models.Api;

    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessagesApiProvider
    {
        IEnumerable<Message> RetreiveAllMesages();
        IEnumerable<Message> RetreiveSpecificMessages();

        Task<UpdateMessageStateReply> AcceptMessageAsync(Message message);
        Task<UpdateMessageStateReply> RejectMessageAsync(Message message);
    }
}