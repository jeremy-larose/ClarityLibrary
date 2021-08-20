using System.Threading.Tasks;

namespace ClarityMailLibrary
{
    public interface IClarityMail
    {
        public Task SendAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries);
        public Task SendTestAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries);
        public void Send(string recipientName, string RecipientMailbox, string senderMailbox, string body, string subject, int retries);
    }
}