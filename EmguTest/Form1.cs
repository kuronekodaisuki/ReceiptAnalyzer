using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.OCR;
using Emgu.Util;

namespace EmguTest
{
    public partial class Form1 : Form
    {
        private Tesseract _ocr;
        static NameValueCollection appSettings = ConfigurationManager.AppSettings;
        static XmlDocument xml = new XmlDocument();
        static Image<Gray, byte> header, footer;

        public Form1()
        {
            InitializeComponent();
            _ocr = new Tesseract("", "eng", OcrEngineMode.TesseractOnly);
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
                      _ocr.Recognize(gray[2].Erode(1));

                      // 
                      Tesseract.Character[] characters = _ocr.GetCharacters();
                      int height = 0;
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

                      String text = _ocr.GetText();
                      ocrTextBox.Text = text;
                      image.Save("result.png");
                  }
              }
              catch (Exception exception)
              {
                  MessageBox.Show(exception.Message);
              }
          }
        }
    }
}
