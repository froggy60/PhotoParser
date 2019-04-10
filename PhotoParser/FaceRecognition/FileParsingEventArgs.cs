using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceReco
{
    public class FileParsingEventArgs : EventArgs
    {
        public FileParsingEventArgs(string fileName)
        {
            FileName = fileName;
        }
        public string  FileName { get; set; }
    }
}
