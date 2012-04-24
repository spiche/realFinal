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

        bool on;
        ConsoleKeyInfo x;

        List<BitmapImage> images;

        public MainWindow()
        {
            InitializeComponent();

        }

        //KinectSensor _sensor;
        bool closing = false;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

            /*
            on = true;
            */
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

            //image1.Height = 500;
            //image1.Width = 800;


            //while (on)
            //{
            //    input();
            //}
        }

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;

            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                depthIndex++, colorIndex += 4)
            {
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;


                if (player == 1 || player == 2)
                {
                    //do something with the pixels
                    pixels[colorIndex + BlueIndex] = 255;
                    pixels[colorIndex + GreenIndex] = 255;
                    pixels[colorIndex + RedIndex] = 255;
                }
                else
                {
                    //non-player pixels are transparent
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;
                }


            }
            
            return pixels;
        }


        private void changeBackground(ConsoleKeyInfo x)
        {
            //List of images:  space(0) aus(1) ball(2)
            //                  hawaii(3) london(4)
            int count = 0;
            //ConsoleKeyInfo x = Console.ReadKey();
            if (x.Key.Equals(Key.Left))
            {
                if (count == 0)
                {
                    count = 4;
                    background.Source = images.ElementAt(count);

                }
                else
                {
                    count--;
                    background.Source = images.ElementAt(count);
                }
            }
            if (x.Key.Equals(Key.Right))
            {
                if (count == 4)
                {
                    count = 0;
                    background.Source = images.ElementAt(count);
                }
                else
                {
                    count++;
                    background.Source = images.ElementAt(count);
                }
            }
            else
            {
                //display message
                MessageBox.Show("Use left or right arrows to change background");
            }
        }

        void input()
        {
            x = Console.ReadKey();
            if (x.Key.Equals(Key.Escape))
            {
                on = false;
            }
            else
            {
                changeBackground(x);
                //x = Console.ReadKey();
            }
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

            //            sensor.SkeletonStream.Enable(parameters);
            //            sensor.SkeletonStream.Enable();
            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            //            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }


        //********************************
        //Might need to add to this method?
        //********************************
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                byte[] pixels = new byte[colorFrame.PixelDataLength];

                //copy data out into our byte array
                colorFrame.CopyPixelDataTo(pixels);
                int stride = colorFrame.Width * 4;

                image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                    96, 96, PixelFormats.Bgr32, null, pixels, stride);

            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                byte[] pixels = GenerateColoredBytes(depthFrame);
                int stride = depthFrame.Width * 4;

                image2.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                    96, 96, PixelFormats.Bgr32, null, pixels, stride);
            }

            //Get a skeleton
            /*
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }
            */
            //set scaled position
            //ScalePosition(headImage, first.Joints[JointType.Head]);
            //ScalePosition(leftEllipse, first.Joints[JointType.HandLeft]);
            //ScalePosition(rightEllipse, first.Joints[JointType.HandRight]);

            //GetCameraPoint(first, e);

        }

        //********************************
        //Might need to add to this method?
        //********************************
        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }

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
    }
}
