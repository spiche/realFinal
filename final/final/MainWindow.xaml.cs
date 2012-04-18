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

        BitmapImage space;
        BitmapImage aus;
        BitmapImage ball;
        BitmapImage hawaii;
        BitmapImage london;

        List<BitmapImage> images;        

        public MainWindow()
        {
            InitializeComponent();


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


            background.Source = london;
            background.Height = 350;
            background.Width = 500;

            //changeBackground();

        }

        private void changeBackground()
        {
            //List of images:  space(0) aus(1) ball(2)
            //                  hawaii(3) london(4)
            int count = 0;

            ConsoleKeyInfo x = Console.ReadKey();
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


    }
}
