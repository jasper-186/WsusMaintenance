using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.Model
{
    public class WsusEmailSettings
    {
        public string SmtpHostName { get; set; }
        public int SmtpPort { get; set; } = 587;
        public bool SmtpServerRequireAuthentication { get; set; } = false;
        public string SmtpUserDisplayName { get; set; }
        public string SmtpUserMailAddress { get; set; }
        public string SmtpUserName { get; set; }
        public bool SmtpUseSsl { get; set; } = true;
        public List<string> Recipients { get; set; } = new List<string>();

    }
}
