using System.Threading.Tasks;

namespace ClarityEmailerLibrary
{
    public interface IClarityMail
    {
        public Task SendAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries);
    }
}