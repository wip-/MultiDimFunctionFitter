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
        BitmapInfo bitmapInfoFitted;

        double[] ri;    // coefficients
        double[] gi;    // coefficients
        double[] bi;    // coefficients

        private int[] orders = new int[] { 0,1,2,3,4,5,6,7,8,9 };
        public int[] Orders{get { return orders; }}

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
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

        private void ImageFitted_MouseMove(object sender, MouseEventArgs e)
        {
            Image_MouseMove(ImageFitted, e);
        }

        private void Image_MouseMove(System.Windows.Controls.Image clickedImage, MouseEventArgs e)
        {
            int x = (int)(e.GetPosition(clickedImage).X);
            int y = (int)(e.GetPosition(clickedImage).Y);

            BitmapInfo[] bitmapInfos =
                new BitmapInfo[] { bitmapInfoSource, bitmapInfoFiltered, bitmapInfoFitted };

            System.Windows.Controls.Label[] labels =
                new System.Windows.Controls.Label[] { LabelColorSource, LabelColorFiltered, LabelColorFitted };

            LabelInfo.Content = String.Format("X={0:D4}, Y={1:D4}", x, y);

            for (int i = 0; i < 3; ++i)
            {
                if (bitmapInfos[i] == null) continue;

                System.Drawing.Color color = bitmapInfos[i].GetPixelColor(x, y);
                float hue = color.GetHue();
                labels[i].Content = String.Format("A={0:D3}, R={1:D3}, G={2:D3}, B={3:D3}, H={4:###.##}",
                    color.A, color.R, color.G, color.B, hue);
            }


            // TODO show in LabelColorFitted the value of interpolated surface
            if (ri != null)
            {
                Color colorIn = bitmapInfoSource.GetPixelColor(x, y);
                Color colorFitted = GetFittedColor(colorIn);
                float hue = colorFitted.GetHue();
                LabelColorFittedFormula.Content = String.Format("A={0:D3}, R={1:D3}, G={2:D3}, B={3:D3}, H={4:###.##}",
                    colorFitted.A, colorFitted.R, colorFitted.G, colorFitted.B, hue);
            }
        }


        private Color GetFittedColor(Color colorIn)
        {
            double r = colorIn.RNormalized();
            double g = colorIn.GNormalized();
            double b = colorIn.BNormalized();
            double d = 1; // TODO get density from slider

            int polynomialOrder = Convert.ToInt32(ComboBoxPolynomialOrder.SelectedValue);

            double rOut = ri[0];
            double gOut = gi[0];
            double bOut = bi[0];

            for (int i = 0; i < polynomialOrder; ++i)
            {
                double rPow = Math.Pow(r, i+1);
                double gPow = Math.Pow(g, i+1);
                double bPow = Math.Pow(b, i+1);

                rOut += ri[3 * i + 1] * rPow + ri[3 * i + 2] * gPow + ri[3 * i + 3] * bPow;
                gOut += gi[3 * i + 1] * rPow + gi[3 * i + 2] * gPow + gi[3 * i + 3] * bPow;
                bOut += bi[3 * i + 1] * rPow + bi[3 * i + 2] * gPow + bi[3 * i + 3] * bPow;
            }

            return Color.FromArgb(255, (byte)(rOut * 255), (byte)(gOut * 255), (byte)(bOut * 255));
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
            int polynomialOrder = Convert.ToInt32(ComboBoxPolynomialOrder.SelectedValue);

            List<double[]> RpowersInputs = new List<double[]>();
            List<double[]> GpowersInputs = new List<double[]>();
            List<double[]> BpowersInputs = new List<double[]>();
            List<double[]> DpowersInputs = new List<double[]>();
            for (int i = 0; i < polynomialOrder; ++i )
            {
                RpowersInputs.Add(new double[pixelsCount]);
                GpowersInputs.Add(new double[pixelsCount]);
                BpowersInputs.Add(new double[pixelsCount]);
                DpowersInputs.Add(new double[pixelsCount]);
            }


            double[] ConstantInputs = new double[pixelsCount];

            double[] Routputs = new double[pixelsCount];
            double[] Goutputs = new double[pixelsCount];
            double[] Boutputs = new double[pixelsCount];

            int counter = 0;

            for (int x = 0; x < width; ++x )
            for (int y = 0; y < height; ++y )
            {
                ConstantInputs[counter] = 1;

                System.Drawing.Color colorIn = bitmapInfoSource.GetPixelColor(x, y);
                double r = colorIn.RNormalized();
                double g = colorIn.GNormalized();
                double b = colorIn.BNormalized();
                double d = 1.0; // TODO load from image
                for (int i = 0; i < polynomialOrder; ++i)
                {
                    RpowersInputs[i][counter] = Math.Pow(r, i + 1);
                    GpowersInputs[i][counter] = Math.Pow(g, i + 1);
                    BpowersInputs[i][counter] = Math.Pow(b, i + 1);
                    DpowersInputs[i][counter] = Math.Pow(d, i + 1);
                }
              

                System.Drawing.Color colorOut = bitmapInfoFiltered.GetPixelColor(x, y);
                Routputs[counter] = colorOut.RNormalized();
                Goutputs[counter] = colorOut.GNormalized();
                Boutputs[counter] = colorOut.BNormalized();

                ++counter;
            }

            List<double[]> columns = new List<double[]>();
            columns.Add(ConstantInputs);
            for (int i = 0; i < polynomialOrder; ++i)
            {
                columns.Add(RpowersInputs[i]);
                columns.Add(GpowersInputs[i]);
                columns.Add(BpowersInputs[i]);
                //columns.Add(DpowersInputs[i]);
            }
            

            // http://christoph.ruegg.name/blog/linear-regression-mathnet-numerics.html
            var X = DenseMatrix.OfColumns(pixelsCount, columns.Count, columns);           
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

        private void ComboBoxPolynomialOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCoefficients();
        }
    }
}
