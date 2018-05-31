using System;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;
using System.Collections.Generic;

namespace Work
{
    public static class SURFMethods//представляет из себя набор методов для обработки изображений с помощью OpenCV и получение из него необходимой информации о найденных особенностях 
    {
        public static double uniquenessThreshold = 0.8; // настройка SURF
        public static double hessianThresh = 300;// настройка SURF
        public static void DetectAndCompute(Mat img, out VectorOfKeyPoint keypoints, out Mat descriptors, Feature2D detector, Feature2D computer) // находит и обрабатывает дескрипторы изображения
        {
            keypoints = null;
            descriptors = new Mat();
            try
            {
                var mkp = detector.Detect(img,null);
                keypoints = new VectorOfKeyPoint(mkp);
            }
            catch (Exception e)
            {
                throw e;
            }

            try
            {
                computer.Compute(img, keypoints, descriptors);
            }
            catch (Exception e)
            {
                throw e;
            }
        } 

        public static void FindMatchWM(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Feature2D computer, Feature2D detector)
        {
            Stopwatch watch;
            modelKeyPoints = new VectorOfKeyPoint(); // точки на модели
            observedKeyPoints = new VectorOfKeyPoint(); // точки на большем изображении
            homography = null;
            int k = 2;


            using (Mat uModelImage = modelImage.Clone())
            using (Mat uObservedImage = observedImage.Clone())
            {
                //получаем дескрипторы из первого изображения
                Mat modelDescriptors = new Mat();
                DetectAndCompute(uModelImage, out modelKeyPoints, out modelDescriptors, detector, computer);

                watch = Stopwatch.StartNew();

                // ... из второго изображения
                Mat observedDescriptors = new Mat();
                DetectAndCompute(uObservedImage, out observedKeyPoints, out observedDescriptors, detector, computer);


                BFMatcher matcher = new BFMatcher(DistanceType.L2); // "сравниватель" дескрипторов на 2-х изображениях
                matcher.Add(modelDescriptors);

                matcher.KnnMatch(observedDescriptors, matches, k, null); // сравнение
                mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                mask.SetTo(new MCvScalar(255));
                Features2DToolbox.VoteForUniqueness(matches, 0.8, mask); // построениии маски (см ниже)

                int nonZeroCount = CvInvoke.CountNonZero(mask);
                if (nonZeroCount >= 4)
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                       matches, mask, 1.5, 20);
                    if (nonZeroCount >= 4)
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, // получение предположительной зоны, куда должна встать модель
                           observedKeyPoints, matches, mask, 2);
                }

                watch.Stop();
            }
            matchTime = watch.ElapsedMilliseconds;
        }

        //public static ImgInfo DrawInfo(Mat modelImage, Mat observedImage, out long matchTime, Methods method) // сводим все полученные данные
        //{
        //    Mat homography;
        //    VectorOfKeyPoint modelKeyPoints;
        //    VectorOfKeyPoint observedKeyPoints;
        //    using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
        //    {
        //        Mat mask;
        //        FindMatch(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches,
        //        out mask, out homography, method);

        //        #region Сводим изображения в одно
        //        Image<Bgr, byte> img = new Image<Bgr, byte>(new Size(modelImage.Width + observedImage.Width, observedImage.Height));
        //        Mat result = img.Mat;
        //        var g = Graphics.FromImage(result.Bitmap);
        //        g.DrawImage(observedImage.Bitmap, Point.Empty);
        //        g.DrawImage(modelImage.Bitmap, new Point(observedImage.Bitmap.Width, 0));
        //        #endregion

        //        #region Отрисовка проекции

        //        if (homography != null)
        //        {
        //            //draw a rectangle along the projected model
        //            Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
        //            PointF[] pts = new PointF[]
        //            {
        //              new PointF(rect.Left, rect.Bottom),
        //              new PointF(rect.Right, rect.Bottom),
        //              new PointF(rect.Right, rect.Top),
        //              new PointF(rect.Left, rect.Top)
        //            };
        //            pts = CvInvoke.PerspectiveTransform(pts, homography);

        //            Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
        //            using (VectorOfPoint vp = new VectorOfPoint(points))
        //            {
        //                CvInvoke.Polylines(result, vp, true, new MCvScalar(0, 0, 255, 255), 1);
        //            }

        //        }

        //        #endregion

        //        #region Берем совпадающие точки по маске
        //        var marr = matches.ToArrayOfArray();
        //        Dictionary<int, MDMatch[]> Dmatches = new Dictionary<int, MDMatch[]>();
        //        for (int i = 0; i < marr.Length; i++)
        //        {
        //            if (mask.GetData()[i] == 0) continue;
        //            var m = marr[i];
        //            Dmatches.Add(i, m);
        //        }
        //        MDMatch[][] matchArr = new MDMatch[Dmatches.Count][];
        //        Dmatches.Values.CopyTo(matchArr, 0);
        //        #endregion

        //        #region Сводим данные в список
        //        //OpenCV использует хитрую систему хранения дескрипторов, которая очень неудобна в практическом применение, поэто далее происходит трансляция типов данных OpenCV в более удобные для решения данной задачи
        //        List<PointToPoint> mtchs = new List<PointToPoint>();
        //        for (int i = 0; i < matchArr.Length; i++)
        //        {
        //            var match = matchArr[i][0];
        //            var ptp = new PointToPoint()
        //            {
        //                PModel = new Point((int)modelKeyPoints[match.TrainIdx].Point.X, (int)modelKeyPoints[match.TrainIdx].Point.Y),
        //                PObserved = new Point((int)observedKeyPoints[match.QueryIdx].Point.X, (int)observedKeyPoints[match.QueryIdx].Point.Y),
        //                Distance = match.Distance
        //            };
        //            mtchs.Add(ptp);
        //        }
        //        #endregion

        //        var imgInfo = new ImgInfo()
        //        {
        //            ModelImage = modelImage.Bitmap,
        //            ObservedImage = observedImage.Bitmap,
        //            CombinedImage = result.Bitmap,
        //            Matches = mtchs
        //        };

        //        return imgInfo;

        //    }
        //}
        public enum Methods { SURF, SIFT, FAST }
    }
    public class PointToPoint 
    {
        public Point PModel, PObserved;
        public float Distance;
    }

    public class ImgInfo
    {
        public Image ModelImage, ObservedImage, CombinedImage;
        public List<PointToPoint> Matches;

        public ImgInfo()
        {
            Matches = new List<PointToPoint>();
        }
    }
}