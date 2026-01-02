using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Services.OCR.Models
{
    public sealed class ExternalResultJson
    {
        public string type { get; set; } = "";
        public string id { get; set; } = "";
        public string id_confidence { get; set; } = "";
        public string name { get; set; } = "";
        public string name_confidence { get; set; } = "";
        public string address { get; set; } = "";
        public string address_confidence { get; set; } = "";
        public string nation { get; set; } = "";
        public string nation_confidence { get; set; } = "";
        public string comment { get; set; } = "";
        public bool rotate_image { get; set; }
        public bool need_save_original { get; set; }
    }
}
