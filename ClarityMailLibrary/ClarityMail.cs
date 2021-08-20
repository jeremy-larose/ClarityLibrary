using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ClarityMailLibrary
{
    /// <summary>
    /// Basic email sender with retries functionality. Sends emails both sync and async.
    /// Writes to logs at C:\Logs\ClarityMail using Serilog pertaining to success/failure of emails sent.
    /// Gets config info from appsettings.json, under the heading "ClarityMail" with Host and Port options.
    /// </summary>
    public class ClarityMail : IClarityMail
    {
        
        #region Private
        private int _id;
        private string _mailFrom;
        private string _mailTo;
        private string _mailSubject;
        private string _mailBody;
        private string _mailDisplayName;
        private string _SMTPServer;
        private int _SMTPPort;
        
    #endregion
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

        public string MailDisplayName
        {
            get => _mailDisplayName;
            set => _mailDisplayName = value;
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

        /// <summary>
        /// Sends a single email synchronously, attempting to retry {retries} number of times on failure.
        /// </summary>
        /// <param name="recipientName">The name of the recipient, to insert into the "Hello {recipient}," field.</param>
        /// <param name="recipientEmailAddress">The email address of the person to receive the email.</param>
        /// <param name="senderMailbox">The email address of the sender.</param>
        /// <param name="subject">The data to insert into the subject of the email.</param>
        /// <param name="body">The body of the email, inserted using Razor Template.</param>
        /// <param name="retries">The number of times to attempt to resend in the event of failure.</param>
        public void Send(string recipientName, string recipientEmailAddress, string senderMailbox, string subject, string body, int retries)
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
                Credentials = new NetworkCredential( claritySettings["Mail"], claritySettings["Password"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            });
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.RollingFile(@"C:\Logs\ClarityMail\ClarityMailLibrary-{Date}.txt", retainedFileCountLimit: 10 ) 
                .CreateLogger();
            
            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();
            
            for (var count = 1; count <= retries; count++)
            {
                try
                {
                    Email
                        .From(senderMailbox, senderMailbox)
                        .To(recipientEmailAddress, recipientName)
                        .Subject(subject)
                        //.UsingTemplate(template.ToString(), new { FirstName = $"{recipientName}" })
                        .Body( body, true )
                        .SendAsync();

                    Log.Information( $"SEND SUCCESS: TO:{recipientEmailAddress} FROM:{senderMailbox} SUBJECT:{subject} BODY:{body}");
                }
                catch (Exception ex)
                {
                    var exception = ex.Message;
                    Log.Warning( $"SEND FAIL: TO:{recipientEmailAddress} FROM:{senderMailbox} SUBJECT:{subject} BODY:{body}  : Attempt {count} of {retries}.");

                    if (count >= retries)
                    {
                        continue;
                    }

                    Task.Delay(count * 1000);
                }
            }
        }
        
        /// <summary>
        /// Sends an email asynchronously, attempting to retry {retries} number of times upon failure.
        /// </summary>
        /// <param name="recipientName">The name of the recipient, to insert into the "Hello {recipient}," field.</param>
        /// <param name="recipientEmailAddress">The email address of the person to receive the email.</param>
        /// <param name="senderMailbox">The email address of the sender.</param>
        /// <param name="subject">The data to insert into the subject of the email.</param>
        /// <param name="body">The body of the email, inserted using Razor Template.</param>
        /// <param name="retries">The number of times to attempt to resend in the event of failure.</param>
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
                Credentials = new NetworkCredential( claritySettings["Mail"], claritySettings["Password"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            });
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.RollingFile(@"C:\Logs\ClarityMail\ClarityMailLibrary-{Date}A.txt", retainedFileCountLimit: 10 ) 
                .CreateLogger();
            
            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            for (var count = 1; count <= retries; count++)
            {
                try
                {
                    await Email
                        .From(senderMailbox, claritySettings["DisplayName"])
                        .To(recipientEmailAddress, recipientName)
                        .Subject(subject)
                        //.UsingTemplate(template.ToString(), new { FirstName = $"{recipientName}" })
                        .Body( body, true )
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
        
        /// <summary>
        /// Sends an email asynchronously, attempting to retry {retries} number of times upon failure. Uses development environment for testing with Papercut.
        /// </summary>
        /// <param name="recipientName">The name of the recipient, to insert into the "Hello {recipient}," field.</param>
        /// <param name="recipientEmailAddress">The email address of the person to receive the email.</param>
        /// <param name="senderMailbox">The email address of the sender.</param>
        /// <param name="subject">The data to insert into the subject of the email.</param>
        /// <param name="body">The body of the email, inserted using Razor Template.</param>
        /// <param name="retries">The number of times to attempt to resend in the event of failure.</param>
        public async Task SendTestAsync(string recipientName, string recipientEmailAddress, string senderMailbox, string subject, string body, int retries)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            
            var claritySettings = configuration.GetSection("ClarityMail");

            var sender = new SmtpSender(() => new SmtpClient( "localhost" )
            {
                Port = 35,
                Credentials = new NetworkCredential( claritySettings["Mail"], claritySettings["Password"]),
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            });
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.RollingFile(@"C:\Logs\ClarityMail\ClarityMailLibrary-{Date}A.txt", retainedFileCountLimit: 10 ) 
                .CreateLogger();
            
            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();
            
            for (var count = 1; count <= retries; count++)
            {
                try
                {
                    await Email
                        .From(senderMailbox, claritySettings["DisplayName"])
                        .To(recipientEmailAddress, recipientName)
                        .Subject(subject)
                        //.UsingTemplate(template.ToString(), new { FirstName = $"{recipientName}" })
                        .Body( body, true )
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