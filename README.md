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

For appsettings.json, include the following: 
```
  "ClarityMail" : {
    "Host": "localhost",
    "Port": 35,
    "Mail": "{outgoing email address}",
    "Password" : "",
    "DisplayName" : "Jeremy LaRose"
  }
  ```
  
Includes 3 functions through IClarityMail:

````C#
SendAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries )
````
Sends an email synchronously, attempting to resend {retries} number of times.


[Used for testing purposes for Papercut, doesn't send actual email]
````C#
SendTestAsync( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries )

Send( string recipientName, string recipientMailbox, string senderMailbox, string body, string subject, int retries )
````
