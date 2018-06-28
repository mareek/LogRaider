using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogRaider.Configuration
{
    public class LogConfiguration
    {
        [XmlElement]
        public LogSource Source { get; set; }

        [XmlElement]
        public DateTime LastDownloadDate { get; set; }

        [XmlElement]
        public string FileDateFormat { get; set; }

        public static LogConfiguration GetDefault() => new LogConfiguration
        {
            LastDownloadDate = new DateTime(2018, 5, 1),
            FileDateFormat = @"\Debu\g.lo\gyyyy-MM-dd",
            Source = new LogSource
            {
                Type = LogSource.PathType.File,
                Path = @"\\dcvsvf109.d70.tes.local\ProsperDataFiles\Log"
            }
        };
    }
}
