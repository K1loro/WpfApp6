using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Dapper;
using ZXing;

namespace YourNamespace
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<Product> products;
        public List<Product> Products
        {
            get { return products; }
            set
            {
                products = value;
                OnPropertyChanged();
            }
        }

        private Product selectedProduct;
        public Product SelectedProduct
        {
            get { return selectedProduct; }
            set
            {
                selectedProduct = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            SQLiteDataAccess.InitializeDatabase();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void LoadProducts()
        {
            Products = SQLiteDataAccess.LoadProducts();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Product newProduct = new Product();

            newProduct.Id = IdTextBox.Text;
            newProduct.Name = NameTextBox.Text;
            newProduct.Price = Convert.ToDouble(PriceTextBox.Text);
            newProduct.Description = DescriptionTextBox.Text;

            SQLiteDataAccess.SaveProduct(newProduct);

            newProduct.GenerateQRCode();
            Products.Add(newProduct);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProduct != null)
            {
                SQLiteDataAccess.UpdateProduct(SelectedProduct);
                SelectedProduct.GenerateQRCode();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProduct != null)
            {
                SQLiteDataAccess.DeleteProduct(SelectedProduct);
                Products.Remove(SelectedProduct);
            }
        }

        private void ProductsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedProduct = ProductsListBox.SelectedItem as Product;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedProduct != null)
            {
                SelectedProduct.GenerateQRCode();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public BitmapImage QRCodeImage { get; set; }

        public void GenerateQRCode()
        {
            var writer = new BarcodeWriter<BitmapSource>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 200,
                    Height = 200
                }
            };

            var qrCodeBitmap = writer.Write(Id);
            var qrCodeImage = new BitmapImage();

            using (var memoryStream = new MemoryStream())
            {
                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(qrCodeBitmap));
                bitmapEncoder.Save(memoryStream);
                memoryStream.Position = 0;
                qrCodeImage.BeginInit();
                qrCodeImage.CacheOption = BitmapCacheOption.OnLoad;
                qrCodeImage.StreamSource = memoryStream;
                qrCodeImage.EndInit();
            }

            QRCodeImage = qrCodeImage;
        }
    }

    public class SQLiteDataAccess
    {
        private const string connectionString = "Data Source=C:\\Users\\79178\\Desktop\\SQLite\\Products.db;Version=3;";

        public static void InitializeDatabase()
        {
            if (!File.Exists(connectionString))
            {
                SQLiteConnection.CreateFile(connectionString);

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string createTableQuery = "CREATE TABLE IF NOT EXISTS Products (Id TEXT PRIMARY KEY, Name TEXT, Price REAL, Description TEXT)";
                    connection.Execute(createTableQuery);
                }
            }
        }

        public static List<Product> LoadProducts()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string selectQuery = "SELECT * FROM Products";
                var products = connection.Query<Product>(selectQuery);

                return products.AsList();
            }
        }

        public static void SaveProduct(Product product)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string insertQuery = "INSERT INTO Products (Id, Name, Price, Description) VALUES (@Id, @Name, @Price, @Description)";
                connection.Execute(insertQuery, product);
            }
        }

        public static void UpdateProduct(Product product)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string updateQuery = "UPDATE Products SET Name = @Name, Price = @Price, Description = @Description WHERE Id = @Id";
                connection.Execute(updateQuery, product);
            }
        }

        public static void DeleteProduct(Product product)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string deleteQuery = "DELETE FROM Products WHERE Id = @Id";
                connection.Execute(deleteQuery, product);
            }
        }
    }
}

