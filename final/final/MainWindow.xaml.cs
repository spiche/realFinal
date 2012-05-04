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
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;


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

        bool movement = false;

        private const string AcceptedSpeechPrefix = "Accepted_";
        private const string RejectedSpeechPrefix = "Rejected_";

        //private KinectSensor kinect;
        //private double angle;
        private bool running = true;
        private DispatcherTimer readyTimer;
        private EnergyCalculatingPassThroughStream stream;
        private SpeechRecognitionEngine speechRecognizer;



        public MainWindow()
        {
            InitializeComponent();


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

        bool closing = false;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        int count = 0;
        float oldX, oldY;

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
            label1.Content = dir;

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

        private void savePicture()
        {
            BitmapSource image = (BitmapSource)image2.Source;

            image.Save(DateTime.Now.ToString("ddMMyyyy HHmmss") + ".jpg", ImageFormat.Jpeg);
        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;
            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
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

            sensor.SkeletonStream.Enable(parameters);
            sensor.SkeletonStream.Enable();
            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            //// Only enable this checkbox if we have a sensor
            //enableAec.IsEnabled = this.kinect != null;

            try
            {
                sensor.Start();
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
            try
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.Start();
            }
            catch (Exception)
            {
                SensorChooser.AppConflictOccurred();
                return;
            }

            if (this.speechRecognizer != null && sensor != null)
            {
                // NOTE: Need to wait 4 seconds for device to be ready to stream audio right after initialization
                this.readyTimer = new DispatcherTimer();
                this.readyTimer.Tick += this.ReadyTimerTick;
                this.readyTimer.Interval = new TimeSpan(0, 0, 4);
                this.readyTimer.Start();

                this.ReportSpeechStatus("Initializing audio stream...");
                this.UpdateInstructionsText(string.Empty);

                this.Closing += this.MainWindowClosing;
            }

            this.running = true;
        }

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            this.Start();
            this.ReportSpeechStatus("Ready to recognize speech!");
            this.UpdateInstructionsText("Say: 'save'");
            this.readyTimer.Stop();
            this.readyTimer = null;
        }

        private void UninitializeKinect()
        {
            var sensor = this.kinect;
            this.running = false;
            if (this.speechRecognizer != null && sensor != null)
            {
                sensor.AudioSource.Stop();
                sensor.Stop();
                this.speechRecognizer.RecognizeAsyncCancel();
                this.speechRecognizer.RecognizeAsyncStop();
            }

            if (this.readyTimer != null)
            {
                this.readyTimer.Stop();
                this.readyTimer = null;
            }
        }

        private SpeechRecognitionEngine CreateSpeechRecognizer()
        {
            RecognizerInfo ri = GetKinectRecognizer();
            if (ri == null)
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
Ensure you have the Microsoft Speech SDK installed.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
Ensure you have the Microsoft Speech SDK installed and configured.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            sre.SpeechHypothesized += this.SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;

            return sre;
        }

        private void RejectSpeech(RecognitionResult result)
        {
            string status = "Rejected: " + (result == null ? string.Empty : result.Text + " " + result.Confidence);
            this.ReportSpeechStatus(status);

            Dispatcher.BeginInvoke(new Action(() => { tbColor.Background = blackBrush; }), DispatcherPriority.Normal);
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.RejectSpeech(e.Result);
        }

        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            this.ReportSpeechStatus("Hypothesized: " + e.Result.Text + " " + e.Result.Confidence);
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SolidColorBrush brush;

            if (e.Result.Confidence < 0.5)
            {
                this.RejectSpeech(e.Result);
                return;
            }

            switch (e.Result.Text.ToUpperInvariant())
            {
                case "SAVE":
                    brush = this.redBrush;
                    break;

                //default:
                //    brush = this.blackBrush;
                //    break;
            }

            string status = "Recognized: " + e.Result.Text + " " + e.Result.Confidence;
            this.ReportSpeechStatus(status);

            Dispatcher.BeginInvoke(new Action(() => { tbColor.Background = brush; }), DispatcherPriority.Normal);
        }

        private void ReportSpeechStatus(string status)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbSpeechStatus.Text = status; }), DispatcherPriority.Normal);
        }

        private void UpdateInstructionsText(string instructions)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbColor.Text = instructions; }), DispatcherPriority.Normal);
        }

        private void Start()
        {
            var audioSource = this.kinect.AudioSource;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = audioSource.Start();
            this.stream = new EnergyCalculatingPassThroughStream(kinectStream);
            this.speechRecognizer.SetInputToAudioStream(
                this.stream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            var t = new Thread(this.PollSoundSourceLocalization);
            t.Start();
        }

        private void EnableAecChecked(object sender, RoutedEventArgs e)
        {
            CheckBox enableAecCheckBox = (CheckBox)sender;
            if (enableAecCheckBox.IsChecked != null)
            {
                this.kinect.AudioSource.EchoCancellationMode = enableAecCheckBox.IsChecked.Value
                                                             ? EchoCancellationMode.CancellationAndSuppression
                                                             : EchoCancellationMode.None;
            }
        }

        private class EnergyCalculatingPassThroughStream : Stream
        {
            private const int SamplesPerPixel = 10;

            private readonly double[] energy = new double[WaveImageWidth];
            private readonly object syncRoot = new object();
            private readonly Stream baseStream;

            private int index;
            private int sampleCount;
            private double avgSample;

            public EnergyCalculatingPassThroughStream(Stream stream)
            {
                this.baseStream = stream;
            }

            public override long Length
            {
                get { return this.baseStream.Length; }
            }

            public override long Position
            {
                get { return this.baseStream.Position; }
                set { this.baseStream.Position = value; }
            }

            public override bool CanRead
            {
                get { return this.baseStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return this.baseStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return this.baseStream.CanWrite; }
            }

            public override void Flush()
            {
                this.baseStream.Flush();
            }

            public void GetEnergy(double[] energyBuffer)
            {
                lock (this.syncRoot)
                {
                    int energyIndex = this.index;
                    for (int i = 0; i < this.energy.Length; i++)
                    {
                        energyBuffer[i] = this.energy[energyIndex];
                        energyIndex++;
                        if (energyIndex >= this.energy.Length)
                        {
                            energyIndex = 0;
                        }
                    }
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int retVal = this.baseStream.Read(buffer, offset, count);
                const double A = 0.3;
                lock (this.syncRoot)
                {
                    for (int i = 0; i < retVal; i += 2)
                    {
                        short sample = BitConverter.ToInt16(buffer, i + offset);
                        this.avgSample += sample * sample;
                        this.sampleCount++;

                        if (this.sampleCount == SamplesPerPixel)
                        {
                            this.avgSample /= SamplesPerPixel;

                            this.energy[this.index] = .2 + ((this.avgSample * 11) / (int.MaxValue / 2));
                            this.energy[this.index] = this.energy[this.index] > 10 ? 10 : this.energy[this.index];

                            if (this.index > 0)
                            {
                                this.energy[this.index] = (this.energy[this.index] * A) + ((1 - A) * this.energy[this.index - 1]);
                            }

                            this.index++;
                            if (this.index >= this.energy.Length)
                            {
                                this.index = 0;
                            }

                            this.avgSample = 0;
                            this.sampleCount = 0;
                        }
                    }
                }

                return retVal;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return this.baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                this.baseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.baseStream.Write(buffer, offset, count);
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

                    image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                        96, 96, PixelFormats.Bgr32, null, pixels, stride);


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
                //label1.Content = diff + "";
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
            this.UninitializeKinect();
        }
    }
}
