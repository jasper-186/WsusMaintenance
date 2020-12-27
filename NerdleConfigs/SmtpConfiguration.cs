using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.NerdleConfigs
{
    public class SmtpConfiguration
    {
        public string SmtpFromOverride { get; set; }

        public string SmtpHost { get; set; }

        public int SmtpPort { get; set; } = 587;
        public bool SmtpUseSsl { get; set; } = true;
        public bool SmtpRequiresAuthentication { get; set; }

        public string SmtpAuthUserName { get; set; }

        public string SmtpAuthPassword { get; set; }

        public List<SmtpRecipient> Recipients { get; set; }       
    }
}
