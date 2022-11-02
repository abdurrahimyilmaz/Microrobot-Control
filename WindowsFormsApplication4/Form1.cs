

using System;
using System.Drawing;
using System.Windows.Forms;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging.Filters;
using AForge.Imaging;
using System.Collections.Generic;
using AForge.Math.Geometry;
using System.Linq;
using AForge.Vision.Motion;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Text;
using AForge.Controls;
using System.Drawing.Imaging;

namespace WindowsFormsApplication4
{
    public partial class Form1 : Form
    {     

        private string video;
        FileVideoSource videoSource;
        FileVideoSource videoSource2;

        double ilk = 0;
        double son = 0;
        double hız = 0;
        float konumx = 0;
        float konumy = 0;
        float fark = 0;
        float soluzakx;
        float soluzaky;
        float saguzakx;
        float saguzaky;
        double aci;
        double aci2;        
        float ensagy = 0;
        float altsagy = 0;

        #region kullanılmayanlar
        double ilkaci;
        double sonaci;
        double doksanmi;
        double saniyeilk;
        double saniyeson;
        #endregion

        MotionDetector tespitci;     
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {         
            Control.CheckForIllegalCrossThreadCalls = false;
            tespitci = new MotionDetector(new TwoFramesDifferenceDetector(), new MotionAreaHighlighting());            
        }           
        
        private void hareket(Bitmap image)
        {
            #region tanımlar
            float ensolx = 2000;
            float ensagx = 0;
            float ensoly = 0;
            float cogx;
            float cogy;
            float altsolx = 0;
            float altsagx = 0;
            DateTime zaman = DateTime.Now;
            int sayac = (zaman.Millisecond / 500);
            int red = trackBar1.Value;
            int green = trackBar4.Value;
            int blue = trackBar5.Value;
            int uzunluk1 = trackBar2.Value;
            int uzunluk2 = trackBar3.Value;
            int uzunluk3 = trackBar6.Value;
            
            float altsoly = 0;
            float toplam1 = 1200;

            EuclideanColorFiltering filtre1 = new EuclideanColorFiltering();
            Grayscale filter2 = new Grayscale(0.2125, 0.7154, 0.0721);
            //OtsuThreshold filter3 = new OtsuThreshold();        
            Threshold filter33 = new Threshold(uzunluk2);
            HorizontalRunLengthSmoothing filter4 = new HorizontalRunLengthSmoothing(uzunluk1);
            VerticalRunLengthSmoothing filter5 = new VerticalRunLengthSmoothing(uzunluk3);            
            
            #endregion
            
            var sure = System.Diagnostics.Stopwatch.StartNew();

            if(checkBox1.Checked == true)
                tespitci.ProcessFrame(image);            
                     
          //  Bitmap newImage = filter.Apply(image);              //Kırpma filtresi      
            filtre1.CenterColor = new RGB((byte)red, (byte)green, (byte)blue);
            filtre1.ApplyInPlace(image);                     //Öklit filtresi
            Bitmap image1 = filter2.Apply(image);            //Gri filtre        
            filter4.ApplyInPlace(image1);                       //Horizontal RLS
            filter5.ApplyInPlace(image1);                       //Vertical RLS           
            filter33.ApplyInPlace(image1);
            Bitmap image2 = new Bitmap(image1);
            
            Graphics g = Graphics.FromImage(image2);

            #region tanımlamalar
            Font font = new Font("Arial", 25);
            Pen pen = new Pen(Color.Blue, 5);
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinWidth = 30;
            blobCounter.MinHeight = 30;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
            SolidBrush brush = new SolidBrush(Color.White);
            SolidBrush brush2 = new SolidBrush(Color.Red);
            SolidBrush brush3 = new SolidBrush(Color.LightGreen);
            SolidBrush brush4 = new SolidBrush(Color.Turquoise);
            blobCounter.ProcessImage(image1);
            Rectangle[] rects = blobCounter.GetObjectsRectangles();

            if (rects.Length > 0)
            {
                Blob[] blobs = blobCounter.GetObjectsInformation();
                konumx = blobs[0].CenterOfGravity.X;
                konumy = blobs[0].CenterOfGravity.Y;
                GrahamConvexHull hullFinder = new GrahamConvexHull();

                List<IntPoint> leftPoints, rightPoints, edgePoints;
                edgePoints = new List<IntPoint>();          //+++++önemli netteki örneklerde bu kısım yok ondan çalışmıyor

                blobCounter.GetBlobsLeftAndRightEdges(blobs[0], out leftPoints, out rightPoints);

                edgePoints.AddRange(leftPoints);
                edgePoints.AddRange(rightPoints);


                List<IntPoint> hull = hullFinder.FindHull(edgePoints);
                int kose = hull.Count;
                IntPoint sol = PointsCloud.GetFurthestPoint(hull, new IntPoint(0, 0));
                IntPoint sag = PointsCloud.GetFurthestPoint(hull, new IntPoint(image.Width, 0));

                soluzakx = sol.X;
                soluzaky = sol.Y;
                saguzakx = sag.X;
                saguzaky = sag.Y;
                cogx = blobs[0].CenterOfGravity.X;
                cogy = blobs[0].CenterOfGravity.Y;
                #endregion

                #region döngüler
                Parallel.For(0, kose, i =>                             //(int i = 0; i < hull.Count; i++)
                {
                    int a = hull[i].X;
                    if (a < ensolx)
                    {
                        ensolx = a;
                        ensoly = hull[i].Y;
                    }
                });

                Parallel.For(0, kose, i =>          //(int i = 0; i < hull.Count; i++)
                {
                    int b = hull[i].X;
                    if (b > ensagx)
                    {
                        ensagx = b;
                        ensagy = hull[i].Y;
                    }
                });

                Parallel.For(0, kose - 1, i =>                         //(int i = 0; i < hull.Count - 1; i++)
                {
                    int y = hull[i].Y;
                    int x = Convert.ToInt32(hull[i].X - ensolx);
                    float toplam2 = x + y;

                    if (toplam2 < toplam1 + 30 && y > konumy + 20)
                    {
                        toplam1 = toplam2;
                        altsolx = hull[i].X;
                        altsoly = hull[i].Y;

                    }

                });

                Parallel.For(0, kose - 1, i =>                      //(int i = 0; i < hull.Count - 1; i++)
                {
                    int y = hull[i].Y;
                    int x = Convert.ToInt32(ensagx - hull[i].X);
                    float toplam2 = x + y;

                    if (toplam2 < toplam1 + 30 && y > konumy + 20)
                    {
                        toplam1 = toplam2;
                        altsagx = hull[i].X;
                        altsagy = hull[i].Y;
                    }
                });
                #endregion

                if (aci > 7)
                    aci = 0;

                hiz(cogx);

        /*        if (hız > 0.12)
                  fark = (altsagy - altsoly) * (ensagx - altsagx) / (altsagx - altsolx);
                else
                  fark = 0;


                if (fark > 1)
                    fark = 0;   */

                
                aci2 = (Math.Atan2((altsagy - altsoly + fark), (ensagx - ensolx)) * 180 / Math.PI);
                aci = 90 + (Math.Atan2((saguzakx - soluzakx), (saguzaky - soluzaky)) * 180 / Math.PI);                         


             /*   if(hız<0.3)
                {
                    aci = 0;
                    aci2 = 0;
                }*/

                //g.FillRectangle(brush2, soluzakx-4, soluzaky - 4, 8, 8);
                //g.FillRectangle(brush2, saguzakx - 4, saguzaky - 4, 8, 8);
                g.FillRectangle(brush4, ensolx-4, altsoly-4, 8, 8);
                g.FillRectangle(brush4, ensagx-4, altsagy-4+fark, 8, 8);
                //   g.FillRectangle(brush2, ensagx - 4, altsagy - 4, 8, 8);
                g.DrawString(aci2.ToString("N3"), font, brush2, image2.Width - 130, image2.Height - 50);
             //   label7.Text = fark.ToString("N3");
            }    

                 if(checkBox2.Checked == true)
            {
                richTextBox1.Text += aci.ToString("N3") + "\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
            }
                 if (Math.Abs(aci2) < 6 && checkBox3.Checked == true)
                 {                     
                     richTextBox2.Text += aci2.ToString("N3") + "\n";
                     richTextBox2.SelectionStart = richTextBox2.Text.Length;
                     richTextBox2.ScrollToCaret();
                 }
            if(checkBox5.Checked == true)
                pictureBox2.Image = image2;

         //   g.Dispose();
            
            sure.Stop();
            var verisuresi = sure.ElapsedMilliseconds;

            if (checkBox4.Checked == true)
            {
                richTextBox3.Text += verisuresi.ToString() + " *** " + hız + "\n";
                richTextBox3.SelectionStart = richTextBox3.Text.Length;
                richTextBox3.ScrollToCaret();
            }
        }       
        

        #region kamera işlemleri

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dosya = new OpenFileDialog();
           
            if (dosya.ShowDialog() == DialogResult.OK)
            {
                video = dosya.FileName;
            } 
        }

        private void video_NewFrame1(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap klon = eventArgs.Frame;
            int kesmeXbasla = trackBar7.Value;
            int kesmeYbasla = trackBar8.Value;
            int kesmeXuzaklik = trackBar9.Value;
            int kesmeYuzaklik = trackBar10.Value;

            //   int genis = klon.Width;
            Crop filter = new Crop(new Rectangle(kesmeXbasla, kesmeYbasla, kesmeXuzaklik, kesmeYuzaklik));

               Bitmap newImage = filter.Apply(klon);
            //   Bitmap yeni2 = new Bitmap(klon);           

            hareket(newImage);
        }

        private void button1_Click(object sender, EventArgs e)
        {         
            videoSource.Stop();                        
        }

        private void button3_Click(object sender, EventArgs e)
        {           
            videoSource = new FileVideoSource(video);
         //   videoSource2 = new FileVideoSource(video);            
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame1);
            videoSource.Start();
        }
        #endregion

        #region diğer
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            Parallel.For(0, points.Count, i =>                 //(int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);

            });
            return array;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox2.Clear();
            richTextBox3.Clear();
            trackBar1.Value = 1500;
            trackBar4.Value = 1082;
            trackBar5.Value = 1055;
            
         
        }

        private void hiz(double konum)
        {
            DateTime zaman = DateTime.Now;
            int sayac = (zaman.Millisecond / 500);

            

            if (sayac == 0)
                ilk = konum;
            if (sayac == 1)
                son = konum;

            

            hız = (5 * (2 * Math.Abs(son - ilk))) / 1920;
            hız = Math.Round(hız, 3);
        }

        #endregion
        
    }
}



/*
 * Kodun özeti: Öklid resim filtreleme ile belli alanda ki resim çıakrılıyor
 * ardından başka alanlarda ki bloblar görünmmesi için kesliyor
 * ardından sırasıyla gray, normal thresh, yatay ve dikey run lentgh smoothing
 * uygulanarak blob görünür hale getiriliyor bundan sonra graham kabuk
 * algoritması ile çevresi çiziliyor ve sol alt ve sağ altta ki                                     OUT OF USE
 * noktaları ve arctan2 kullanark açı hesaplanıyor, ikinci yöntem olarak
 * getfurthestpoint metodu, üçüncü yöntem for döngüleri ile köşe noktası bulma
 * algoritması ile noktalar tespit edilerek açı hesaplanıyor
 * ağırlık merkezinde yararlanarakda hız bulunuyor.
 */

/*     private void hiz(float konum)
    {
        DateTime zaman = DateTime.Now;
        int sayac = (zaman.Millisecond / 500);

        label3.Text = sayac.ToString();

        if (sayac == 0)
            ilk = konum;
        if (sayac == 1)
            son = konum;

        label7.Text = Convert.ToInt32(ilk).ToString();
        label9.Text = Convert.ToInt32(son).ToString();

        hız = (5 * (2 * Math.Abs(son - ilk))) / 1920;

    } */

/*      public void kosetespit2(Bitmap image)
{
    float ensolx = 2000;
    float ensagx = 0;
    float ensoly = 0;
    float cogx;
    float altsolx = 0;
    float altsagx = 0;


    float altsoly = 0;
    float toplam1 = 1200;
    Graphics g = Graphics.FromImage(image);
    Pen pen = new Pen(Color.Blue, 5);
    BlobCounter blobCounter = new BlobCounter();
    blobCounter.FilterBlobs = true;
    blobCounter.MinWidth = 25;
    blobCounter.MinHeight = 25;
    blobCounter.ObjectsOrder = ObjectsOrder.Size;
    SolidBrush brush = new SolidBrush(Color.White);
    SolidBrush brush2 = new SolidBrush(Color.Red);
    SolidBrush brush3 = new SolidBrush(Color.LightGreen);
    SolidBrush brush4 = new SolidBrush(Color.Turquoise);
    blobCounter.ProcessImage(image);
    Rectangle[] rects = blobCounter.GetObjectsRectangles();



    if (rects.Length > 0)
    {
        Blob[] blobs = blobCounter.GetObjectsInformation();
        konumx = blobs[0].CenterOfGravity.X;
        konumy = blobs[0].CenterOfGravity.Y;
        GrahamConvexHull hullFinder = new GrahamConvexHull();

        List<IntPoint> leftPoints, rightPoints, edgePoints;
        edgePoints = new List<IntPoint>();          //+++++önemli netteki örneklerde bu kısım yok ondan çalışmıyor

        blobCounter.GetBlobsLeftAndRightEdges(blobs[0], out leftPoints, out rightPoints);

        edgePoints.AddRange(leftPoints);
        edgePoints.AddRange(rightPoints);


        List<IntPoint> hull = hullFinder.FindHull(edgePoints);
        int kose = hull.Count;
        IntPoint sol = PointsCloud.GetFurthestPoint(hull, new IntPoint(0, 0));
        IntPoint sag = PointsCloud.GetFurthestPoint(hull, new IntPoint(image.Width, 0));

        soluzakx = sol.X;
        soluzaky = sol.Y;
        saguzakx = sag.X;
        saguzaky = sag.Y;
        cogx = blobs[0].CenterOfGravity.X;

        Parallel.For(0, kose, i =>                             //(int i = 0; i < hull.Count; i++)
        {
            int a = hull[i].X;
            if (a < ensolx)
            {
                ensolx = a;
                ensoly = hull[i].Y;
            }
        });

        Parallel.For(0, kose, i =>          //(int i = 0; i < hull.Count; i++)
        {
            int b = hull[i].X;
            if (b > ensagx)
            {
                ensagx = b;
                ensagy = hull[i].Y;
            }
        });

        Parallel.For(0, kose - 1, i =>                         //(int i = 0; i < hull.Count - 1; i++)
        {
            int y = hull[i].Y;
            int x = Convert.ToInt32(hull[i].X - ensolx);
            float toplam2 = x + y;

            if (toplam2 < toplam1 + 30 && y > konumy + 20)
            {
                toplam1 = toplam2;
                altsolx = hull[i].X;
                altsoly = hull[i].Y;

            }

        });

        Parallel.For(0, kose - 1, i =>                      //(int i = 0; i < hull.Count - 1; i++)
        {
            int y = hull[i].Y;
            int x = Convert.ToInt32(ensagx - hull[i].X);
            float toplam2 = x + y;

            if (toplam2 < toplam1 + 30 && y > konumy + 20)
            {
                toplam1 = toplam2;
                altsagx = hull[i].X;
                altsagy = hull[i].Y;
            }
        });

        if (aci > 7)
            aci = 0;

        //         hiz(cogx);

        /*          if (hız > 0.12)
                      fark = (altsagy - altsoly) * (ensagx - altsagx) / (altsagx - altsolx);
                  else
                      fark = 0;

                  if (fark > 1)
                      fark = 0;*/

        //IntPoint solref = new IntPoint(altsolx, ensoly);
        //IntPoint sagref = new IntPoint(altsagx, ensagy);
        //aci2 = (Math.Atan2((ensagx - ensolx), (altsagy - altsoly + fark)) * 180 / Math.PI) - 90;
        //        aci = 90 + (Math.Atan2((saguzakx - soluzakx), (saguzaky - soluzaky)) * 180 / Math.PI);



        //    if (label3.Text == "0")
        //      ilkaci = aci;
        //   if (label3.Text == "1")
        //       sonaci = aci;

        //   doksanmi = Math.Abs(sonaci - ilkaci);

        /* if (doksanmi > 30)
         {
             aci = 90;

         }

        //aci = Convert.ToInt32(aci);
        //label11.Text = aci.ToString();
        //        g.DrawPolygon(pen, ToPointsArray(hull));

        //     for (int i = 0, n = hull.Count; i < n; i++)
        //     {
        //       g.FillRectangle(brush, hull[i].X, hull[i].Y, 3, 3);
        // }

        //         g.FillRectangle(brush2, soluzakx-4, soluzaky - 4, 8, 8);
        //         g.FillRectangle(brush2, saguzakx - 4, saguzaky - 4, 8, 8);
        //         g.FillRectangle(brush3, ensolx-4, altsoly-4, 8, 8);
        //         g.FillRectangle(brush4, ensagx-4, altsagy-4+fark, 8, 8);
        //g.DrawLine(pen, x1, y1, x2, y2);           
        //         label7.Text = fark.ToString("N5");
    }



    if (aci != 0 && Math.Abs(aci2) < 6 && hız > 0.2)
    {
        richTextBox1.Text += aci.ToString("N3") + "\n";
        richTextBox1.SelectionStart = richTextBox1.Text.Length;
        richTextBox1.ScrollToCaret();
        richTextBox2.Text += aci2.ToString("N3") + "\n";
        richTextBox2.SelectionStart = richTextBox2.Text.Length;
        richTextBox2.ScrollToCaret();

    }

    //    pictureBox2.Image = image;
}         */       