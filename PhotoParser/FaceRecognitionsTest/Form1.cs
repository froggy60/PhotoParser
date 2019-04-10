using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FaceReco;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceRecognitionsTest
{
    public partial class Form1 : Form
    {
        private string _lastFileName;
        private Image<Bgr, byte> _lastImage;
        private string _workingDir;
        private string _defaultWorkingDir;

        public Form1()
        {
            InitializeComponent();
            _workingDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestImages");
            _defaultWorkingDir = _workingDir;
            textBox1.Text = _workingDir;
        }

        private bool isDefaultDir()
        {
            return _workingDir == _defaultWorkingDir;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            _workingDir = textBox1.Text;
            Task.Run(() =>
            {
                if (Directory.Exists(Path.Combine(_workingDir, "Faces")))
                    Directory.Delete(Path.Combine(_workingDir, "Faces"), true);
                FaceRecognition faceRecognition = new FaceRecognition();
                faceRecognition.OnFileParsing += FaceRecognition_OnFileParsing;
                faceRecognition.OnFaceFound += FaceRecognition_OnFaceFound;
                Invoke(new Action(() => label2.Text = "En cours..."));
                faceRecognition.FindFacesInDirectory(_workingDir);
                Invoke(new Action(() => label2.Text = "Terminé"));
            });

        }

        private void FaceRecognition_OnFaceFound(object sender, FaceFoundEventArgs args)
        {
            if (_lastFileName != args.FileName)
                _lastImage = new Image<Bgr, byte>(args.FileName);
            _lastFileName = args.FileName;
            _lastImage.Draw(args.Face, new Bgr(Color.Red), 4);
            Invoke(new Action(() => iMain.Image = _lastImage));

            string fileName = Path.Combine(Path.GetDirectoryName(args.FileName), "Faces",
                isDefaultDir() ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(args.FileName)) : String.Empty,
                Guid.NewGuid().ToString() + ".jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            FaceRecognition.SaveTrainingImage(args.FileName, args.Face, fileName);
            Invoke(new Action(() =>
            {
                imFace.Load(fileName);
                Application.DoEvents();
            }));
        }

        private void FaceRecognition_OnFileParsing(object sender, FileParsingEventArgs e)
        {
            Invoke(new Action(() =>
            {
                label2.Text = e.FileName;
                Application.DoEvents();
            }));
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                FaceRecognition faceRecognition = new FaceRecognition();

                int i = 0;
                Dictionary<int, string> labels = new Dictionary<int, string>();
                List<Tuple<string, int>> files = new List<Tuple<string, int>>();

                foreach (var dir in Directory.GetDirectories(Path.Combine(textBox1.Text, "Faces")))
                {
                    labels.Add(i, Path.GetFileName(dir));

                    foreach (var file in Directory.GetFiles(dir))
                        files.Add(new Tuple<string, int>(file, i));
                    i++;
                }

                string outDir = Path.Combine(textBox1.Text, "Faces");
                faceRecognition.CreateTrainingFaceRecognizerFile(labels, files, Path.Combine(outDir, "labels.txt"), Path.Combine(outDir, "recognitions.txt"));
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
            MessageBox.Show("Fichiers créés avec succès.");
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            _workingDir = textBox1.Text;
            Task.Run(() =>
            {
                FaceRecognition faceRecognition = new FaceRecognition();
                faceRecognition.OnFaceRecognized += FaceRecognition_OnFaceRecognized;
                faceRecognition.OnFileParsing += FaceRecognition_OnFileParsing;
                Invoke(new Action(() => label2.Text = "En cours..."));
                
                string outDir = Path.Combine(_workingDir, "Faces");
                faceRecognition.RecognizeFacesInDirectory(textBox1.Text, Path.Combine(outDir, "labels.txt"), Path.Combine(outDir, "recognitions.txt"));
                Invoke(new Action(() => label2.Text = "Terminé"));
            });
        }

        private void FaceRecognition_OnFaceRecognized(object sender, FaceRecognizedEventArgs args)
        {
            if (_lastFileName != args.FileName)
            {
                _lastImage = new Image<Bgr, byte>(args.FileName);
                Invoke(new Action(() => label3.Text = ""));
            }
            _lastFileName = args.FileName;
            _lastImage.Draw(args.Face, new Bgr(Color.Blue), 4);
            _lastImage.Draw(args.Label, new Point(args.Face.X - 2, args.Face.Y + args.Face.Height + 100), FontFace.HersheyPlain, 4, new Bgr(Color.LimeGreen), 8, LineType.Filled);
            Invoke(new Action(() =>
            {
                iMain.Image = _lastImage;
                label3.Text = args.Label;
                imFace.Image = _lastImage.Copy(args.Face);

                Application.DoEvents();
            }));
        }
    }
}
