using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Dynamic;
using Microsoft.CSharp;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;
using System.IO;

using System.Diagnostics;
using Work;


namespace IPS
{
    public static class NodeLibrary
    {
        static Random rand = new Random();
        public static List<NodeControl> Nodes;
        public static void InitializeLibrary()
        {

            Nodes = new List<NodeControl>();
            var methodsWithAtt =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                from m in t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                let att = m.GetCustomAttributes<NFuncAttribute>()
                where att != null && att.Count() > 0
                select m;


            foreach (var m in methodsWithAtt)
                Nodes.Add(new NodeControl(m));


            var ass = Assembly.GetExecutingAssembly();
            var types = ass.DefinedTypes.ToArray();
            var d = ass.GetReferencedAssemblies();


            object p = null;
        }



        [NFunc(Name = "Variable", Category = "Basic")]
        static double Variable([InField]double GetVal)
        {
            return GetVal;
        }

        [NFunc(Name = "StrVariable", Category = "Basic")]
        static string StrVariable([InField]string val)
        {
            return val;
        }

        [NFunc(Name = "Show String", Category = "Basic")]
        static void ShowString(string inData, out string data)
        {
            data = "str: " + inData;
        }

        [NFunc(Name = "Show Object", Category = "Basic")]
        static void ShowObject(object inData, out string sdata, out BitmapImage bdata, out Array adata)
        {
            sdata = null;
            bdata = null;
            adata = null;
            if (inData.GetType().IsIConvertible())
                sdata = inData.ToString();
            if (inData.GetType() == typeof(IndexedValue))
                sdata = inData.ToString();
            if (inData.GetType() == typeof(Bitmap))
                bdata = B2BI(inData as Bitmap);
            if (inData.GetType().IsArray)
                adata = inData as Array;
        }

        [NFunc(Name = "Show Image", Category = "Basic")]
        static void ShowImage(Bitmap img, out BitmapImage mid)
        {
            mid = B2BI(img);
        }




        [NFunc(Name = "Add", Category = "Math")]
        static double Add(float a, float b)
        {
            return a + b;
        }

        [NFunc(Name = "Multiply", Category = "Math")]
        static double Multiply(double a, double b)
        {
            return a * b;
        }




        [NFunc(Name = "Create Image RGB", Category = "Image")]
        static Bitmap CreateImageFromRGB(byte r, byte g, byte b, [InField(PVal = 50)]int x, [InField(PVal = 50)]int y, out BitmapImage img)
        {
            img = new BitmapImage();
            if (x == 0 || y == 0) return null;


            BitmapImage bi = new BitmapImage();
            Bitmap bmp = new System.Drawing.Bitmap(x, y);
            for (int i = 0; i < bmp.Height; i++)
                for (int k = 0; k < bmp.Width; k++)
                    bmp.SetPixel(i, k, Color.FromArgb(r, g, b));

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
            }
            img = bi;
            return bmp;
        }

        [NFunc(Name = "Load Img From File", Category = "Image")]
        static Bitmap LoadImgFromFile([InField]Bitmap file, [InField(PVal = true)]bool ShowImage, [InField(PVal = 1.0f, Max = 1.0f, Min = 0.01f)]float ResizeScale, out string OutRes, out BitmapImage img)
        {
            img = null;
            OutRes = "";
            if (file == null) return null;
            var copy = new Bitmap(file);
            var res = new Image<Bgr, byte>(copy);
            res = res.Resize(ResizeScale, Inter.Linear);
            file = new Bitmap(res.Bitmap);

            if (ShowImage)
            {
                img = B2BI(file);
            }
            OutRes = "Output Resolution: " + res.Size;
            return file;
        }






        [NFunc(Name = "Gauss Blur", Category = "OpenCV/Misc")]
        static Bitmap Blur(Bitmap img, [InField(PVal = 9.0)]string cnt, out BitmapImage outImg)
        {

            if (cnt == "") cnt = "9";
            outImg = new BitmapImage();



            Image<Rgba, byte> m_ProcessImage = new Image<Rgba, byte>(new Bitmap(img));


            var count = Convert.ToInt32(cnt);
            for (int i = 1; i < count; i += 2)
                CvInvoke.GaussianBlur(m_ProcessImage, m_ProcessImage, new System.Drawing.Size(i, i), 0);

            outImg = B2BI(new Bitmap(m_ProcessImage.Bitmap));

            return new Bitmap(new Bitmap(m_ProcessImage.Bitmap));
        }

        [NFunc(Name = "Detect and Compute", Category = "OpenCV/Features", OutputTypes = new Type[] { typeof(VectorOfKeyPoint), typeof(UMat) })]
        static object[] DetectAndCompute2(UMat img, Feature2D detector, Feature2D computer) // находит и обрабатывает дескрипторы изображения
        {
            object[] outp = new object[0];
            UMat descriptors = new UMat();
            var mkp = new MKeyPoint[0];
            VectorOfKeyPoint keypoints;

            try
            {
                mkp = detector.Detect(img);
                keypoints = new VectorOfKeyPoint(mkp);
                computer.Compute(img, keypoints, descriptors);
                outp = new object[] { keypoints, descriptors };
            }
            finally
            {

            }
            return outp;
        }

        [NFunc(Name = "SURF", Category = "OpenCV/Detectors")]
        static Feature2D SurfComputer([InField(PVal = 300.0)]double hessianThresh, [InField(PVal = 4)]int nOctaves, [InField(PVal = 2)]int nOctaveLayers, [InField(PVal = true)]bool extended, [InField(PVal = false)]bool upright)
        {
            return (Feature2D)new SURF(hessianThresh, nOctaves, nOctaveLayers, extended, upright);
        }

        [NFunc(Name = "SIFT", Category = "OpenCV/Detectors")]
        static Feature2D SiftComputer([InField(PVal = 0)]int nFeatures, [InField(PVal = 3)]int nOctaveLayers, [InField(PVal = 0.04)]double contrastThreshold, [InField(PVal = 10.0)]double edgeThreshold, [InField(PVal = 1.6)]double sigma)
        {
            return (Feature2D)new SIFT(nFeatures, nOctaveLayers, contrastThreshold, edgeThreshold, sigma);
        }

        [NFunc(Name = "ORB", Category = "OpenCV/Detectors")]
        static Feature2D ORBComputer([InField(PVal = 500)]int numberOfFeatures, [InField(PVal = 1.2f)]float scaleFactor, [InField(PVal = 8)]int nLevels, [InField(PVal = 31)]int edgeThreshold, [InField(PVal = 0)]int firstLevel, [InField(PVal = 2)]int WTK_A, [InField(PVal = 31)]int patchSize, [InField(PVal = 20)]int fastThreshold)
        {
            return (Feature2D)new ORBDetector(numberOfFeatures, scaleFactor, nLevels, edgeThreshold, firstLevel, WTK_A, ORBDetector.ScoreType.Harris, patchSize, fastThreshold);


        }





        [NFunc(Name = "Find Points", Category = "OpenCV/Features")]
        static Bitmap FindPoints(Bitmap Imodel, Bitmap Iobserved, Feature2D computer, Feature2D detector, out string matchTime, out string mCount, out BitmapImage model, [InField(PVal = "White")]Brush PointColor) // сводим все полученные данные
        {
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            long time;

            var modelImage = new Image<Bgr, byte>(Imodel);
            var observedImage = new Image<Bgr, byte>(Iobserved);

            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                SURFMethods.FindMatchWM(modelImage.Mat, observedImage.Mat, out time, out modelKeyPoints, out observedKeyPoints, matches,
                out mask, out homography, computer, detector);

                int miw = Imodel.Width;
                int oiw = Iobserved.Width;
                int oih = Iobserved.Height;

                var size = new System.Drawing.Size(miw + oiw, oih);

                Image<Bgr, byte> Combined = new Image<Bgr, byte>(size);
                var g = Graphics.FromImage(Combined.Bitmap);
                g.DrawImage(new Bitmap(Iobserved), PointF.Empty);
                g.DrawImage(new Bitmap(Imodel), new PointF(oiw, 0));


                Mat result = new Mat();
                Color color = new Pen(PointColor).Color;
                Features2DToolbox.DrawMatches(modelImage.Mat, modelKeyPoints, observedImage.Mat, observedKeyPoints, matches, result, new MCvScalar(color.B, color.G, color.R), new MCvScalar(color.B, color.G, color.R), mask, Features2DToolbox.KeypointDrawType.NotDrawSinglePoints);


                if (homography != null)
                {
                    //draw a rectangle along the projected model
                    Rectangle rect = new Rectangle(System.Drawing.Point.Empty, modelImage.Size);
                    PointF[] pts = new PointF[]
                    {
                      new PointF(rect.Left, rect.Bottom),
                      new PointF(rect.Right, rect.Bottom),
                      new PointF(rect.Right, rect.Top),
                      new PointF(rect.Left, rect.Top)
                    };
                    pts = CvInvoke.PerspectiveTransform(pts, homography);

                    System.Drawing.Point[] points = Array.ConvertAll<PointF, System.Drawing.Point>(pts, System.Drawing.Point.Round);
                    using (VectorOfPoint vp = new VectorOfPoint(points))
                    {
                        CvInvoke.Polylines(result, vp, true, new MCvScalar(0, 0, 255, 255), 1);
                    }

                }


                var resc = result.ToImage<Bgr, byte>();
                model = B2BI(new Bitmap(resc.Bitmap));


                matchTime = "Time: " + time + " ms";
                mCount = "Amount: " + matches.Size;

                return result.Bitmap;

            }
        }

        [NFunc(Name = "Canny", Category = "OpenCV/Image Processing")]
        static Bitmap Canny(Bitmap img, [InField(PVal = 1)]float threshold, [InField(PVal = 2)]float ratio, [InField(PVal = 3)]int apertureSize, [InField(PVal = false)]bool l2Gradient, out string time, out BitmapImage MidImg)
        {
            var inp = new Image<Gray, byte>(new Bitmap(img));
            var outp = new Image<Gray, byte>(inp.Size);
            var now = DateTime.Now;
            CvInvoke.Canny(inp, outp, threshold, threshold * ratio, apertureSize, l2Gradient);
            time = "time: " + (DateTime.Now - now).Milliseconds;

            MidImg = B2BI(new Bitmap(outp.Bitmap));
            var bgr = new Bitmap(outp.Convert<Bgr, byte>().Bitmap);

            return bgr;
        }

        [NFunc(Name = "BGR Channels", Category = "OpenCV/Image Processing", OutputTypes = new Type[] { typeof(Bitmap), typeof(Bitmap), typeof(Bitmap) })]
        static object[] BGRChannels(Bitmap img, out string chans)
        {
            Image<Bgr, byte> inp = new Image<Bgr, byte>(img);
            var channels = inp.Split();
            chans = "channels: " + channels.Length;
            Bitmap B = new Bitmap(channels[0].Bitmap);
            Bitmap G = new Bitmap(channels[1].Bitmap);
            Bitmap R = new Bitmap(channels[2].Bitmap);
            return new object[] { B, G, R };
        }

        [NFunc(Name = "Find Contours235", Category = "OpenCV/Image Processing", OutputTypes = new Type[] { typeof(Bitmap), typeof(Bitmap), typeof(Bitmap[]), typeof(Array), typeof(VectorOfPoint[]) })]
        static object[] FindContours(Bitmap img, [InField(PVal = 25)]float MinContourSize, [InField(PVal = 500)]float MaxContourSize, [InField(PVal = RetrType.List)]RetrType retrType, [InField(PVal = ChainApproxMethod.ChainApproxNone)]ChainApproxMethod cam, [InField(PVal = true)]bool DrawContours, out string time, out BitmapImage drawedCs)
        {
            var inp = new Image<Bgr, byte>(img);
            var onlyCont = new Image<Bgra, byte>(inp.Size);
            var inpGray = new Image<Gray, byte>(img);
            var conts = new VectorOfVectorOfPoint();
            var now = DateTime.Now;
            CvInvoke.FindContours(inpGray, conts, null, retrType, cam);

            time = "Find Time: " + (DateTime.Now - now).TotalMilliseconds;

            List<VectorOfPoint> contsList = new List<VectorOfPoint>();
            var cout = new List<VectorOfPoint>();
            for (int i = 0; i < conts.Size; i++)
            {
                var con = conts[i];
                var area = CvInvoke.ContourArea(con);
                if (con.Size > 2)
                    if (area >= MinContourSize && area < MaxContourSize)
                    {
                        contsList.Add(con);
                        cout.Add(new VectorOfPoint(con.ToArray()));
                    }
            }



            now = DateTime.Now;

            var parts = new List<Bitmap>();
            Bitmap[,] outpArr = null;
            var moments = new List<string>();
            double[,] oArr = null;

            if (DrawContours)
            {
                for (int i = 0; i < contsList.Count; i++)
                {
                    VectorOfPoint contour = contsList[i];
                    var convex = contour.ToArray();//CvInvoke.ConvexHull(contour.ToArray().Select(x => new PointF(x.X, x.Y)).ToArray());
                    var pnt = convex.Select(x => new System.Drawing.Point((int)x.X, (int)x.Y)).ToArray();
                    inp.Draw(pnt, new Bgr(0, 255, 0), 1);

                    onlyCont.Draw(contour.ToArray(), new Bgra(255, 255, 255, 255), 1);
                }
                for (int i = 0; i < contsList.Count; i++)
                {
                    VectorOfPoint contour = contsList[i];
                    var cx = contour.ToArray().Sum(x => x.X) / contour.Size;
                    var cy = contour.ToArray().Sum(x => x.Y) / contour.Size;
                    System.Drawing.Point center = new System.Drawing.Point(cx, cy);
                    inp.Draw(string.Format("[{0}|{1}|{2}]", i, CvInvoke.ContourArea(contour), contour.Size), center, FontFace.HersheyPlain, 1, new Bgr(0, 0, 255), 1);
                }

                for (int i = 0; i < contsList.Count; i++)
                {

                    VectorOfPoint contour = contsList[i];
                    var cArray = contour.ToArray();
                    var left = cArray.Min(x => x.X);
                    var right = cArray.Max(x => x.X);
                    var top = cArray.Min(x => x.Y);
                    var bot = cArray.Max(x => x.Y);
                    var width = right - left;
                    var height = bot - top;

                    int size = 500;

                    Image<Bgr, byte> part = new Image<Bgr, byte>(new System.Drawing.Size(width > size ? width : size, height > size ? height : size));

                    for (int k = 0; k < cArray.Length; k++)
                    {
                        cArray[k].X -= left;
                        cArray[k].Y -= top;
                    }
                    part.Draw(cArray, new Bgr(Color.White));


                    part.Draw(string.Format("[num: {0}; area: {1}; size: {2}]", i, CvInvoke.ContourArea(contour), contour.Size), new System.Drawing.Point(0, part.Height - 5), FontFace.HersheyPlain, 1, new Bgr(0, 0, 255), 1);



                    MCvMoments cvmoments = CvInvoke.Moments(contour);
                    var ncmoment = CvInvoke.cvGetNormalizedCentralMoment(ref cvmoments, 0, 0);
                    var cmom = CvInvoke.cvGetCentralMoment(ref cvmoments, 0, 0);
                    var smom = CvInvoke.cvGetSpatialMoment(ref cvmoments, 0, 0);



                    moments.Add(string.Format("CM: {0} || NCM: {1} || SM: {2}", cmom, ncmoment, smom));



                    parts.Add(new Bitmap(part.Bitmap));

                }


            }
            time = time + "|| Draw Time: " + (DateTime.Now - now).TotalMilliseconds + " Contours: " + contsList.Count;

            if (DrawContours)
                drawedCs = B2BI(inp.Bitmap);
            else
                drawedCs = null;

            return new object[] { new Bitmap(inp.Bitmap), new Bitmap(onlyCont.Bitmap), parts.ToArray(), moments.ToArray(), cout.ToArray() };
        }

        [NFunc(Name = "Match Shapes", Category = "OpenCV/Contours")]
        static Array MatchShapes(VectorOfPoint[] First, VectorOfPoint[] Second, [InField]MatchTypes matchType)
        {
            Array outp = default(Array);
            var matches = new List<IndexedValue>();
            switch (matchType)
            {
                case MatchTypes.Symmetry:
                    int max = First.Length > Second.Length ? Second.Length : First.Length;
                    for (int i = 0; i < max; i++)
                    {
                        var match = CvInvoke.MatchShapes(First[i], Second[i], ContoursMatchType.I1);
                        matches.Add(new IndexedValue() { i = i, k = 0, info = matches.Count.ToString(), value = match });
                    }
                    outp = matches.ToArray();
                    break;
                case MatchTypes.OneToMany:
                    for (int i = 0; i < First.Length; i++)
                        for (int k = 0; k < Second.Length; k++)
                        {
                            var match = CvInvoke.MatchShapes(First[i], Second[k], ContoursMatchType.I1);
                            matches.Add(new IndexedValue() { i = i, k = k, info = matches.Count.ToString(), value = match });
                        }
                    outp = matches.ToArray();
                    break;
                default:
                    break;
            }
            return outp;
        }
        enum MatchTypes { Symmetry, OneToMany }

        public struct IndexedValue : IComparable
        {
            public double value;
            public int i, k;
            public string info;
            public override string ToString()
            {
                return string.Format("[{1},{2}]({0}): {3}", info, i,k, value);
            }


            public int CompareTo(object obj)
            {
                IndexedValue objc = (IndexedValue)obj;
                if (value > objc.value) return 1;
                if (value < objc.value) return -1;
                return 0;
            }
        }

        [NFunc(Name = "Take Array Element", Category = "Basic")]
        static object TakeElement(Array arr, [InField(PVal = 0)]int I1, [InField(PVal = 0)]int I2, out string type)
        {
            object ret = null;
            if (arr.Rank == 1 && arr.GetLength(0) > I1)
                ret =  arr.GetValue(I1);
            if (arr.Rank == 2 && arr.GetLength(0) > I1 && arr.GetLength(1) > I2)
                ret =  arr.GetValue(new int[] { I1, I2 });
            type = ret.GetType().ToString();
            return ret;
        }

        [NFunc(Name = "Max Array Element", Category = "Basic")]
        static object MaxElement(Array arr, out string sout)
        {
            sout = null;
            object max = null;
            if (arr.GetType().GetElementType() == typeof(IndexedValue))
                if (arr.Rank == 1)
                {
                    var marr = (IndexedValue[])arr;
                    max = marr.Max();
                }
            if (arr.GetType().GetElementType().IsIConvertible())
                max = ((IConvertible[])arr).Max();
            sout = max.ToString();
            return max;
        }

        [NFunc(Name = "Contrast", Category = "OpenCV/Image Processing")]
        static Bitmap Contrast(Bitmap img, out string time, out BitmapImage res)
        {
            var copy = new Bitmap(img);
            var cvimg = new Image<Gray, byte>(copy);


            var data = cvimg.Data;
            var now = DateTime.Now;
            for (int x = 0; x < cvimg.Rows; x++)
                for (int y = 0; y < cvimg.Cols; y++)
                {
                    var i = data[x, y, 0];
                    if (i != 0)
                    {
                        data[x, y, 0] = 255;
                    }
                }
            time = "Time: " + (DateTime.Now - now).ToString();
            res = B2BI(new Bitmap(cvimg.Bitmap));
            return new Bitmap(cvimg.Bitmap);
        }

        [NFunc(Name = "EqHist", Category = "OpenCV/Image Processing")]
        static Bitmap EqHist(Bitmap img, out string time, out BitmapImage res)
        {
            var copy = new Bitmap(img);
            var cvimg = new Image<Bgr, byte>(copy);

            var data = new Image<Bgr, byte>(cvimg.Size);
            var now = DateTime.Now;

            var channels = cvimg.Split();


            CvInvoke.EqualizeHist(channels[0], channels[0]);
            CvInvoke.EqualizeHist(channels[1], channels[1]);
            CvInvoke.EqualizeHist(channels[2], channels[2]);

            data = new Image<Bgr, byte>(channels);



            time = "Time: " + (DateTime.Now - now).ToString();
            res = B2BI(new Bitmap(data.Bitmap));
            return new Bitmap(data.Bitmap);
        }

        [NFunc(Name = "Hough Circles", Category = "OpenCV/Image Processing", OutputTypes = new Type[] { typeof(Bitmap), typeof(Bitmap) })]
        static object[] HoughCircles(Bitmap img, [InField(PVal = 1)]float dp, [InField(PVal = 10)]float minDist, [InField(PVal = 100)]float param1, [InField(PVal = 100)] float param2, [InField(PVal = 0)]float minRadius, [InField(PVal = 0)]float maxRadius, out BitmapImage ret)
        {
            var copy = new Bitmap(img);
            var cvimg = new Image<Gray, byte>(copy);
            var circles = CvInvoke.HoughCircles(cvimg, HoughType.Gradient, (double)dp, (double)minDist, (double)param1, (double)param2, (int)minRadius, (int)maxRadius);
            var outp = new Image<Bgr, byte>(copy);

            foreach (var c in circles)
            {
                outp.Draw(c, new Bgr(0, 255, 0), 1);
                cvimg.Draw(c, new Gray(255), 1);
            }

            ret = B2BI(new Bitmap(cvimg.Bitmap));
            return new object[] { new Bitmap(cvimg.Bitmap), new Bitmap(outp.Bitmap) };
        }

        [NFunc(Name = "HoughLines", Category = "OpenCV/Image Processing", OutputTypes = new Type[] { typeof(Bitmap), typeof(Bitmap) })]
        static object[] HoughLines(Bitmap img, [InField(PVal = 1.0f, Max = 10.0f, Min = 0.0f)]float rho, [InField(PVal = Math.PI / 45, Max = 1.0f, Min = 0.0f)]float theta, [InField(PVal = 20.0f, Max = 100.0f, Min = 0.0f)]float threshold, [InField(PVal = 30.0f, Max = 100.0f, Min = 0.0f)]float minLineLength, [InField(PVal = 10.0f, Max = 100.0f, Min = 0.0f)]float maxGap, [InField(PVal = "White")]Brush PointColor, out BitmapImage ret)
        {
            var copy = new Bitmap(img);
            var gray = new Image<Gray, byte>(copy);
            var image = new Image<Bgr, byte>(copy);
            var sized = new Image<Bgr, byte>(image.Size);
            Color color = new Pen(PointColor).Color;

            var lines = CvInvoke.HoughLinesP(gray, rho, theta, (int)threshold, minLineLength, maxGap);

            foreach (var line in lines)
            {
                image.Draw(line, new Bgr(color.B, color.G, color.R), 1);
                sized.Draw(line, new Bgr(color.B, color.G, color.R), 1);
            }


            ret = B2BI(new Bitmap(image.Bitmap));
            return new object[] { new Bitmap(image.Bitmap), new Bitmap(sized.Bitmap) };
        }


        [NFunc(Name = "Mask", Category = "OpenCV/Image Processing")]
        static Bitmap Mask(Bitmap image, Bitmap mask, [InField(PVal = MaskType.And)]MaskType maskType, out BitmapImage res)
        {
            var img = new Image<Gray, byte>(new Bitmap(image));
            var msk = new Image<Gray, byte>(new Bitmap(mask));
            var result = new Image<Gray, byte>(img.Size);



            switch (maskType)
            {
                case MaskType.And:
                    result = img.And(msk);
                    break;
                case MaskType.Or:
                    result = img.Or(msk);
                    break;
                case MaskType.Xor:
                    result = img.Xor(msk);
                    break;
                default:
                    break;
            }

            res = B2BI(new Bitmap(result.Bitmap));
            return new Bitmap(result.Bitmap);
        }
        enum MaskType { And, Or, Xor }

        [NFunc(Name = "Image Not()", Category = "OpenCV/Image Processing")]
        static Bitmap ImageNot(Bitmap image, out BitmapImage res)
        {
            var img = new Image<Bgr, byte>(new Bitmap(image));
            var outp = img.Not();

            res = B2BI(new Bitmap(outp.Bitmap));
            return new Bitmap(outp.Bitmap);
        }

        [NFunc(Name = "TableTest", Category = "")]
        static void TableTest([InField(PVal = 1)] int sizex, [InField(PVal = 1)] int sizey, [InField(PVal = 0)]int add, out int[,] arr)
        {
            arr = new int[sizex, sizey];
            int val = 0;
            for (int i = 0; i < sizex; i++)
            {
                for (int k = 0; k < sizey; k++)
                {
                    arr[i, k] = ++val + add;

                }
            }

        }
        [NFunc(Name = "DivideTest", Category = "")]
        static Bitmap[,] DivideImage(Bitmap img, [InField(PVal = 1)] int xcount, [InField(PVal = 1)] int ycount, out string info, out BitmapImage[,] arr)
        {
            var copy = new Bitmap(img);
            var cvimg = new Image<Bgr, byte>(copy);

            var w = cvimg.Width / xcount;
            var h = cvimg.Height / ycount;

            var outp = new Bitmap[ycount, xcount];
            arr = new BitmapImage[ycount, xcount];

            for (int x = 0; x < xcount; x++)
                for (int y = 0; y < ycount; y++)
                {
                    var part = cvimg.Copy(new Rectangle(x * w, y * h, w, h));
                    arr[y, x] = B2BI(new Bitmap(part.Bitmap));
                    arr[y, x].Freeze();
                    outp[y, x] = new Bitmap(part.Bitmap);

                }
            info = string.Format(@"w:{0} || h:{1}", w, h);
            return outp;
        }

        [NFunc(Name = "Show Image Array", Category = "")]
        static void ShowImgArray(Bitmap[] imgarr, out BitmapImage[] arr)
        {
            arr = null;
            if (imgarr.GetType() == typeof(Bitmap[]))
            {
                arr = new BitmapImage[imgarr.GetLength(0)];
                var inp = (imgarr as Bitmap[]);
                for (int i = 0; i < imgarr.GetLength(0); i++)
                {
                    if (inp[i] == null) continue;
                    arr[i] = B2BI(new Bitmap(inp[i]));
                    arr[i].Freeze();
                }
            }
        }

        [NFunc(Name = "Show Array", Category = "")]
        static void ShowArray(Array inarr, out Array arr)
        {
            arr = inarr;
        }

        [NFunc(Name = "Sort Array", Category = "")]
        static Array SortArray(Array input, out Array outp)
        {
            Array.Sort(input);
            outp = input;
            return input;
        }

        [NFunc(Name = "Draw Contour", Category="OpenCV/Image Processing")]
        static Bitmap DrawContour(Bitmap image, VectorOfPoint contour, out BitmapImage bout)
        {
            bout = null;
            var copy = new Image<Bgr, byte>(new Bitmap(image));

            copy.Draw(contour.ToArray(), new Bgr(0,0,255));
            bout = B2BI(new Bitmap(copy.Bitmap));
            return new Bitmap(copy.Bitmap);
        }


        [NFuncBuilder]
        static NodeControl Code()
        {
            NodeControl node = new NodeControl();
            return null;
        }






        static Mat BI2M(BitmapImage bi)
        {
            var bmp = BI2B(bi);
            Image<Rgba, byte> img = new Image<Rgba, byte>(bmp);



            return img.Mat;
        }

        static BitmapImage M2BI(Mat mat)
        {
            var bmp = mat.Bitmap;
            return B2BI(bmp);
        }

        public static Bitmap BI2B(BitmapImage bi)
        {
            Bitmap bm;
            using (System.IO.MemoryStream outStream = new System.IO.MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bi));
                enc.Save(outStream);
                bm = new Bitmap(outStream);
            }
            return bm;
        }

        static BitmapImage B2BI(Bitmap bmp)
        {
            BitmapImage result = new BitmapImage();
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                result.BeginInit();
                result.StreamSource = ms;
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.EndInit();
            }
            return result;

        }


        public static bool IsIConvertible(this Type t)
        {
            return t.GetInterfaces().Contains(typeof(IConvertible));
        }

    }

    public class NodeControlToken
    {
        public NodeControl Node;
        public NodeControlToken(NodeControl node)
        {
            Node = node;
        }

        public void AddInputSocket()
        {

        }
    }

    //out - вывод в MiddlePanel
    //InField - Ввод через поле
    //OutputTypes - Вывод массива
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NFuncAttribute : Attribute
    {
        public string Name, Category;
        public Type[] OutputTypes;
        public NFuncAttribute()
            : this("Node", "Basic", null)
        {

        }
        public NFuncAttribute(string name, string category, Type[] multOutputTypes)
        {
            Name = name;
            Category = category;
            OutputTypes = multOutputTypes;
        }

    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NFuncBuilderAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class InField : Attribute
    {
        public object PVal;
        public float Min = 0, Max = 1000;
    }



}
