using MailKit.Net.Smtp;
using Microsoft.Data.SqlClient;
using Microsoft.UpdateServices.Administration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;
using WSUSMaintenance.Model;

namespace WSUSMaintenance.OtherStep
{
    public class SendEmail
    {
        // Should probably be static in the future
        public void SendCompletionEmail(WsusMaintenanceConfiguration config, string logFile)
        {
            if (string.IsNullOrWhiteSpace(config?.Database?.ConnectionString))
            {
                var messages = new Dictionary<ResultMessageType, IList<string>>();
                messages.Add(ResultMessageType.Error, new List<string>() { "Config not set properly" });
                return;
            }

            var emailSettings = GetEmailSettings(config);
            if (!CanSendEmail(emailSettings))
            {
                // If its not set up or invalid addresses bail.
                return;
            }

            // Put Together Email Body
            var emailBody = System.IO.File.ReadAllText(logFile, Encoding.UTF8);
            var mailList = new List<MimeMessage>();
            foreach (var recipient in emailSettings.Recipients)
            {
                if (!IsValidEmail(recipient)) continue;

                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(emailSettings.SmtpUserDisplayName, emailSettings.SmtpUserMailAddress));
                mailMessage.To.Add(new MailboxAddress(recipient, recipient));
                mailMessage.Subject = "WSUS Maintenance Log";
                mailMessage.Body = new TextPart("plain")
                {
                    Text = emailBody
                };

                mailList.Add(mailMessage);
            }

            if (mailList.Count < 1)
            {
                return;
            }

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Connect(emailSettings.SmtpHostName, emailSettings.SmtpPort, emailSettings.SmtpUseSsl);
                if (emailSettings.SmtpServerRequireAuthentication)
                {
                    smtpClient.Authenticate(emailSettings.SmtpUserName, emailSettings.SmtpPassword);
                }

                foreach (var mailMessage in mailList)
                {
                    smtpClient.Send(mailMessage);
                }

                smtpClient.Disconnect(true);
            }

            return;
        }

        private bool CanSendEmail(WsusEmailSettings settings)
        {
            if (settings.Recipients.Count < 1) return false;
            if (string.IsNullOrWhiteSpace(settings.SmtpHostName)) return false;
            if (string.IsNullOrWhiteSpace(settings.SmtpUserMailAddress)) return false;
            if (settings.SmtpPort == 0) return false;

            // Check for at least 1 valid Email address
            if (settings.Recipients.All(e => !IsValidEmail(e))) return false;

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private WsusEmailSettings GetEmailSettings(WsusMaintenanceConfiguration config)
        {
            // Get Wsus Email Configuration
            var smtpEmail = new WsusEmailSettings();
            using (var dbconnection = new SqlConnection(config.Database.ConnectionString))
            {
                dbconnection.Open();
                var cmd = dbconnection.CreateCommand();
                cmd.CommandText = @"
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'SmtpHostName'
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'SmtpPort'
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'SmtpServerRequireAuthentication'
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'SmtpUserDisplayName'
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'SmtpUserMailAddress'
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'SmtpUserName'
                SELECT Value FROM dbo.tbConfiguration WHERE [Name] = 'EmailNeedToSendStatusNotification'
                SELECT Distinct EmailAddress FROM dbo.tbEmailNotificationRecipient Where NotificationType=3";
                cmd.CommandTimeout = 0;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        smtpEmail.SmtpHostName = reader[0].ToString();
                    }
                    reader.NextResult();

                    if (reader.Read())
                    {
                        if (int.TryParse(reader[0].ToString(), out int port))
                        {
                            smtpEmail.SmtpPort = port;
                        }
                    }
                    reader.NextResult();

                    if (reader.Read())
                    {
                        if (bool.TryParse(reader[0].ToString(), out bool auth))
                        {
                            smtpEmail.SmtpServerRequireAuthentication = auth;
                        }
                    }
                    reader.NextResult();

                    if (reader.Read())
                    {
                        smtpEmail.SmtpUserDisplayName = reader[0].ToString();
                    }
                    reader.NextResult();

                    if (reader.Read())
                    {
                        smtpEmail.SmtpUserMailAddress = reader[0].ToString();
                    }
                    reader.NextResult();

                    if (reader.Read())
                    {
                        smtpEmail.SmtpUserName = reader[0].ToString();
                    }
                    reader.NextResult();

                    // Check if we should be sending Emails from WSUS
                    if (reader.Read())
                    {
                        if (bool.TryParse(reader[0].ToString(), out bool sendStatusEmails))
                        {
                            if (sendStatusEmails)
                            {
                                // If so, get the List of Recipients
                                reader.NextResult();
                                while (reader.Read())
                                {
                                    smtpEmail.Recipients.Add(reader[0].ToString());
                                }
                            }
                        }
                    }
                }
            }

            // Apply Overrides from Config
            smtpEmail.SmtpHostName = config?.Smtp?.SmtpHost ?? smtpEmail.SmtpHostName;
            smtpEmail.SmtpServerRequireAuthentication = config?.Smtp?.SmtpRequiresAuthentication ?? smtpEmail.SmtpServerRequireAuthentication;
            smtpEmail.SmtpUserMailAddress = config?.Smtp?.SmtpFromOverride ?? smtpEmail.SmtpUserMailAddress;
            smtpEmail.SmtpUserName = config?.Smtp?.SmtpAuthUserName ?? smtpEmail.SmtpUserName;
            smtpEmail.SmtpPassword = config?.Smtp?.SmtpAuthPassword;

            if ((config?.Smtp?.SmtpPort >> 0) > 0)
            {
                // if we override the port, make sure to override the SSL settings
                smtpEmail.SmtpPort = config?.Smtp?.SmtpPort ?? smtpEmail.SmtpPort;
                smtpEmail.SmtpUseSsl = config.Smtp.SmtpUseSsl;
            }

            // Append Emails to End of List
            foreach (var r in config?.Smtp?.Recipients)
            {
                smtpEmail.Recipients.Add(r.Email);
            }

            return smtpEmail;
        }
    }
}
