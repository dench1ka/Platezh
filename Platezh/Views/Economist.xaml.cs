using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;

namespace Platezh.Views
{
    public partial class Economist : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;
        private bool materialsVisible;
        private List<MaterialItem> materialList = new List<MaterialItem>();
        private List<ServiceItem> serviceList = new List<ServiceItem>();

        public Economist()
        {
            InitializeComponent();
            LoadMaterials();
            LoadServices();
            TypeCategories();
            materialsVisible = true;
            NdsChoice.SelectedItem = 0;
            PriceChoice.SelectedItem = 0;
        }

        private void TypeCategories_Click(object sender, RoutedEventArgs e)
        {
            TypeCategories();
        }

        private void TypeCategories()
        {
            materialsVisible = !materialsVisible;
            if (materialsVisible)
            {
                Services.Visibility = Visibility.Collapsed;
                ServicesInputPanel.Visibility = Visibility.Collapsed;

                Materials.Visibility = Visibility.Visible;
                MaterialsInputPanel.Visibility = Visibility.Visible;
                TypeCategoriesbtn.Content = "Услуги";
                NameOfCategory.Content = "Материалы";
            }
            else
            {
                Materials.Visibility = Visibility.Collapsed;
                MaterialsInputPanel.Visibility = Visibility.Collapsed;

                Services.Visibility = Visibility.Visible;
                ServicesInputPanel.Visibility = Visibility.Visible;
                TypeCategoriesbtn.Content = "Материалы";
                NameOfCategory.Content = "Услуги";
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();

            if (materialsVisible)
            {
                Materials.ItemsSource = materialList
                    .Where(m => m.name.ToLower().Contains(searchText))
                    .ToList();
            }
            else
            {
                Services.ItemsSource = serviceList
                    .Where(s => s.Name.ToLower().Contains(searchText))
                    .ToList();
            }
        }

        private readonly Dictionary<string, string> columnHeadersMaterials = new Dictionary<string, string>
        {
            { "id", "ID" },
            { "name", "Название" },
            { "priceWithoutNds", "Цена BYN" },
            { "stockCount", "Остаток" },
            { "nds", "НДС BYN" },
            { "nds_percent", "НДС %" },
            { "totalPrice", "Итоговая стоимость" },
        };

        private readonly Dictionary<string, string> columnHeadersServices = new Dictionary<string, string>
        {
            { "Id", "ID" },
            { "Name", "Название" },
            { "tarif", "Тариф BYN" },
            { "additionalMaterialsPrice", "Доп. материалы BYN" },
            { "totalPrice", "Итоговая стоимость" }
        };
  
        private void LoadMaterials()
        {
            materialList.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id_materials, name, price_without_nds, stock, nds, nds_percent, total_price FROM Materials", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materialList.Add(new MaterialItem
                        {
                            id = reader.GetInt32(reader.GetOrdinal("id_materials")),
                            name = reader.GetString(reader.GetOrdinal("name")),
                            priceWithoutNds = reader.GetDecimal(reader.GetOrdinal("price_without_nds")),
                            stockCount = reader.GetInt32(reader.GetOrdinal("stock")),
                            nds = reader.GetDecimal(reader.GetOrdinal("nds")),
                            nds_percent = reader.GetDecimal(reader.GetOrdinal("nds_percent")),
                            totalPrice = reader.GetDecimal(reader.GetOrdinal("total_price"))
                        });
                    }
                }
            }

            // Присваиваем данные
            Materials.ItemsSource = materialList;

            Materials.AutoGenerateColumns = false;
            Materials.Columns.Clear();

            foreach (var column in columnHeadersMaterials)
            {
                Materials.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Value, // Русское название из словаря
                    Binding = new Binding(column.Key), // Привязка к свойству
                    Width = DataGridLength.Auto
                });
            }
        }

        private void LoadServices()
        {
            serviceList.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id_services, name, tarif, additonal_materials_price, total_price FROM Services", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        serviceList.Add(new ServiceItem
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id_services")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            additionalMaterialsPrice = reader.GetDecimal(reader.GetOrdinal("additonal_materials_price")),
                            tarif = reader.GetDecimal(reader.GetOrdinal("tarif")),
                            totalPrice = reader.GetDecimal(reader.GetOrdinal("total_price"))
                        });
                    }
                }
            }

            // Присваиваем данные
            Services.ItemsSource = serviceList;

            Services.AutoGenerateColumns = false;
            Services.Columns.Clear();

            foreach (var column in columnHeadersServices)
            {
                Services.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Value, // Русское название из словаря
                    Binding = new Binding(column.Key), // Привязка к свойству
                    Width = DataGridLength.Auto
                });
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (materialsVisible)
                {
                    string name = NameBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string ndsText = NdsBox.Text?.Trim();

                    if (string.IsNullOrEmpty(ndsText) && materialsVisible == true)
                    {
                        MessageBox.Show("Введите НДС!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    ndsText = ndsText.Replace('.', ',');

                    if (!decimal.TryParse(ndsText, NumberStyles.Number, new CultureInfo("ru-RU"), out decimal nds))
                    {
                        MessageBox.Show("Введите корректный НДС (например, 13,45)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string priceText = PriceBox.Text?.Trim();

                    if (string.IsNullOrEmpty(priceText))
                    {
                        MessageBox.Show("Введите цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    priceText = priceText.Replace('.', ',');

                    if (!decimal.TryParse(priceText, NumberStyles.Number, new CultureInfo("ru-RU"), out decimal price))
                    {
                        MessageBox.Show("Введите корректную цену (например, 123,45)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                   
                    decimal ndsByn = 0;
                    decimal ndsPercent = 0;
                    decimal totalPrice = 0;

                    if (NdsChoice.SelectedIndex == -1 || PriceChoice.SelectedIndex == -1)
                    {
                        MessageBox.Show("Выберите, с НДС цена или без");
                        return;
                    }

                    if (PriceChoice.SelectedIndex == 0) //total price without nds (input)
                    {
                        if (NdsChoice.SelectedIndex == 0)//%
                        {
                            ndsByn = price * (nds / 100);
                            ndsPercent = nds;
                        }
                        else if (NdsChoice.SelectedIndex == 1) //byn
                        {
                            ndsByn = nds;
                            ndsPercent = Math.Round(price/ndsByn, 2);
                        }
                        totalPrice = price + ndsByn;
                    }
                    else if (PriceChoice.SelectedIndex == 1) //total price with nds (input)
                    {                        
                        totalPrice = price;
                        if (NdsChoice.SelectedIndex == 0) //%
                        {
                            price = price / (1 + (nds / 100));
                            ndsByn = totalPrice - price;
                            ndsPercent = nds;
                        }
                        else if (NdsChoice.SelectedIndex == 1) //byn
                        {
                            price = price - nds;
                            ndsByn = nds;
                            ndsPercent = Math.Round(price / ndsByn, 2);
                        }
                    }


                    //id
                    int id = Convert.ToInt32(idBox.Text);
                    int stock = Convert.ToInt32(StockBox.Text);

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "INSERT INTO Materials (id_materials, name, price_without_nds, stock, nds, nds_percent, total_price)  VALUES (@id, @name, @priceWithoutNds, @stock, @nds, @ndspercent, @totalPrice)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@priceWithoutNds", price);
                            cmd.Parameters.AddWithValue("@stock", stock);
                            cmd.Parameters.AddWithValue("@nds", ndsByn);
                            cmd.Parameters.AddWithValue("@ndspercent", ndsPercent);
                            cmd.Parameters.AddWithValue("totalPrice", totalPrice);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    NameBox.Clear();
                    PriceBox.Clear();
                    StockBox.Clear();
                    NdsBox.Clear();
                    idBox.Clear();
                }
                else
                {
                    //id
                    int id = Convert.ToInt32(idBox.Text);
                    //название
                    string name = NameBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    //тариф
                    string tarifText = TarifBox.Text?.Trim();

                    if (string.IsNullOrEmpty(tarifText))
                    {
                        MessageBox.Show("Введите тариф!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    tarifText = tarifText.Replace('.', ',');

                    if (!decimal.TryParse(tarifText, NumberStyles.Number, new CultureInfo("ru-RU"), out decimal tarif))
                    {
                        MessageBox.Show("Введите корректный тариф (например, 123,45)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    //Цена за доп материалы
                    string AdditionalMatPriceText = AdditionalMaterialsPriceBox.Text?.Trim();

                    if (string.IsNullOrEmpty(AdditionalMatPriceText))
                    {
                        MessageBox.Show("Введите цену доп материалов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    AdditionalMatPriceText = AdditionalMatPriceText.Replace('.', ',');

                    if (!decimal.TryParse(AdditionalMatPriceText, NumberStyles.Number, new CultureInfo("ru-RU"), out decimal AdditionalMatPrice))
                    {
                        MessageBox.Show("Введите корректную цену материалов (например, 123,45)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // итоговая стоимость
                    decimal totalPrice = tarif + AdditionalMatPrice;

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "INSERT INTO Services (id_services, name, tarif, additonal_materials_price, total_price) VALUES (@id, @name, @tarif, @addMatPri, @totalPrice)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@tarif", tarif);
                            cmd.Parameters.AddWithValue("addMatPri", AdditionalMatPrice);
                            cmd.Parameters.AddWithValue("totalPrice", totalPrice);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    idBox.Clear();
                    NameBox.Clear();
                    PriceBox.Clear();
                    TarifBox.Clear();
                    AdditionalMaterialsPriceBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Update_Table();
        }

        private void Update_Click(object sender, EventArgs e)
        {
            Update_Table();
        }

        public void Update_Table()
        {
            try
            {
                if (materialsVisible)
                {
                    LoadMaterials();
                    Materials.Items.Refresh();
                }
                else
                {
                    LoadServices();
                    Services.Items.Refresh();
                }

                MessageBox.Show("Таблица обновлена.", "Обновление успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"SQL ошибка: {sqlEx.Message}", "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (materialsVisible && Materials.SelectedItem is MaterialItem material)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Materials WHERE id_materials=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", material.id);
                        cmd.ExecuteNonQuery();
                    }
                }
                Update_Table();
            }
            else if (!materialsVisible && Services.SelectedItem is ServiceItem service)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Services WHERE id_services=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", service.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                Update_Table();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if ((Materials.SelectedItem != null) || (Services.SelectedItem != null))
            {
                EditRecord();
            } else
            {
                MessageBox.Show("Выберите в таблице строку для редактирования!");
            }

        }

        private void Grid_Click(object sender, RoutedEventArgs e)
        {
            EditRecord();
        }

        private void EditRecord()
        {
            EditWindow editWindow = new EditWindow(this);
            editWindow.Show();
            if (materialsVisible && Materials.SelectedItem is MaterialItem material)
            {
                editWindow.materialsVisible = true;
                editWindow.SectionName.Content = "Раздел: Материалы";
                editWindow.UpdateVisibility();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Materials WHERE id_materials=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", material.id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                editWindow.IdBox.Text = reader["id_materials"].ToString();
                                editWindow.NameBox.Text = reader["name"].ToString();
                                editWindow.NameItem.Content = "Материал: " + reader["name"].ToString(); //label
                                editWindow.StockBox.Text = reader["stock"].ToString();
                                editWindow.PriceWithoutNdsBox.Text = reader["price_without_nds"].ToString();
                                editWindow.NdsBox.Text = reader["nds_percent"].ToString();
                                editWindow.TotalPriceBox.Text = reader["total_price"].ToString();
                            }
                        }
                    }
                }
            }
            else if (!materialsVisible && Services.SelectedItem is ServiceItem service)
            {
                editWindow.materialsVisible = false;
                editWindow.SectionName.Content = "Раздел: Услуги";
                editWindow.UpdateVisibility();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Services WHERE id_services=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", service.Id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                editWindow.IdBox.Text = reader["id_services"].ToString();
                                editWindow.NameBox.Text = reader["name"].ToString();
                                editWindow.NameItem.Content = "Услуга: " + reader["name"].ToString(); //заполнение label
                                editWindow.TarifBox.Text = reader["tarif"].ToString();
                                editWindow.AdditionalMaterialsPriceBox.Text = reader["additonal_materials_price"].ToString();
                                editWindow.TotalPriceBox.Text = reader["total_price"].ToString();
                            }
                        }
                    }
                }
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }

    public class MaterialItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public int stockCount { get; set; }
        public decimal priceWithoutNds { get; set; }
        public decimal nds { get; set; }
        public decimal nds_percent { get; set; }
        public decimal totalPrice { get; set; }
    }

    public class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal tarif { get; set; }
        public decimal additionalMaterialsPrice { get; set; }
        public decimal totalPrice { get; set; }
    }
}
