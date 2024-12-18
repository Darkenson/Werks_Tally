using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tesseract;

namespace Werks_Tally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string configFilePath = AppDomain.CurrentDomain.BaseDirectory + "config.ini";
            if (!File.Exists(configFilePath))
            {
                File.WriteAllText(configFilePath, "Central_CSV_Path=" + Environment.NewLine + "OCR_Language=eng");
            }
            else
            {
                try
                {
                    string[] configLines = File.ReadAllLines(configFilePath);
                    centralCSVPath = configLines[0].Split('=')[1];
                    ocrLang = configLines[1].Split('=')[1];
                }
                catch { }
            }
            //// Enable drag and drop
            //imgPastedImage.AllowDrop = true;
            //imgPastedImage.PreviewDragOver += Image_PreviewDragOver;
            //imgPastedImage.Drop += Image_Drop;

            SetupImagePreviewPopup();
        }
        string ocrLang = "eng";

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl+V paste
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                PasteImage();
            }
        }

        private void Image_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    LoadImage(files[0]);
                }
            }
        }

        private void PasteImage()
        {
            // Check if clipboard contains an image
            if (Clipboard.ContainsImage())
            {
                LoadBitmapFromClipboard();
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files.Count > 0)
                {
                    LoadImage(files[0]);
                }
            }
        }

        private void LoadBitmapFromClipboard()
        {
            BitmapSource clipboardImage = Clipboard.GetImage();
            if (clipboardImage != null)
            {
                imgPastedImage.Source = clipboardImage;
                PerformOcr(clipboardImage);
            }
        }

        private Popup largeImagePopup;
        private Image largePreviewImage;

        private void SetupImagePreviewPopup()
        {
            // Create the popup
            largeImagePopup = new Popup
            {
                PopupAnimation = PopupAnimation.Fade,
                AllowsTransparency = true,
                PlacementTarget = imgPastedImage,
                Placement = PlacementMode.Mouse
            };

            // Create large preview image
            largePreviewImage = new Image
            {
                MaxWidth = 600,
                MaxHeight = 600,
                Stretch = System.Windows.Media.Stretch.Uniform
            };

            // Border to add some styling to the popup
            Border popupBorder = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                Child = largePreviewImage
            };

            largeImagePopup.Child = popupBorder;

            //// Add mouse events to trigger popup
            //imgPastedImage.MouseEnter += imgPastedImage_MouseEnter;
            //imgPastedImage.MouseLeave += imgPastedImage_MouseLeave;
        }
        private void imgPastedImage_MouseEnter(object sender, MouseEventArgs e)
        {
            // Only show popup if an image exists
            if (imgPastedImage.Source == null)
                return;
            // Set the large preview source to the same image
            largePreviewImage.Source = imgPastedImage.Source;
            largeImagePopup.IsOpen = true;
        }

        private void imgPastedImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (largePreviewImage.Source == null)
                return;
            largeImagePopup.IsOpen = false;
        }

        private void LoadImage(string filePath)
        {
            BitmapImage originalBitmap = new BitmapImage(new Uri(filePath, UriKind.Absolute));

            // Check if scaling is necessary
            if (originalBitmap.PixelWidth > 600 || originalBitmap.PixelHeight > 600)
            {
                // Calculate scaling factor
                double scale = Math.Min(600.0 / originalBitmap.PixelWidth, 600.0 / originalBitmap.PixelHeight);

                // Create a new scaled bitmap
                TransformedBitmap scaledBitmap = new TransformedBitmap(
                    originalBitmap,
                    new ScaleTransform(scale, scale)
                );

                imgPastedImage.Source = scaledBitmap;
                PerformOcr(scaledBitmap);
            }
            else
            {
                // If no scaling is needed, use the original bitmap
                imgPastedImage.Source = originalBitmap;
                PerformOcr(originalBitmap);
            }
        }

        private void PerformOcr(BitmapSource image)
        {
            try
            {
                // Save the image to a temporary file
                string tempImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_ocr_image.png");

                using (var fileStream = new FileStream(tempImagePath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                }

                // Perform OCR
                // IMPORTANT: Ensure you have 'eng.traineddata' 
                // in your application directory or bin/Debug/net folder
                if (!File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ocrLang + ".traineddata")))
                {
                    MessageBox.Show($"The OCR language module you have configured ({ocrLang}) could not be found next to the executable. Please download it from https://github.com/tesseract-ocr/tessdata or set it back to \"eng\" in the config.ini file.");
                    return;
                }

                using (var engine = new TesseractEngine(AppDomain.CurrentDomain.BaseDirectory, ocrLang, EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(tempImagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            // Extract text
                            string extractedText = page.GetText();
                            WerksReader.Text += extractedText;

                            // Optional: Get confidence levels
                            //var confidence = page.GetMeanConfidence();
                            //txtConfidence.Text = $"Confidence: {confidence:P2}";
                        }
                    }
                }

                // Clean up temporary file
                //File.Delete(tempImagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OCR Error: {ex.Message}");
            }
        }

        public static string centralCSVPath = "";

        public List<WerksItem> Items { get; set; }

        private List<string> ProcessWerksInput(string input)
        {
            var completedItems = new List<string>();

            // Regex to match "Build completed" lines and extract the item name
            var regex = new Regex(@"Build completed \(([^()]*(?:\([^()]*\)[^()]*)*)\)");

            foreach (Match match in regex.Matches(input))
            {
                if (match.Groups.Count > 1)
                {
                    completedItems.Add(match.Groups[1].Value.Trim());
                }
            }

            return completedItems;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Items == null) return;
            if (Items.Count == 0) return;

            string outputPath = AppDomain.CurrentDomain.BaseDirectory;

            string filePathText = System.IO.Path.Combine(outputPath, "Werks.txt");
            string filePathCSV = System.IO.Path.Combine(outputPath, "AllWerks.csv");
            if (!File.Exists(filePathCSV))
            {
                File.WriteAllText(filePathCSV, "ItemName,ItemCount,Date" + Environment.NewLine);
            }

            if (centralCSVPath != "" && !File.Exists(centralCSVPath))
            {
                File.WriteAllText(centralCSVPath, "ItemName,ItemCount,Date" + Environment.NewLine);
            }

            try
            {
                using (StreamWriter writerCSV = new StreamWriter(filePathCSV, true))
                {
                    foreach (var item in Items)
                    {
                        writerCSV.WriteLine($"{item.ItemName},{item.CompletionCount},{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                if (centralCSVPath != "")
                {
                    using (StreamWriter writerCSV = new StreamWriter(centralCSVPath, true))
                    {
                        foreach (var item in Items)
                        {
                            writerCSV.WriteLine($"{item.ItemName},{item.CompletionCount},{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                }

                using (StreamWriter writer = new StreamWriter(filePathText, true))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine();
                    foreach (var item in Items)
                    {
                        writer.WriteLine($"{item.CompletionCount}x {item.ItemName}");
                    }
                    writer.WriteLine("------------------------------------------");
                    writer.WriteLine();
                }
            }
            catch (Exception)
            {
                MessageBox.Show($"Cannot save the output files. {Environment.NewLine}Do you have the CSV file open in Excel?{Environment.NewLine}If so, close it and click again.", "Cannot save output", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = filePathText, UseShellExecute = true });
            }
            catch
            { }
        }

        private void WerksReader_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                PasteImage();
            }
        }

        private void WerksReader_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = WerksReader.Text;
            var completedItems = ProcessWerksInput(input);

            Items = completedItems
                // Remove empty or whitespace entries
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .GroupBy(item => item)
                .Select(g => new WerksItem
                {
                    ItemName = g.Key,
                    CompletionCount = g.Count()
                })
                .ToList();

            WerksList.ItemsSource = Items;
            //WerksReader.Text = "";
        }
    }

    // Class to represent items in the DataGrid
    public class WerksItem
    {
        public string ItemName { get; set; }
        public int CompletionCount { get; set; }
    }
}
