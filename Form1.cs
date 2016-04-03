using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading; 


using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using OpenCvSharp.UserInterface;
using OpenCvSharp.Utilities;

namespace Cam
{
    public partial class Form1 : Form
    {
        CvCapture capture;
        IplImage imgSrc;
        TestSURF tsurf;


        public bool isComputing;                           //지금 추출 중인가?
        OpenCvSharp.CPlusPlus.Point[] rectanPts;    //추출된 영역. 4개의 Point로 구성되어 있다.
        Thread ComputingSURF;                       //계산을 하는 스레드.

        public int capture_type;
        static int DO_WITH_DIRECT_WEBCAM = 0;       //PC에 직접연결된 웹캠
        static int DO_WITH_IP_CAMERA = 1;           //IP 웹캠(안드로이드도 된다!)



        public Form1()
        {
            InitializeComponent();
            isComputing = false;
            //capture_type = DO_WITH_IP_CAMERA;           //웹캠 or IP웹캠
            capture_type = DO_WITH_DIRECT_WEBCAM;

            rectanPts = new OpenCvSharp.CPlusPlus.Point[4];
            //init
            for (int i = 0; i < 4; i++ )
                rectanPts[i].X = rectanPts[i].Y = 0;
            

            //TestSIFT는 SURF+kdTreeMatching+RANSAC 을 구현하여 영역을 추출한다.
            tsurf = new TestSURF();
            tsurf.setTarget("origim.png");      //검색하려는 이미지 파일명
            
            
            
        }

        //Capture a image from webcam in Timer
        private void timer1_Tick(object sender, EventArgs e)
        {

            /*
            if (capture_type == DO_WITH_IP_CAMERA)
            {
                //capture = CvCapture.FromFile("[IP CAMERA ADDR]");
                capture.SetCaptureProperty(CaptureProperty.FrameWidth, 680);
                capture.SetCaptureProperty(CaptureProperty.FrameHeight, 480);
            }
             */
          

            //Retrieve image as Iplimage to resize
            //이미지를 IplImage 타입으로 추출하여 가져온 후, resize한 이미지를 사용
            IplImage temimg = new IplImage(680, 480, BitDepth.U8, 3);
            temimg = capture.QueryFrame();

            if (temimg == null) return;

            imgSrc = Cv.CreateImage(new CvSize(680, 480), BitDepth.U8, 3);
            Cv.Resize(temimg, imgSrc, Interpolation.Linear);
            Cv.ReleaseImage(temimg);

            //Object Detection will be implmented by running thread.
            //If thread is running already, this procedure is to be skipped.
            //[This allows user see smooth webcam streaming]
            //영역 계산 중이라면 스킵. 계산 중이 아니라면 영역 계산 스레드를 실행
            if (!isComputing)
            {
                isComputing = true;

                ComputingSURF = new Thread(() => TestSURF.setImg(tsurf, this, imgSrc));
                ComputingSURF.Start();
                
            }
            
            
            //Draw Object Rectangle with cvLine
            //가지고 있는 영역 값을 이용해 매 영상마다 영역은 항상 표시하도록 한다.
            drawRec(rectanPts);
            pictureBoxIpl1.ImageIpl = imgSrc;

        }

        //Set object rectangle, this method is to be called in thread
        //가지고 있는 영역 값을 설정한다. 스레드에서 이 함수를 호출하여, 계산이 끝날 때마다 업데이트 시킨다.
        public void setDetectionRec(int u0, int v0, int u1, int v1, int u2, int v2, int u3, int v3)
        {
            rectanPts[0] = new OpenCvSharp.CPlusPlus.Point(u0, v0);
            rectanPts[1] = new OpenCvSharp.CPlusPlus.Point(u1, v1);
            rectanPts[2] = new OpenCvSharp.CPlusPlus.Point(u2, v2);
            rectanPts[3] = new OpenCvSharp.CPlusPlus.Point(u3, v3);
        }

        //draw rectangle with cvLine
        //가지고 있는 영역 값대로 사각형을 그린다.
        public void drawRec(OpenCvSharp.CPlusPlus.Point[] pts)
        {
            Cv.Line(imgSrc, new CvPoint(pts[0].X, pts[0].Y), new CvPoint(pts[1].X, pts[1].Y), new CvScalar(0, 255, 0), 4);
            Cv.Line(imgSrc, new CvPoint(pts[1].X, pts[1].Y), new CvPoint(pts[2].X, pts[2].Y), new CvScalar(0, 255, 0), 4);
            Cv.Line(imgSrc, new CvPoint(pts[2].X, pts[2].Y), new CvPoint(pts[3].X, pts[3].Y), new CvScalar(0, 255, 0), 4);
            Cv.Line(imgSrc, new CvPoint(pts[3].X, pts[3].Y), new CvPoint(pts[0].X, pts[0].Y), new CvScalar(0, 255, 0), 4);   
        }

        private void pictureBoxIpl1_Click(object sender, EventArgs e)
        {
            if (initCamera())
            {
                StartTimer();
            }
            else
            {
                MessageBox.Show("Cannot detect Camera");
            }
        }

        private bool initCamera()
        {
            try
            {
                if (capture_type == DO_WITH_DIRECT_WEBCAM)
                {
                    capture = CvCapture.FromCamera(CaptureDevice.DShow, 0);
                    capture.SetCaptureProperty(CaptureProperty.FrameWidth, 680);
                    capture.SetCaptureProperty(CaptureProperty.FrameHeight, 480);

                    return true;
                }

                if (capture_type == DO_WITH_IP_CAMERA)
                {
                    //capture = CvCapture.FromFile("http://192.168.0.5:8080/shot.jpg");
                    //capture.SetCaptureProperty(CaptureProperty.FrameWidth, 680);
                    //capture.SetCaptureProperty(CaptureProperty.FrameHeight, 480);

                    return true;
                }
                    
                return false;
            }
            catch
            {
                return false;
            }
        }
        private void StartTimer()
        {
            timer1.Interval = 2;
            timer1.Enabled = true;
        }
        
    }
}
