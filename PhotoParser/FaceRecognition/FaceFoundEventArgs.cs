using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceReco
{
    public class FaceFoundEventArgs : EventArgs
    {
        public FaceFoundEventArgs(Rectangle face, string fileName)
        {
            Face = face;
            FileName = fileName;
        }
        public Rectangle Face { get; set; }
        public string FileName { get; set; }
    }
}
