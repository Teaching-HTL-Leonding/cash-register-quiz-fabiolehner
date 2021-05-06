using CashRegister.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

namespace CashRegister.UICore
{
    /// <summary>

    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://localhost:5001"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            async Task LoadProducts()
            {
                var products = await HttpClient.GetFromJsonAsync<List<Product>>("api/products");
                if (products == null || products.Count == 0) return;
                foreach (var product in products) Products.Add(product);
            }
            Loaded += async (_, __) => await LoadProducts();
        }
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<ReceiptLineViewModel> Basket { get; } = new();
        public decimal TotalSum => Basket.Sum(rl => rl.TotalPrice);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnAddProduct(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is not Product selectedProduct) return;
            var product = Products.First(p => p.ID == selectedProduct.ID);

            Basket.Add(new ReceiptLineViewModel
            {
                ProductID = product.ID,
                Amount = 1,
                ProductName = product.ProductName,
                TotalPrice = product.UnitPrice
            });
            PropertyChanged?.Invoke(this, new(nameof(TotalSum)));
        }

        private async void OnCheckout(object sender, RoutedEventArgs e)
        {
            var dto = Basket.Select(b => new ReceiptLineDto
            {
                ProductID = b.ProductID,
                Amount = b.Amount
            }).ToList();
            var response = await HttpClient.PostAsJsonAsync("/api/receipts", dto);
            response.EnsureSuccessStatusCode();
            Basket.Clear();
            PropertyChanged?.Invoke(this, new(nameof(TotalSum)));
        }
    }
}
