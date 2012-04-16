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
using System.Timers;

namespace final
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Timer time;
        BitmapImage space;
        BitmapImage aus;
        BitmapImage ball;
        BitmapImage hawaii;
        BitmapImage london;
        

        public MainWindow()
        {
            InitializeComponent();

            //time = new Timer(3);
            //time.Elapsed += new ElapsedEventHandler(applyBackground);
            //time.Start();


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


            background.Source = london;
            background.Height = 350;
            background.Width = 500;

        }

        //private void applyBackground(object source, ElapsedEventArgs e)
        //{
        //    time.Stop();
        //    background.Height = 350;
        //    background.Width = 500;

            
        //}

    }
}
