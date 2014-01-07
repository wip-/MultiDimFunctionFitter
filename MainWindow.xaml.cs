using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using MathNet.Numerics.LinearAlgebra.Double;

namespace MultiDimFunctionFitter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapInfo bitmapInfoSource;
        BitmapInfo bitmapInfoFiltered;
        BitmapInfo bitmapInfoFitted;    // TODO rebuild
        double[] ri;    // coefficients
        double[] gi;    // coefficients
        double[] bi;    // coefficients

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SliderZoomOut_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderZoomOut.Value != 1)
                Zoom(SliderZoomOut.Value);
            if (SliderZoomIn != null)
                SliderZoomIn.Value = 1;
        }

        private void SliderZoomIn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderZoomIn.Value != 1)
                Zoom(SliderZoomIn.Value);
            if (SliderZoomOut != null)
                SliderZoomOut.Value = 1;
        }

        private void ButtonResetZoom_Click(object sender, RoutedEventArgs e)
        {
            Zoom(1);
            SliderZoomIn.Value = 1;
            SliderZoomOut.Value = 1;
        }

        private void MyCatch(System.Exception ex)
        {
            var st = new StackTrace(ex, true);      // stack trace for the exception with source file information
            var frame = st.GetFrame(0);             // top stack frame
            String sourceMsg = String.Format("{0}({1})", frame.GetFileName(), frame.GetFileLineNumber());
            Console.WriteLine(sourceMsg);
            MessageBox.Show(ex.Message + Environment.NewLine + sourceMsg);
            Debugger.Break();
        }

        private void Zoom(double val)
        {
            try
            {
                var myScaleTransform = new System.Windows.Media.ScaleTransform();
                myScaleTransform.ScaleY = val;
                myScaleTransform.ScaleX = val;
                if (LabelZoom != null)
                    LabelZoom.Content = val;
                var myTransformGroup = new System.Windows.Media.TransformGroup();
                myTransformGroup.Children.Add(myScaleTransform);

                System.Windows.Controls.Image[] images =
                    new System.Windows.Controls.Image[] { ImageSource, ImageFiltered };

                foreach (System.Windows.Controls.Image image in images)
                {
                    if (image == null || image.Source == null)
                        continue;
                    //image.RenderTransform = myTransformGroup;
                    image.LayoutTransform = myTransformGroup;
                }
            }
            catch (System.Exception ex)
            {
                MyCatch(ex);
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double vert = ((ScrollViewer)sender).VerticalOffset;
            double hori = ((ScrollViewer)sender).HorizontalOffset;

            ScrollViewer[] scrollViewers = new ScrollViewer[] { ScrollViewerSource, ScrollViewerFiltered };

            foreach (ScrollViewer scrollViewer in scrollViewers)
            {
                scrollViewer.ScrollToVerticalOffset(vert);
                scrollViewer.ScrollToHorizontalOffset(hori);
                scrollViewer.UpdateLayout();
            }
        }

        private void ImageSource_MouseMove(object sender, MouseEventArgs e)
        {
            Image_MouseMove(ImageSource, e);
        }

        private void ImageFiltered_MouseMove(object sender, MouseEventArgs e)
        {
            Image_MouseMove(ImageFiltered, e);
        }

        private void Image_MouseMove(System.Windows.Controls.Image clickedImage, MouseEventArgs e)
        {
            int x = (int)(e.GetPosition(clickedImage).X);
            int y = (int)(e.GetPosition(clickedImage).Y);

            BitmapInfo[] bitmapInfos =
                new BitmapInfo[] { bitmapInfoSource, bitmapInfoFiltered };

            System.Windows.Controls.Label[] labels =
                new System.Windows.Controls.Label[] { LabelColorSource, LabelColorFiltered };

            LabelInfo.Content = String.Format("X={0:D4}, Y={1:D4}", x, y);

            for (int i = 0; i < 2; ++i)
            {
                if (bitmapInfos[i] == null) continue;

                System.Drawing.Color color = bitmapInfos[i].GetPixelColor(x, y);
                float hue = color.GetHue();
                labels[i].Content = String.Format("A={0:D3}, R={1:D3}, G={2:D3}, B={3:D3}, H={4:###.##}",
                    color.A, color.R, color.G, color.B, hue);
            }


            // TODO show in LabelColorFitted the value of interpolated surface
            if (ri!=null)
            {
                Color colorIn = bitmapInfoSource.GetPixelColor(x, y);
                Color colorFitted = GetFittedColor(colorIn);
                float hue = colorFitted.GetHue();
                LabelColorFitted.Content = String.Format("A={0:D3}, R={1:D3}, G={2:D3}, B={3:D3}, H={4:###.##}",
                    colorFitted.A, colorFitted.R, colorFitted.G, colorFitted.B, hue);
            }
        }


        private Color GetFittedColor(Color colorIn)
        {
            double r1 = colorIn.RNormalized();
            double g1 = colorIn.GNormalized();
            double b1 = colorIn.BNormalized();
            //double d = : TODO density

            double r2 = r1 * r1;
            double g2 = g1 * g1;
            double b2 = b1 * b1;
            //double d2 = d * d;

            double r3 = r1 * r2;
            double g3 = g1 * g2;
            double b3 = b1 * b2;
            //double d3 = d * d2;


            double r = ri[0] * r3 + ri[1] * r2 + ri[2] * r1 + ri[3] * g3 + ri[4] * g2 + ri[5] * g1 + ri[6] * b3 + ri[7] * b2 + ri[8] * b1 + ri[9] * 1;
            double g = gi[0] * r3 + gi[1] * r2 + gi[2] * r1 + gi[3] * g3 + gi[4] * g2 + gi[5] * g1 + gi[6] * b3 + gi[7] * b2 + gi[8] * b1 + gi[9] * 1;
            double b = bi[0] * r3 + bi[1] * r2 + bi[2] * r1 + bi[3] * g3 + bi[4] * g2 + bi[5] * g1 + bi[6] * b3 + bi[7] * b2 + bi[8] * b1 + bi[9] * 1;

            return Color.FromArgb(255, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }


        private void ImageSource_Drop(object sender, DragEventArgs e)
        {
            String msg = LoadImage(ImageSource, out bitmapInfoSource, e);

            if (msg != null)
                LabelInfo.Content = msg;
            else
            {
                UpdateCoefficients();
            }
        }

        private void ImageFiltered_Drop(object sender, DragEventArgs e)
        {
            String msg = LoadImage(ImageFiltered, out bitmapInfoFiltered, e);

            if (msg != null)
                LabelInfo.Content = msg;
            else
            {
                UpdateCoefficients();
            }
        }

        private static Bitmap LoadBitmap(String filename, out String errorMessage)
        {
            FileStream fs = null;
            try
            {
                fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                if (fs != null)
                    fs.Close();
                errorMessage = "File already in use!";
                return null;
            }

            Bitmap bitmap;
            //try
            {
                bitmap = new Bitmap(fs);
                errorMessage = null;
            }
            //catch (System.Exception /*ex*/)
            //{
            //    bitmap.Dispose();
            //    errorMessage = "Not an image!";
            //}
            return bitmap;
        }

        private String LoadImage(
            System.Windows.Controls.Image destinationImage,
            out BitmapInfo destinationBitmapInfo,
            DragEventArgs e)
        {
            destinationBitmapInfo = null;

            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                    return "Not a file!";

                String[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 1)
                    return "Too many files!";

                String imageSourceFileName = files[0];

                if (!File.Exists(imageSourceFileName))
                    return "Not a file!";

                String errorMessage;
                Bitmap destinationBitmap = LoadBitmap(imageSourceFileName, out errorMessage);
                if (errorMessage != null)
                    return errorMessage;

                destinationImage.Source =
                    Imaging.CreateBitmapSourceFromHBitmap(
                        destinationBitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, 
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                destinationBitmapInfo = new BitmapInfo(destinationBitmap);

                return null;
            }
            catch (System.Exception ex)
            {
                MyCatch(ex);
                return "Exception";
            }
        }

        private void UpdateCoefficients()
        {
            if (bitmapInfoSource == null || bitmapInfoFiltered == null)
                return;

            int width = bitmapInfoSource.Width;
            int height = bitmapInfoSource.Height;
            int pixelsCount = width * height;

            double[] Rinputs3 = new double[pixelsCount];
            double[] Ginputs3 = new double[pixelsCount];
            double[] Binputs3 = new double[pixelsCount];
            double[] Dinputs3 = new double[pixelsCount]; // we will need density too
            
            double[] Rinputs2 = new double[pixelsCount];
            double[] Ginputs2 = new double[pixelsCount];
            double[] Binputs2 = new double[pixelsCount];
            double[] Dinputs2 = new double[pixelsCount]; // we will need density too

            double[] Rinputs1 = new double[pixelsCount];
            double[] Ginputs1 = new double[pixelsCount];
            double[] Binputs1 = new double[pixelsCount];
            double[] Dinputs1 = new double[pixelsCount]; // we will need density too

            double[] Cinputs0 = new double[pixelsCount];

            double[] Routputs = new double[pixelsCount];
            double[] Goutputs = new double[pixelsCount];
            double[] Boutputs = new double[pixelsCount];

            int counter = 0;

            for (int x = 0; x < width; ++x )
            for (int y = 0; y < height; ++y )
            {
                System.Drawing.Color colorIn = bitmapInfoSource.GetPixelColor(x, y);
                System.Drawing.Color colorOut = bitmapInfoFiltered.GetPixelColor(x, y);

                double r = colorIn.RNormalized();
                double g = colorIn.GNormalized();
                double b = colorIn.BNormalized();
                //double d = sample id

                double r2 = r*r;
                double g2 = g*g;
                double b2 = b*b;
                //double d2 = d*d;

                Rinputs3[counter] = r*r2;
                Ginputs3[counter] = g*g2;
                Binputs3[counter] = b*b2;
                //Dinputs3[counter] = ; // TODO

                Rinputs2[counter] = r2;
                Ginputs2[counter] = g2;
                Binputs2[counter] = b2;
                //Dinputs2[counter] = ; // TODO

                Rinputs1[counter] = r;
                Ginputs1[counter] = g;
                Binputs1[counter] = b;
                //Dinputs1[counter] = ; // TODO

                Cinputs0[counter] = 1;

                Routputs[counter] = colorOut.RNormalized();
                Goutputs[counter] = colorOut.GNormalized();
                Boutputs[counter] = colorOut.BNormalized();

                ++counter;
            }


            List<double[]> columns = new List<double[]>
            {
                Rinputs3,
                Ginputs3,
                Binputs3,
                //Dinputs3,
                            
                Rinputs2,
                Ginputs2,
                Binputs2,
                //Dinputs2,
                            
                Rinputs1,
                Ginputs1,
                Binputs1,
                //Dinputs1,

                Cinputs0
            };

            // http://christoph.ruegg.name/blog/linear-regression-mathnet-numerics.html
            var X = DenseMatrix.OfColumns(pixelsCount, 10, columns);           
            var Yred = new DenseVector(Routputs);
            var Ygreen = new DenseVector(Goutputs);
            var Yblue = new DenseVector(Boutputs);

            try
            {
                var QR = X.QR();

                ri = QR.Solve(Yred).ToArray();
                gi = QR.Solve(Ygreen).ToArray();
                bi = QR.Solve(Yblue).ToArray();

                // TODO use methods suggested in http://stackoverflow.com/a/20900765/758666
            }
            catch (System.Exception ex)
            {
                MyCatch(ex);	
            }

            // now that we have the coefficients, we try to regenerate the filtered image

            bitmapInfoFitted = new BitmapInfo(width, height, bitmapInfoSource.PixelFormat);
            for (int x = 0; x < width; ++x )
            for (int y = 0; y < height; ++y)
            {
                System.Drawing.Color colorIn = bitmapInfoSource.GetPixelColor(x, y);
                System.Drawing.Color colorOut = GetFittedColor(colorIn);
                bitmapInfoFitted.SetPixelColor(x, y, colorOut);
            }


            ImageFitted.Source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    bitmapInfoFitted.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, 
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
    }
}
