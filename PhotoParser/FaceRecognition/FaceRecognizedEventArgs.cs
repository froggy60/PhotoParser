using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceReco
{
    public class FaceRecognizedEventArgs : EventArgs
    {
        public Rectangle Face { get; set; }
        public string FileName { get; set; }
        public int LabelId { get; set; }
        public string Label { get; set; }
    }
}
