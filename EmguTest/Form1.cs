using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;

using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.OCR;
using Emgu.CV.Util;
using Emgu.CV.Reflection;

using Tesseract;

namespace EmguTest
{
    public partial class Form1 : Form
    {
        private Tesseract.TesseractEngine _ocr;
        static NameValueCollection appSettings = ConfigurationManager.AppSettings;
        static XmlDocument xml = new XmlDocument();
        static Image<Gray, byte> header, footer;

        public Form1()
        {
            InitializeComponent();
            _ocr = new Tesseract.TesseractEngine(@"", @"eng", EngineMode.TesseractOnly);
            string templateFile = appSettings["RECEIPT.TEMPLATE"];
            xml.Load(templateFile);
            XmlNode doc = xml.DocumentElement;

            XmlNode node = doc.SelectSingleNode("shop/header");
            if (node != null && node.InnerText != null)
            {
                string headerFile = appSettings[node.InnerText];
                header = new Image<Gray, byte>(headerFile);
            }
            node = doc.SelectSingleNode("shop/footer");
            if (node != null && node.InnerText != null)
            {
                string headerFile = appSettings[node.InnerText];
                footer = new Image<Gray, byte>(headerFile);
            }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
          {
              Bgr drawColor = new Bgr(Color.Blue);
              Bgr redColor = new Bgr(Color.Red);
              try
              {
                  Image<Bgr, Byte> image = new Image<Bgr, byte>(openFileDialog.FileName);

                  using (Image<Hsv, byte> hsv = image.Convert<Hsv, Byte>())
                  {
                      int[] histogram = new int[image.Height];
                      int[] movingAve = new int[image.Height];

                      Image<Gray, byte>[] gray = hsv.Split();
                      gray[2]._GammaCorrect(2.0);
                      //_ocr.Recognize(gray[2].Erode(1));
                      Tesseract.Page page =_ocr.Process(gray[2].Erode(1).Bitmap);


                      ocrTextBox.Text = Path.GetFileName(openFileDialog.FileName) + "\r\n";
                      int height = 0, count = 0;
                     
                      using (var iter = page.GetIterator())
                      {
                          Rect bounds;
                          do {
                              do {
                                  do {
                                      do {
                                          if (iter.TryGetBoundingBox(Tesseract.PageIteratorLevel.Symbol, out bounds))
                                          {
                                              height += bounds.Height;
                                              Rectangle rect = new Rectangle(bounds.X1, bounds.Y1, bounds.Width, bounds.Height);
                                              image.Draw(rect, drawColor, 1);
                                              for (int y = bounds.Y1; y < bounds.Y2; y++)
                                              {
                                                  histogram[y]++;
                                              }
                                              count++;
                                          }
                                      } while (iter.Next(Tesseract.PageIteratorLevel.Symbol));

                                  } while (iter.Next(Tesseract.PageIteratorLevel.Word));

                              } while (iter.Next(Tesseract.PageIteratorLevel.TextLine, Tesseract.PageIteratorLevel.Word));

                              //string text = iter.GetText(Tesseract.PageIteratorLevel.TextLine);
                              //ocrTextBox.Text += text + "\r\n";
                          } while (iter.Next(Tesseract.PageIteratorLevel.Para, Tesseract.PageIteratorLevel.TextLine));
                      }
                      height /= count;

                      using (var iter = page.GetIterator())
                      {
                          do
                          {
                              string text = iter.GetText(Tesseract.PageIteratorLevel.TextLine);
                              ocrTextBox.Text += text + "\r\n";
                          } while (iter.Next(Tesseract.PageIteratorLevel.TextLine));
                      }

                      /*
                      Tesseract.Character[] characters = _ocr.GetCharacters();

                      foreach (Tesseract.Character c in characters)
                      {
                          height += c.Region.Height;
                          image.Draw(c.Region, drawColor, 1);
                          for (int y = c.Region.Top; y < c.Region.Bottom; y++)
                          {
                              histogram[y]++;
                          }
                      }
                      System.Diagnostics.Debug.WriteLine(height / characters.Length);
                      height /= characters.Length;
                       */

                      
                      // Draw histogram
                      LineSegment2D line = new LineSegment2D(new Point(histogram[0], 0), new Point(histogram[1], 1));
                      for (int y = 1; y < image.Height - 1; y++)
                      {
                          image.Draw(line, drawColor, 1);
                          line.P1 = line.P2;
                          line.P2 = new Point(histogram[y + 1], y + 1);
                      }
                      
                      /*
                      // Draw Moving Average
                      line.P1 = new Point(0, 5);
                      for (int y = 5; y < image.Height - 5; y++)
                      {
                          int sum = histogram[y - 5] + histogram[y - 4] + histogram[y - 3] + histogram[y -2] + histogram[y - 1];
                          sum += histogram[y] + histogram[y + 1] + histogram[y + 2] + histogram[y + 3] + histogram[y + 4];
                          line.P2 = new Point(sum, y);
                          image.Draw(line, drawColor, 2);
                          line.P1 = line.P2;
                      }
                      */

                      bool zero = false;
                      int zeroBegin = 0;
                      line.P1 = new Point(0, height / 2);
                      for (int y = height / 2; y < image.Height - height / 2; y++ )
                      {
                          int sum = histogram[y - height / 2] + histogram[y - height / 4] + histogram[y] + histogram[y + height / 4] + histogram[y + height / 2];
                          line.P2 = new Point(sum, y);
                          image.Draw(line, redColor, 1);
                          line.P1 = line.P2;
                          if (line.P2.X <= 3)
                          {
                              if (!zero)
                              {
                                  zero = true;
                                  zeroBegin = y;
                              }
                          }
                          else if (zero)
                          {
                              zero = false;
                              LineSegment2D separator = new LineSegment2D(new Point(0, (zeroBegin + y) / 2), new Point(image.Width, (zeroBegin + y) / 2));
                              image.Draw(separator, redColor, 3);
                          }
                      }
                          
                      imageBox1.Image = image;

                      /*
                      String text = _ocr.GetText();
                      string[] lines = text.Split('\n');
                      ocrTextBox.Text = Path.GetFileName(openFileDialog.FileName) + "\r\n";
                      for (int i = 9; i < lines.Length; i++ )
                      {
                          ocrTextBox.Text += lines[i] + "\n";
                      }
                       */
                      //ocrTextBox.Text = text;
                      image.Save("result.png");
                  }
              }
              catch (Exception exception)
              {
                  MessageBox.Show(exception.Message);
              }
          }

        }

        /// <summary>
        /// http://stackoverflow.com/questions/8051166/emgu-cv-how-i-can-get-all-occurrence-of-pattern-in-image
        /// </summary>
        /// <param name="modelImage"></param>
        /// <param name="observedImage"></param>
        static private void Matching(Image<Gray, Byte> modelImage, Image<Gray, Byte> observedImage)
        {
            ORBDetector detector = new ORBDetector();
            VectorOfKeyPoint modelKeyPoints = new VectorOfKeyPoint();
            VectorOfKeyPoint observedKeyPoints = new VectorOfKeyPoint();
            Mat modelDescriptors = new Mat();
            Mat observedDescriptors = new Mat();
            VectorOfVectorOfDMatch indices = new VectorOfVectorOfDMatch();
            Matrix<byte> mask;

            int k = 2;
            double uniquenessThreshold = 0.8;

            // Detect Features
            detector.DetectRaw(modelImage, modelKeyPoints);
            detector.Compute(modelImage, modelKeyPoints, modelDescriptors);

            detector.DetectRaw(observedImage, observedKeyPoints);
            detector.Compute(observedImage, observedKeyPoints, observedDescriptors);

            BFMatcher matcher = new BFMatcher(DistanceType.L2);
            matcher.Add(modelDescriptors);

            //indices = new Matrix<int>(observedDescriptors.Rows, k);
            using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
            {
                matcher.KnnMatch(observedDescriptors, indices, k, null);
                mask = new Matrix<byte>(dist.Rows, 1);
                mask.SetValue(255);
                //Features2DToolbox.VoteForUniqueness(indices, uniquenessThreshold, mask);
            }

            /*
            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            if (nonZeroCount >= 4)
            {
                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                if (nonZeroCount >= 4)
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);
            }

            #region draw the projected region on the image
            if (homography != null)
            {  //draw a rectangle along the projected model
                Rectangle rect = modelImage.ROI;
                PointF[] pts = new PointF[] { 
               new PointF(rect.Left, rect.Bottom),
               new PointF(rect.Right, rect.Bottom),
               new PointF(rect.Right, rect.Top),
               new PointF(rect.Left, rect.Top)};
                homography.ProjectPoints(pts);

                //result.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Bgr(Color.Red), 5);
            }
            #endregion
             */
        }

        /*
        /// <summary>
        /// http://www.emgu.com/wiki/index.php/FAST_feature_detector_in_CSharp
        /// </summary>
        /// <param name="modelImage"></param>
        /// <param name="observedImage"></param>
        /// <returns></returns>
        public static Image<Bgr, Byte> Draw(Image<Gray, Byte> modelImage, Image<Gray, byte> observedImage)
        {
            //HomographyMatrix homography = null;

            FastDetector fastCPU = new FastDetector(10, true);
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            VectorOfVectorOfDMatch indices = new VectorOfVectorOfDMatch();
            //Matrix<int> indices;

            //BriefDescriptorExtractor descriptor = new BriefDescriptorExtractor();

            Matrix<byte> mask;
            int k = 2;
            double uniquenessThreshold = 0.8;

            //extract features from the object image
            fastCPU.DetectRaw(modelImage, modelKeyPoints, null);
            //Matrix<Byte> modelDescriptors = descriptor.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

            // extract features from the observed image
            fastCPU.DetectRaw(observedImage, observedKeyPoints, null);
            //Matrix<Byte> observedDescriptors = descriptor.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
            BFMatcher matcher = new BFMatcher(DistanceType.L2);
            matcher.Add(modelDescriptors);

            //indices = new Matrix<int>(observedDescriptors.Rows, k);
            using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
            {
                matcher.KnnMatch(observedDescriptors, indices, k, null);
                mask = new Matrix<byte>(dist.Rows, 1);
                mask.SetValue(255);
                //Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
            }

            //Draw the matched keypoints
            Image<Bgr, Byte> result = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
               indices, new Bgr(255, 255, 255), new Bgr(255, 255, 255), mask, Features2DToolbox.KeypointDrawType.DEFAULT);

            
            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            if (nonZeroCount >= 4)
            {
                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                if (nonZeroCount >= 4)
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(
                    modelKeyPoints, observedKeyPoints, indices, mask, 2);
            }
            

            #region draw the projected region on the image
            if (homography != null)
            {  //draw a rectangle along the projected model
                Rectangle rect = modelImage.ROI;
                PointF[] pts = new PointF[] { 
         new PointF(rect.Left, rect.Bottom),
         new PointF(rect.Right, rect.Bottom),
         new PointF(rect.Right, rect.Top),
         new PointF(rect.Left, rect.Top)};
                homography.ProjectPoints(pts);

                result.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Bgr(Color.Red), 5);
            }
            #endregion
            

            return result;
        }
         * */

        /*
        private bool Header(out Rectangle rect)
        {
            //HomographyMatrix matrix;
            return false;
        }
         */
    }
}
