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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        public List<WerksItem> Items { get; set; }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
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

            try
            {
                using (StreamWriter writerCSV = new StreamWriter(filePathCSV, true))
                {
                    foreach (var item in Items)
                    {
                        writerCSV.WriteLine($"{item.ItemName},{item.CompletionCount},{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
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
    }

    // Class to represent items in the DataGrid
    public class WerksItem
    {
        public string ItemName { get; set; }
        public int CompletionCount { get; set; }
    }
}
