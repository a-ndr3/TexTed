using Microsoft.Win32;
using System.Diagnostics;
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
            try
            {
                InitializeComponent();
                if (Application.Current.Properties["FileName"] != null)
                {
                    textViewer.FilePath = Application.Current.Properties["FileName"].ToString();
                }
                else
                {
                    textViewer.FilePath = "C:\\Users\\abloh\\Desktop\\sw_mos\\texted_sharp\\TexTed\\testfile_and_metadata_gen.txt";
                }
                Keyboard.Focus(textViewer);
            }
            catch (Exception e) { MessageBox.Show(e.ToString()); }
        }

        private void TextViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextViewer textViewer)
            {
                if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
                {
                    textViewer.HandleArrowKeyPress(e);

                    e.Handled = true;
                }
            }
        }

        private void verticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Canvas.SetTop(textViewer, -e.NewValue * this.Height);
        }

        private void horizontalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Canvas.SetLeft(textViewer, -e.NewValue * this.Width);
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string exePath = Environment.ProcessPath;
                Process? process = null;

                try
                {
                    process = Process.Start(exePath, openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    process?.Close();
                }
            }
            textViewer.Focus();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                textViewer.SaveFile();

                MessageBox.Show("File saved!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            textViewer.Focus();
        }

        private void fontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontComboBox.SelectedItem is ComboBoxItem selectedFontItem)
            {
                string fontFamilyName = selectedFontItem.Content.ToString();
                FontFamily fontFamily = new FontFamily(fontFamilyName);

                textViewer.SetFont(fontFamily);
            }

            textViewer.Focus();
        }

        private void fontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontSizeComboBox.SelectedItem is ComboBoxItem selectedSizeItem)
            {
                if (int.TryParse(selectedSizeItem.Content.ToString(), out int fontSize))
                {
                    textViewer.SetFontSize(fontSize);
                }
            }
            textViewer.Focus();
        }

        private void normalButton_Click(object sender, RoutedEventArgs e)
        {
            FontStyle fontStyle = FontStyles.Normal;
            textViewer.SetFontStyleForSelection(fontStyle);

            textViewer.Focus();
        }

        private void italicButton_Click(object sender, RoutedEventArgs e)
        {
            FontStyle fontStyle = FontStyles.Italic;
            textViewer.SetFontStyleForSelection(fontStyle);

            textViewer.Focus();
        }

        private void OblButton_Click(object sender, RoutedEventArgs e)
        {

            FontStyle fontStyle = FontStyles.Oblique;
            textViewer.SetFontStyleForSelection(fontStyle);

            textViewer.Focus();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = searchTextBox.Text;

            if (!string.IsNullOrEmpty(searchText))
            {
                textViewer.FindAndSelect(searchText);
            }
            textViewer.Focus();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            textViewer.Undo();
            textViewer.Focus();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            textViewer.Redo();
            textViewer.Focus();
        }
    }
}