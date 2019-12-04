using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceUWPSampleApp
{
    public class IdentifyResult
    {
        public string faceId { get; set; }
        public List<PersonResult> candidates { get; set; }
    }
}
