﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TexTed
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var filePath = "C:\\Users\\abloh\\Desktop\\sw_mos\\texted_sharp\\TexTed\\testfile.txt";
            textViewer.FilePath = filePath;
        }
    }
}