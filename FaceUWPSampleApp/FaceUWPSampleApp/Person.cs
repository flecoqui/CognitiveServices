using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceUWPSampleApp
{
    class Person
    {
        public string personId { get; set; }
        public string name { get; set; }
        public string userData { get; set; }
        public List<string>  persistedFaceIds { get; set; }
        public override string ToString() { return name; }
    }
}
