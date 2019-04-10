using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Emgu.CV.Face.FaceRecognizer;

namespace FaceReco
{
    public class FaceRecognition
    {
        private const int MinFaceSize = 100;

        //http://alereimondo.no-ip.org/OpenCV/34/ pour récupérer des Haar Cascades
        private CascadeClassifier frontalFaceCascade = new CascadeClassifier(@"HaarCascade\haarcascade_frontalface_alt2.xml");
        private CascadeClassifier profileFaceCascade = new CascadeClassifier(@"HaarCascade\haarcascade_profileface.xml");


        public FaceRecognition()
        {

        }

        /// <summary>
        /// Recherche les visages dans toutes les images d'un répertoire
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="searchPattern"></param>
        /// <param name="recursive"></param>
        public void FindFacesInDirectory(string directoryName, string searchPattern = "*.jpg", bool recursive = false)
        {
            foreach (var item in Directory.GetFiles(directoryName, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                FindFaces(item);
        }

        /// <summary>
        /// Recherche les visages dans la photo passée en paramètre
        /// </summary>
        /// <param name="fileName"></param>
        public void FindFaces(string fileName)
        {
            DoOnFileParsing(this, new FileParsingEventArgs(fileName));
            using (Image<Bgr, byte> image = new Image<Bgr, byte>(fileName))
            using (Image<Gray, byte> gray = image.Convert<Gray, byte>())
            {
                Task<List<Rectangle>>[] tasks = new Task<List<Rectangle>>[2];
                tasks[0] = Task.Run<List<Rectangle>>(() => ParseFrontalFace(gray));
                tasks[1] = Task.Run<List<Rectangle>>(() => ParseProfileFace(gray));
                Task.WaitAll(tasks);
                List<Rectangle> allRectangles = new List<Rectangle>();
                for (int i = 0; i < 2; i++)
                    allRectangles.AddRange(tasks[i].Result);
                allRectangles = RemoveIntersectionRectangles(allRectangles);
                foreach (Rectangle rec in allRectangles)
                    DoOnFaceFound(this, new FaceFoundEventArgs(rec, fileName));
            }
        }

        public event EventHandler<FileParsingEventArgs> OnFileParsing;
        private void DoOnFileParsing(object sender, FileParsingEventArgs args)
        {
            if (OnFileParsing != null)
                OnFileParsing(sender, args);
        }

        /// <summary>
        /// Supprime les rectangles qui sont en intersections entre eux (garde le premier trouvé)
        /// </summary>
        /// <param name="allRectangles"></param>
        private List<Rectangle> RemoveIntersectionRectangles(List<Rectangle> allRectangles)
        {
            List<Rectangle> keepedRectangles = new List<Rectangle>();
            for (int i = 0; i < allRectangles.Count; i++)
            {
                if (!IntersectWithOthers(allRectangles[i], keepedRectangles))
                    keepedRectangles.Add(allRectangles[i]);
            }
            return keepedRectangles;
        }

        /// <summary>
        /// Vérifie si un rectangle est en intersection avec au moins un autre rectangle de la liste
        /// </summary>
        /// <param name="srcRec"></param>
        /// <param name="keepedRectangles"></param>
        /// <returns></returns>
        private bool IntersectWithOthers(Rectangle srcRec, List<Rectangle> keepedRectangles)
        {
            foreach (Rectangle rec in keepedRectangles)
            {
                if (!rec.Equals(srcRec) && rec.IntersectsWith(srcRec))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Recherche des visages de profile
        /// </summary>
        /// <param name="gray"></param>
        /// <returns></returns>
        private List<Rectangle> ParseProfileFace(Image<Gray, byte> gray)
        {
            List<Rectangle> result = new List<Rectangle>();
            Rectangle[] faces = frontalFaceCascade.DetectMultiScale(gray, 1.1, 5, new Size(20, 20));

            foreach (Rectangle rec in faces)
            {
                if (rec.Width > MinFaceSize && rec.Height > MinFaceSize)
                    result.Add(rec);
            }
            return result;
        }

        /// <summary>
        /// Recherche des visages de face
        /// </summary>
        /// <param name="gray"></param>
        /// <returns></returns>
        private List<Rectangle> ParseFrontalFace(Image<Gray, byte> gray)
        {
            List<Rectangle> result = new List<Rectangle>();
            Rectangle[] faces = profileFaceCascade.DetectMultiScale(gray, 1.1, 5, new Size(20, 20));

            foreach (Rectangle rec in faces)
            {
                if (rec.Width > MinFaceSize && rec.Height > MinFaceSize)
                    result.Add(rec);
            }
            return result;

        }

        public delegate void FaceFound(object sender, FaceFoundEventArgs args);
        /// <summary>
        /// Lorsqu'un visage est trouvé
        /// </summary>
        public event FaceFound OnFaceFound;
        private void DoOnFaceFound(object sender, FaceFoundEventArgs args)
        {
            if (OnFaceFound != null)
                OnFaceFound(sender, args);
        }

        /// <summary>
        /// Sauvegarde une partie de l'image source délimité par rec dans l'image destination au format d'une image d’entraînement
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="rec"></param>
        /// <param name="destFileName"></param>
        public static void SaveTrainingImage(string sourceFileName, Rectangle rec, string destFileName)
        {
            using (Image<Bgr, byte> srcImage = new Image<Bgr, byte>(sourceFileName))
            {
                using (var destImage = srcImage.Copy(rec).Convert<Gray, byte>().Resize(100, 100, Inter.Cubic))
                    destImage.Save(destFileName);
            }
        }

        /// <summary>
        /// Création d'un fichier d’entraînement
        /// </summary>
        /// <param name="labels">Dictionnaire Id de libellé / libellé</param>
        /// <param name="files">Liste des couples fichier / Id de libellé</param>
        /// <param name="labelsFileName">Nom du fichier des correspondances Id de libellé / Libellé</param>
        /// <param name="recognizerFileName">Nom du fichier de reconnaissance</param>
        public void CreateTrainingFaceRecognizerFile(Dictionary<int, string> labels, List<Tuple<string, int>> files, string labelsFileName, string recognizerFileName)
        {
            List<Image<Gray, byte>> images = new List<Image<Gray, byte>>();
            List<int> labelIds = new List<int>();
            try
            {
                foreach (var item in files)
                {
                    images.Add(new Image<Bgr, byte>(item.Item1).Convert<Gray, byte>());
                    labelIds.Add(item.Item2);
                }

                using (EigenFaceRecognizer frz = new EigenFaceRecognizer(images.Count, 3000))
                {
                    frz.Train(images.ToArray(), labelIds.ToArray());
                    frz.Write(recognizerFileName);
                }
            }
            finally
            {
                images.ForEach(i => i.Dispose());
                images.Clear();
            }

            StringBuilder sb = new StringBuilder();
            foreach (var item in labels)
                sb.AppendLine($"{item.Key}|{item.Value}");
            File.WriteAllText(labelsFileName, sb.ToString());
        }

        /// <summary>
        /// Récupère les couples Id de libellé / Libellé
        /// </summary>
        /// <param name="fileName">Nom du fichier des libellés</param>
        /// <returns></returns>
        private Dictionary<int, string> GetLabels(string fileName)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            string[] labels = File.ReadAllLines(fileName);
            foreach (string item in labels)
            {
                string[] data = item.Split('|');
                result.Add(int.Parse(data[0]), data[1]);
            }
            return result;
        }

        private Dictionary<int, string> _faceLabels;
        EigenFaceRecognizer _currentFaceRecognizer;

        /// <summary>
        /// Reconnaît les visages dans toutes les images d'un répertoire
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="searchPattern"></param>
        /// <param name="recursive"></param>
        public void RecognizeFacesInDirectory(string directoryName, string labelsFileName, string recognizerFileName, string searchPattern = "*.jpg",  bool recursive = false)
        {
            _faceLabels = GetLabels(labelsFileName);
            using (EigenFaceRecognizer faceRecognizer = new EigenFaceRecognizer())
            {
                _currentFaceRecognizer = faceRecognizer;
                faceRecognizer.Read(recognizerFileName);
                foreach (var item in Directory.GetFiles(directoryName, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    RecognizeFaces(item);
            }
        }

        /// <summary>
        /// Reconnaît les visages dans une image
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="labelsFileName"></param>
        /// <param name="recognizerFileName"></param>
        private void RecognizeFaces(string fileName, string labelsFileName, string recognizerFileName)
        {
            _faceLabels = GetLabels(labelsFileName);
            using (EigenFaceRecognizer faceRecognizer = new EigenFaceRecognizer())
            {
                _currentFaceRecognizer = faceRecognizer;
                faceRecognizer.Read(recognizerFileName);
                RecognizeFaces(fileName);
            }
        }

        /// <summary>
        /// Reconnaît les visages dans la photo passée en paramètres
        /// </summary>
        /// <param name="fileName"></param>
        private void RecognizeFaces(string fileName)
        {
            this.OnFaceFound += RecognizeFaces;
            try
            {
                FindFaces(fileName);
            }
            finally
            {
                this.OnFaceFound -= RecognizeFaces;
            }
        }

        private void RecognizeFaces(object sender, FaceFoundEventArgs args)
        {
            using (Image<Bgr, byte> image = new Image<Bgr, byte>(args.FileName))
            {
                using (var face = image.Copy(args.Face).Convert<Gray, byte>().Resize(100, 100, Inter.Cubic))
                {
                    PredictionResult prediction = _currentFaceRecognizer.Predict(face);
                    if (prediction.Label > -1)
                        DoOnFaceRecognized(this,
                            new FaceRecognizedEventArgs()
                            {
                                Face = args.Face,
                                FileName = args.FileName,
                                LabelId = prediction.Label,
                                Label = _faceLabels[prediction.Label]
                            });
                }
            }
        }

        public delegate void FaceRecognized(object sender, FaceRecognizedEventArgs args);
        /// <summary>
        /// Lorsqu'un visage est reconnu
        /// </summary>
        public event FaceRecognized OnFaceRecognized;
        private void DoOnFaceRecognized(object sender, FaceRecognizedEventArgs args)
        {
            if (OnFaceRecognized != null)
                OnFaceRecognized(sender, args);
        }
    }
}
