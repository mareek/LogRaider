using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogRaider.Configuration
{
    public class LogSource
    {
        public enum PathType
        {
            File,
            Url
        }

        [XmlElement]
        public string Path { get; set; }

        [XmlAttribute]
        public PathType Type { get; set; }
    }
}
