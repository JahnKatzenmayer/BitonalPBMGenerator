using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BitonalPBMGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void selectImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if(ofd.ShowDialog()==true)
            {
                try
                {
                    originalImage.ImageSource = new BitmapImage(new Uri(ofd.FileName));
                    previewImage.ImageSource = ConvertToGrayscale(new BitmapImage(new Uri(ofd.FileName)));
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid Image File!", "Error");
                }
            }
        }

        public BitmapImage ConvertToGrayscale(BitmapImage originalImage)
        {
   
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);

            int stride = writeableBitmap.PixelWidth * 4;

            byte[] pixels = new byte[writeableBitmap.PixelHeight * stride];
            writeableBitmap.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte average = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
                if(average>bwBalance.Value)
                {
                    average = 255;
                } else
                {
                    average = 0;
                }

                pixels[i] = average;
                pixels[i + 1] = average;
                pixels[i + 2] = average;
            }

            WriteableBitmap grayscaleBitmap = new WriteableBitmap(originalImage.PixelWidth, originalImage.PixelHeight, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Bgra32, null);
            grayscaleBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, originalImage.PixelWidth, originalImage.PixelHeight), pixels, stride, 0);

            BitmapImage grayscaleImage = new BitmapImage();
            using (var stream = new System.IO.MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(grayscaleBitmap));
                encoder.Save(stream);
                grayscaleImage.BeginInit();
                grayscaleImage.CacheOption = BitmapCacheOption.OnLoad;
                grayscaleImage.StreamSource = stream;
                grayscaleImage.EndInit();
            }

            return grayscaleImage;
        }

        public static int[] GetRedChannel(BitmapImage bitmapImage)
        {

            WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            int stride = width * 4;

            byte[] pixels = new byte[height * stride];
            writeableBitmap.CopyPixels(pixels, stride, 0);

            int[] redChannel = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    redChannel[y * width + x] = pixels[index + 2]==255?0:1;
                }
            }

            return redChannel;
        }

        public static void SaveText(string content, string filePath)
        {
            // Check if the directory exists, create it if not
            string directoryPath = System.IO.Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Write the content to the specified text file
            File.WriteAllText(filePath, content);
        }

        private void bwBalance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (updatePreview!=null)
            {
                if (updatePreview.IsChecked == false) return;
            }
            if(originalImage.ImageSource!=null) previewImage.ImageSource = ConvertToGrayscale(originalImage.ImageSource as BitmapImage);
        }

        private void saveBitonal(object sender, RoutedEventArgs e)
        {
            if(previewImage.ImageSource==null)
            {
                MessageBox.Show("No Input Image has been selected.", "Error");
                return;
            }
            BitmapImage image = previewImage.ImageSource as BitmapImage;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Portable Bitmap (*.pbm)|*.pbm";
            if (saveFileDialog.ShowDialog() == true)
            {
                string output = $"P1\n{image.PixelWidth} {image.PixelHeight}\n";
                foreach (int i in GetRedChannel(image))
                {
                    output += i+" ";
                }
                SaveText(output.Substring(0,output.Length-2), saveFileDialog.FileName);
                Process.Start("explorer.exe", $"/select,\"{saveFileDialog.FileName}\"");
            }
        }

        private void saveTXT(object sender, RoutedEventArgs e)
        {
            if (previewImage.ImageSource == null)
            {
                MessageBox.Show("No Input Image has been selected.", "Error");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text File (*.txt)|*.txt";
            if(saveFileDialog.ShowDialog()==true)
            {
                string output = "";
                foreach (int i in GetRedChannel(previewImage.ImageSource as BitmapImage))
                {
                    output += " " + i;
                }
                SaveText(output.Substring(1), saveFileDialog.FileName);
                Process.Start("explorer.exe", $"/select,\"{saveFileDialog.FileName}\"");
            }
        }

        private void updatePreview_Checked(object sender, RoutedEventArgs e)
        {
            if(updateAutomaticallyButton!=null) updateAutomaticallyButton.Visibility = Visibility.Collapsed;
        }

        private void updatePreview_Unchecked(object sender, RoutedEventArgs e)
        {
            if (updateAutomaticallyButton != null) updateAutomaticallyButton.Visibility = Visibility.Visible;
        }

        private void updateAutomaticallyButton_Click(object sender, RoutedEventArgs e)
        {
            if (originalImage.ImageSource != null) previewImage.ImageSource = ConvertToGrayscale(originalImage.ImageSource as BitmapImage);
        }
    }
}