# ClarityLibrary
Class library for building out .DLL to use with Clarity Console App

Dependencies required for insertion into projects:
FluentEmail.Core
FluentEmail.Smtp
FluentEmail.Razor
Microsoft.Extensions.DependencyModel
Serilog
Serilog.AspNetCore
Serilog.Sinks.File
Serilog.Sinks.RollingFile
Serilog.Settings.Config


Includes 3 functions through IClarityMail:


SendAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries )
Sends an email synchronously, attempting to resend {retries} number of times.
SendTestAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries )
Send( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries )
