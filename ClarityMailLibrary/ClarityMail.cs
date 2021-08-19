using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ClarityEmailerLibrary
{
    public class ClarityMail : IClarityMail
    {
        #region Private
        private int _id;
        private string _mailFrom;
        private string _mailTo;
        private string _mailSubject;
        private string _mailBody;
        private string _SMTPServer;
        private int _SMTPPort;
    #endregion
        // Public properties
        #region Public

        public int Id
        {
            get => _id;
            set => _id = value;
        }

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

        #endregion

        public async Task SendAsync(string recipientName, string recipientEmailAddress, string senderMailbox, string subject, string body, int retries)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            
            var claritySettings = configuration.GetSection("ClarityMail");

            var sender = new SmtpSender(() => new SmtpClient( claritySettings["Host"])
            {
                Port = int.Parse(claritySettings["Port"]),
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            });
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.RollingFile(@"C:\Logs\ClarityMailLibrary-{Date}A.txt", retainedFileCountLimit: 10 ) 
                .CreateLogger();
            
            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            StringBuilder template = new();
            template.AppendLine("Dear @Model.FirstName");
            template.Append(body);
            template.Append("- The Clarity Ventures Team");

            for (var count = 1; count <= retries; count++)
            {
                try
                {
                    await Email
                        .From(senderMailbox, senderMailbox)
                        .To(recipientEmailAddress, recipientName)
                        .Subject(subject)
                        .UsingTemplate(template.ToString(), new { FirstName = "Jeremy" })
                        .SendAsync();

                    Log.Information( $"SENDASYNC SUCCESS: TO:{recipientEmailAddress} FROM:{senderMailbox} SUBJECT:{subject} BODY:{body}");
                    return;
                }
                catch (Exception ex)
                {
                    var exception = ex.Message;
                    Log.Warning( $"SENDASYNC FAIL: TO:{recipientEmailAddress} FROM:{senderMailbox} SUBJECT:{subject} BODY:{body}  : Attempt {count} of {retries}.");

                    if (count >= retries)
                    {
                        continue;
                    }

                    await Task.Delay(count * 1000);
                }
            }
        }
    }
}