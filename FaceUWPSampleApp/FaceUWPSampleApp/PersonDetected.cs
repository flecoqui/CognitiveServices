using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceUWPSampleApp
{
    class PersonDetected
    {
        public string personId { get; set; }
        public string name { get; set; }
        public Rectangle rect { get; set; }
        public override string ToString() { return name; }
    }
}
