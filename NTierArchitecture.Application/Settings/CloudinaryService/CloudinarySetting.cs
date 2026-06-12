using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTierArchitecture.Application.Settings.CloudinaryService
{
    public class CloudinarySetting
    {
        public const string SectionName = "CloudinarySetting";
        public string CloudName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string Folder { get; set; }
    }
}
