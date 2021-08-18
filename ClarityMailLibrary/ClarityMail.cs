using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using FluentEmail.Razor;
using FluentEmail.Smtp;

namespace ClarityEmailerLibrary
{
    public partial class ClarityMail : Component
    {
        // Private fields necessary
        private string _mailFrom;
        private string _mailTo;
        private string _mailSubject;
        private string _mailBody;
        private string _SMTPServer;
        private int _SMTPPort;

        // Public properties
        public string MailFrom
        {
            get => _mailFrom;
            set => _mailFrom = value;
        }

        public string MailTo
        {
            get => _mailTo;
            set => _mailTo = value;
        }

        public string MailSubject
        {
            get => _mailSubject;
            set => _mailSubject = value;
        }

        public string MailBody
        {
            get => _mailBody;
            set => _mailBody = value;
        }

        public string SMTPServer
        {
            get => _SMTPServer;
            set => _SMTPServer = value;
        }

        public int SMTPPort
        {
            get => _SMTPPort;
            set => _SMTPPort = value;
        }

        private static Dictionary<string, (int, bool)> _recipientsToReattempt = new();

        public ClarityMail()
        {
            _mailTo = "";
            _mailFrom = "";
            _mailSubject = "";
            _mailBody = "";
            _SMTPPort = 25;
            _SMTPServer = "";
        }
        
        public async Task RetrySendMessage(string recipientMailbox, string senderMailbox, string body, string subject, int retries)
        {
            var sender = new SmtpSender(() => new SmtpClient("localhost")
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Port = 25
            });

            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            StringBuilder template = new();

            template.AppendLine("Dear @Model.FirstName,");
            template.Append(body);
            template.AppendLine("- The Clarity Ventures Team");

            var recipientsToAttempt = _recipientsToReattempt[recipientMailbox];

            while (recipientsToAttempt.Item2 == false)
            {
                Console.WriteLine($"Resending Emails: Retry Count: {retries}");
                try
                {
                    var email = await Email
                        .From(senderMailbox, senderMailbox)
                        .To(recipientMailbox, recipientMailbox)
                        .Subject(subject)
                        .UsingTemplate(template.ToString(), new { FirstName = "Jeremy" })
                        .SendAsync();
                    recipientsToAttempt.Item2 = true;
                    Console.WriteLine($"Successfully resent to {recipientMailbox}.");
                }
                catch (SmtpException ex)
                {
                    --retries;
                    var exception = ex.Message;
                    recipientsToAttempt.Item1 = retries;
                    Console.WriteLine($"RetrySend: Retry sending to {recipientMailbox} failed: {retries}");
                    if (retries <= 0)
                    {
                        Console.WriteLine( "Failed all retries. Continuing.");
                        break;
                    }
                }
            }
        }

        public async Task SendMessages(List<string> recipientMailboxes, string senderMailbox, string body, string subject, int retries)
        {
            var sender = new SmtpSender(() => new SmtpClient("localhost")
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Port = 35,
            });

            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            StringBuilder template = new();

            template.AppendLine("Dear @Model.FirstName,");
            template.Append(body);
            template.AppendLine("- The Clarity Ventures Team");

            foreach (var recipientMailbox in recipientMailboxes)
            {
                try
                {
                    var email = await Email
                        .From(senderMailbox, senderMailbox)
                        .To(recipientMailbox, recipientMailbox)
                        .Subject(subject)
                        .UsingTemplate(template.ToString(), new { FirstName = "Jeremy" })
                        .SendAsync();
                    Console.WriteLine($"Send to {recipientMailbox} successful!");
                }
                catch (SmtpException ex)
                {
                    var exception = ex.Message;
                    Console.WriteLine($"Sending to {recipientMailbox} failed: {exception}");
                    // Add them to the database of failed recipients to retry later
                    _recipientsToReattempt.Add(recipientMailbox, (retries, false));
                }
            }

            if (_recipientsToReattempt.Count != 0)
            {
                Console.WriteLine($"SendMessage: There are {_recipientsToReattempt.Count} number of emails that failed to deliver.");
                // Iterate through each person that is on the reattempt dictionary
                foreach (var retryRecipient in _recipientsToReattempt)
                {
                    // If the last valid email status was false, then try to resend an email
                    if (retryRecipient.Value.Item2 == false)
                    {
                        // Resend email
                        try
                        {
                            await RetrySendMessage(retryRecipient.Key, senderMailbox, body, subject, retries);
                        }
                        catch (SmtpException ex)
                        {
                            retries--;
                            Console.WriteLine($"Resend failed for {retryRecipient}. Tries remaining: {retries}.");
                        }
                    }
                }
            }
        }
    }
}