using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

/*
using Emgu.Util.TypeEnum;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
*/



using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using OpenCvSharp.UserInterface;
using OpenCvSharp.Utilities;

 
namespace Cam
{
    public class TestSURF
    {
        public Mat target;             //target Object
        public bool isComputing;       //bool var for running only single thread at once


        public SURF surfobj;             //SURF class
        public FlannBasedMatcher fm;     //MATCHER

        //---target image's keypoints and descriptor
        public KeyPoint[] t_keypoints;
        public Mat t_descriptor;
        Point2d[] obj_corners = new Point2d[4];

        public TestSURF()
        {
            //Constructor
            //생성자에서 필요한 배열들을 선언

            t_keypoints = new KeyPoint[10000];
            t_descriptor = new Mat();

            surfobj = new SURF();
            surfobj.HessianThreshold = 400;
            
            
            fm = new FlannBasedMatcher();
            isComputing = false;

        }

        //Set target image to retrieve keypoints and descriptor(SURF)
        //Target 등록하여 디스크립터 추출
        public void setTarget(string filename)
        {
            
            target = Cv2.ImRead(filename, LoadMode.GrayScale);

            t_keypoints = surfobj.Detect(target);                   //SURF keypoint
            surfobj.Compute(target, ref t_keypoints, t_descriptor); //SURF descriptor
            obj_corners = new Point2d[4];

            //target의 크기에 맞는 사각형 (For RANSAC)
            obj_corners[0] = new Point2d(0, 0);
            obj_corners[1] = new Point2d(target.Cols, 0);
            obj_corners[2] = new Point2d(target.Cols, target.Rows);
            obj_corners[3] = new Point2d(0, target.Rows);

        }

        //Set Frame image from Webcam to start matching&detection
        //img에 Mat 하나를 등록. 본격적인 매칭을 시작하도록 한다
        public static void setImg(TestSURF thisobj, Form1 mainform, IplImage imgFromCam)
        {
            if (thisobj.isComputing) return;


            thisobj.isComputing = true;

            //---frame 특징점, 디스크립터
            //---frame image's keypoints and descriptor
            KeyPoint[] f_keypoints;
            Mat f_descriptor;

            Mat imgOrig;            //캠 영상 (Original)
            Mat img;                //캠 영상 (Grayscale)

            //Convert to GrayScale Mat
            imgOrig = Cv2.CvArrToMat(imgFromCam);
            img = new Mat();
            Cv2.CvtColor(imgOrig, img, ColorConversion.BgrToGray);



            //---------------------1. 디스크립터 추출 (keypoint & descriptor retrieval)
            f_keypoints = new KeyPoint[10000];
            f_descriptor = new Mat();

            f_keypoints = thisobj.surfobj.Detect(img);                      //SIFT keypoint
            thisobj.surfobj.Compute(img, ref f_keypoints, f_descriptor);    //SIFT descriptor




            //---------------------2. 매칭 (descriptor Matching)
            DMatch[] matches = new DMatch[10000];

            try
            {
                matches = thisobj.fm.Match(thisobj.t_descriptor, f_descriptor);          //MATCHING
                
                //matching error will be caught in this block
            }
            catch { return; }

            
            //record proper distances for choosing Good Matches
            //좋은 매치를 찾기 위해 디스크립터 간 매칭 거리를 기록한다
            double max_dist = 0;
            double min_dist = 100;

            for(int i = 0; i < thisobj.t_descriptor.Rows; i++)
            {
                double dist = matches[i].Distance;

                if (dist < min_dist) min_dist = dist;
                if (dist > max_dist) max_dist = dist;
            }



            //---------------------3. gootmatch 탐색 (calculating goodMatches)
            List<DMatch> good_matches = new List<DMatch>();

            for(int i = 0; i < thisobj.t_descriptor.Rows; i++)
            {
                if (matches[i].Distance < 3 * min_dist)
                    good_matches.Add(matches[i]);
            }

            /*
            KeyPoint[] goodkey = new KeyPoint[good_matches.Count];
            for(int goodidx = 0; goodidx < good_matches.Count; goodidx++)
            {
                goodkey[goodidx] = new KeyPoint((f_keypoints[good_matches.ElementAt(goodidx).TrainIdx].Pt.X), (f_keypoints[good_matches.ElementAt(goodidx).TrainIdx].Pt.Y), f_keypoints[good_matches.ElementAt(goodidx).TrainIdx].Size);
            }
             */


            //Goodmatches의 keypoint 중, target과 frame 이미지에 해당하는 keypoint 정리
            Point2d[] target_lo = new Point2d[good_matches.Count];
            Point2d[] frame_lo = new Point2d[good_matches.Count];


            for(int i = 0; i < good_matches.Count; i++)
            {
                target_lo[i] = new Point2d(thisobj.t_keypoints[good_matches.ElementAt(i).QueryIdx].Pt.X,
                    thisobj.t_keypoints[good_matches.ElementAt(i).QueryIdx].Pt.Y);
                frame_lo[i] = new Point2d(f_keypoints[good_matches.ElementAt(i).TrainIdx].Pt.X,
                    f_keypoints[good_matches.ElementAt(i).TrainIdx].Pt.Y);
            }


            //Homography for RANSAC
            Mat hom = new Mat();

            
            //-------------------------------4. RANSAC
            hom = Cv2.FindHomography(target_lo, frame_lo, HomographyMethod.Ransac);
          
            Point2d[] frame_corners;
            frame_corners = Cv2.PerspectiveTransform(thisobj.obj_corners, hom);



            //Mat -> iplimage
            //IplImage returnimg = (IplImage)imgOrig;

            mainform.setDetectionRec((int)frame_corners[0].X, (int)frame_corners[0].Y,
                (int)frame_corners[1].X, (int)frame_corners[1].Y,
                (int)frame_corners[2].X, (int)frame_corners[2].Y,
                (int)frame_corners[3].X, (int)frame_corners[3].Y);

            mainform.isComputing = false;
            thisobj.isComputing = false;

            //Cv2.DrawKeypoints(imgOrig, goodkey, imgOrig);
            //Cv2.DrawKeypoints(img, f_keypoints, img);
            //Cv2.ImWrite("temtem.png", img);

            return;
        }




    }
}
