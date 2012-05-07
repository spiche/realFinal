using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using System.IO;
using Microsoft.Speech.Recognition;
using System.Windows.Threading;
using Microsoft.Speech.AudioFormat;


namespace final
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        BitmapImage space;
        BitmapImage aus;
        BitmapImage ball;
        BitmapImage hawaii;
        BitmapImage london;

        List<BitmapImage> images;

        Joint right;
        Joint left;
        Joint center;

        public MainWindow()
        {
            InitializeComponent();

        }

        bool closing = false;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        int count = 0;
        float oldX, oldY;

        private DispatcherTimer readyTimer;
        //private EnergyCalculatingPassThroughStream stream;
        private SpeechRecognitionEngine speechRecognizer;
        private KinectSensor kinect;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

            Uri src = new Uri(@"space.jpg", UriKind.Relative);
            space = new BitmapImage(src);
            Uri src1 = new Uri(@"australia.jpg", UriKind.Relative);
            aus = new BitmapImage(src1);
            Uri src2 = new Uri(@"ballpit.jpg", UriKind.Relative);
            ball = new BitmapImage(src2);
            Uri src3 = new Uri(@"hawaii.jpg", UriKind.Relative);
            hawaii = new BitmapImage(src3);
            Uri src4 = new Uri(@"london.jpg", UriKind.Relative);
            london = new BitmapImage(src4);

            images = new List<BitmapImage>();
            images.Add(space);
            images.Add(aus);
            images.Add(ball);
            images.Add(hawaii);
            images.Add(london);

            background.Source = space;
            background.Height = 500;
            background.Width = 800;

            image2.Height = 500;
            image2.Width = 800;

            InitializeKinect();

        }


        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;
            StopKinect(old);

            kinect = (KinectSensor)e.NewValue;

            if (kinect == null)
            {
                return;
            }

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };

            kinect.SkeletonStream.Enable(parameters);
            kinect.SkeletonStream.Enable();
            kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            //// Only enable this checkbox if we have a sensor
            //enableAec.IsEnabled = this.kinect != null;

            try
            {
                kinect.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        private void InitializeKinect()
        {
            var sensor = this.kinect;
            this.speechRecognizer = this.CreateSpeechRecognizer();

            if (this.speechRecognizer != null)/*&& sensor != null*/
            {
                //NOTE: Need to wait 4 seconds for device to be ready to stream audio right after initialization
                this.readyTimer = new DispatcherTimer();
                this.readyTimer.Tick += this.ReadyTimerTick;
                this.readyTimer.Interval = new TimeSpan(0, 0, 4);
                this.readyTimer.Start();

                //this.Closing += this.MainWindowClosing;
            }

            //this.running = true;
        }

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            this.Start();
            this.readyTimer.Stop();
            this.readyTimer = null;
        }

        private SpeechRecognitionEngine CreateSpeechRecognizer()
        {
            RecognizerInfo ri = GetKinectRecognizer();
            if (ri == null)
            {
                this.Close();
                return null;
            }

            SpeechRecognitionEngine sre;
            try
            {
                sre = new SpeechRecognitionEngine(ri.Id);
            }
            catch
            {
                this.Close();
                return null;
            }

            var grammar = new Choices();
            grammar.Add("save");


            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Append(grammar);

            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);

            sre.LoadGrammar(g);
            sre.SpeechRecognized += this.SreSpeechRecognized;
            //sre.SpeechHypothesized += this.SreSpeechHypothesized;
            //sre.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;
            //sre.SpeechRecognitionRejected += new
            //EventHandler<SpeechRecognitionRejectedEventArgs>(sre_SpeechRecognitionRejected);

            return sre;
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //label1.Content = "recognized";
            if (e.Result.Confidence < 0.5)
            {
                //this.RejectSpeech(e.Result);
                return;
            }

            switch (e.Result.Text.ToUpperInvariant())
            {
                case "SAVE":
                    savePicture();
                    break;

                //default:
                //    brush = this.blackBrush;
                //    break;
            }
        }

        private void Start()
        {
            var audioSource = this.kinect.AudioSource;
            //audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = audioSource.Start();
            //this.stream = new EnergyCalculatingPassThroughStream(kinectStream);
            this.speechRecognizer.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);

            //t.Start();
        }



        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame, ColorImageFrame colorFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;
            const int AlphaIndex = 3;

            byte[] pixelsColor = new byte[colorFrame.PixelDataLength];

            colorFrame.CopyPixelDataTo(pixelsColor);

            for (int depthIndex = 0, colorIndex = 0;
                    depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                    depthIndex++, colorIndex += 4)
            {
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                
                if (player == 1)
                {
                    pixels[colorIndex + BlueIndex] = pixelsColor[colorIndex+BlueIndex];
                    pixels[colorIndex + GreenIndex] = pixelsColor[colorIndex + GreenIndex];
                    pixels[colorIndex + RedIndex] = pixelsColor[colorIndex + RedIndex];
                    pixels[colorIndex + AlphaIndex] = 255;

                }
                /*
                else if (player == 2)
                {
                    pixels[colorIndex + BlueIndex] = pixelsColor[colorIndex + BlueIndex];
                    pixels[colorIndex + GreenIndex] = pixelsColor[colorIndex + GreenIndex];
                    pixels[colorIndex + RedIndex] = pixelsColor[colorIndex + RedIndex];
                }
                */
                else
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;    
                }
            }
            
            return pixels;
        }

        void changeBackground(string dir)
        {
            //images:   space (0), aus (1), ball (2)
            //          hawaii (3), london (4)
            

            if( dir.Equals("right"))
            {

                background.Source = images.ElementAt(count%5);
                count++;
                
            }
            
            if( dir.Equals("left") )
            {

                background.Source = images.ElementAt(count%5);
                count--;
                if (count < 0)
                {
                    count = 0;
                }
            }
        }

        
        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }


        private void savePicture()
        {
            /*
            BitmapSource imagePerson = (BitmapSource)image2.Source;
            BitmapSource imageBG = (BitmapSource)background.Source;
            BitmapSource img = (BitmapSource)canvas1;
            image.Save(DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".jpg", ImageFormat.Jpeg);
            */
            string path = "test.tif";
            FileStream fs = new FileStream(path, FileMode.Create);
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)canvas1.ActualWidth,
                (int)canvas1.ActualHeight, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
            bmp.Render(canvas1);
            BitmapEncoder encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(fs);
            fs.Close();
 
        }

        public static byte[] BitSourceToArray(BitmapSource bitmapSource)
        {
            BitmapEncoder encoder = new JpegBitmapEncoder();
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {

                    if (colorFrame == null || depthFrame==null )
                    {
                        return;
                    }

                    byte[] pixels = new byte[colorFrame.PixelDataLength];

                    //copy data out into our byte array
                    colorFrame.CopyPixelDataTo(pixels);
                    int stride = colorFrame.Width * 4;

                    //image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                    //    96, 96, PixelFormats.Bgr32, null, pixels, stride);


                    byte[] pixelsDepth = GenerateColoredBytes(depthFrame,colorFrame);
                    int strideDepth = depthFrame.Width * 4;

                    image2.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                        96, 96, PixelFormats.Bgra32, null, pixelsDepth, strideDepth);




                }
            }        

            //Get a skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            GetCameraPoint(first, e);

        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }

                right = first.Joints[JointType.HandRight];
                left = first.Joints[JointType.HandLeft];
                center = first.Joints[JointType.ShoulderCenter];
                
                float diff = oldX - first.Joints[JointType.ShoulderCenter].Position.X;

                if (diff > .035)
                {
                    changeBackground("right");
                }
                
                else if (diff <  -0.035)
                {
                    changeBackground("left");
                }
                
                oldX = first.Joints[JointType.ShoulderCenter].Position.X;
                oldY = first.Joints[JointType.ShoulderCenter].Position.Y;
                
                    //Map a joint location to a point on the depth map
                //left hand
                
                
                
                DepthImagePoint leftDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);
                //right hand
                DepthImagePoint rightDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);

                //Map a depth point to a point on the color image
                //left hand
                ColorImagePoint leftColorPoint =
                    depth.MapToColorImagePoint(leftDepthPoint.X, leftDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right hand
                ColorImagePoint rightColorPoint =
                    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Set location
                //CameraPosition(headImage, headColorPoint);
                //CameraPosition(leftEllipse, leftColorPoint);
                //CameraPosition(rightEllipse, rightColorPoint);
            }
        }

        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;
            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }
                }
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);
        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 

            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            StopKinect(kinectSensorChooser1.Kinect);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            savePicture();
        }
    }
}
