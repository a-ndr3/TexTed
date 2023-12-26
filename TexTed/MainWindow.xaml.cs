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

namespace TexTed
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var filePath = "";
            textViewer.FilePath = filePath;
        }

        private void verticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Canvas.SetTop(textViewer, -e.NewValue*this.Height);
        }

        private void horizontalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Canvas.SetLeft(textViewer, -e.NewValue*this.Width);
        }    
    }
}