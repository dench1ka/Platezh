using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.SqlClient;
using Platezh.Services;
using Microsoft.VisualBasic;
using System.Configuration;

namespace Platezh.Views
{
    public partial class Casher : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        private enum ViewMode { Materials, Services, Clients }
        private ViewMode currentMode = ViewMode.Materials;

        private List<MaterialPriceItem> pricesList = new List<MaterialPriceItem>();
        private List<ServiceItem> serviceList = new List<ServiceItem>();
        private List<ClientItem> clientList = new List<ClientItem>();

        private List<MaterialPriceItem> selectedMaterials = new List<MaterialPriceItem>();
        private List<ServiceItem> selectedServices = new List<ServiceItem>();

        public Casher()
        {
            InitializeComponent();
            LoadAllData();
            SwitchView(ViewMode.Materials);
        }

        private void LoadAllData()
        {
            try
            {
                LoadPrices();
                LoadServices();
                LoadClients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        #region Загрузка данных
        private void LoadPrices()
        {
            pricesList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT PriceID, MaterialPrices.MaterialID as MatId, Materials.Name as MatName, Units.Name as UnitName, 
                               PriceWithoutNds, NdsPercent, ValidFrom, ValidTo, StockAmount, 
                               Materials.IsActive as MatActive, MaterialPrices.IsActive as PriceActive 
                               FROM MaterialPrices 
                               LEFT JOIN Materials ON MaterialPrices.MaterialID = Materials.MaterialID 
                               LEFT JOIN Units ON Materials.UnitID = Units.UnitID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal price = reader.GetDecimal(reader.GetOrdinal("PriceWithoutNds"));
                        decimal percent = reader.GetDecimal(reader.GetOrdinal("NdsPercent"));
                        decimal ndsVal = Math.Round(price * (percent / 100m), 2);

                        pricesList.Add(new MaterialPriceItem
                        {
                            PriceID = reader.GetInt32(0),
                            MatId = reader.GetInt32(1),
                            MaterialsName = reader.IsDBNull(2) ? "—" : reader.GetString(2),
                            UnitName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            PriceWithoutNds = price,
                            StockAmount = reader.GetInt32(8),
                            Nds = ndsVal,
                            TotalPrice = price + ndsVal,
                            IsActiveWord = reader.GetBoolean(9) ? "Доступен" : "Недоступен"
                        });
                    }
                }
            }
        }

        private void LoadServices()
        {
            serviceList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ServiceId, Name, BasePrice, IsActive, AddMaterials FROM Services";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        serviceList.Add(new ServiceItem
                        {
                            ServiceId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            BasePrice = reader.GetDecimal(2),
                            AddMaterials = reader.GetDecimal(4),
                            TotalPrice = reader.GetDecimal(2) + reader.GetDecimal(4),
                            IsActiveWord = reader.GetBoolean(3) ? "Доступна" : "Недоступна"
                        });
                    }
                }
            }
        }

        private void LoadClients()
        {
            clientList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT id_client, fio, passport, address FROM Clients";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) clientList.Add(new ClientItem
                    {
                        id = r.GetInt32(0),
                        FullName = r.GetString(1),
                        Passport = r.IsDBNull(2) ? "—" : r.GetString(2),
                        Address = r.IsDBNull(3) ? "—" : r.GetString(3)
                    });
                }
            }
            ClientSelectBox.ItemsSource = null;
            ClientSelectBox.ItemsSource = clientList;
        }
        #endregion

        #region Интерфейс и Переключение
        private void ViewSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (sender == BtnMaterials) SwitchView(ViewMode.Materials);
            else if (sender == BtnServices) SwitchView(ViewMode.Services);
            else if (sender == BtnClients) SwitchView(ViewMode.Clients);
        }

        private void SwitchView(ViewMode mode)
        {
            currentMode = mode;
            MainGrid.Columns.Clear();

            if (mode == ViewMode.Materials)
            {
                CurrentTableTitle.Content = "Материалы";
                MainGrid.ItemsSource = pricesList;
                AddCol("Материал", "MaterialsName"); AddCol("Остаток", "StockAmount"); AddCol("Итого", "TotalPrice");
            }
            else if (mode == ViewMode.Services)
            {
                CurrentTableTitle.Content = "Услуги";
                MainGrid.ItemsSource = serviceList;
                AddCol("Название", "Name"); AddCol("Тариф", "BasePrice"); AddCol("Итого", "TotalPrice");
            }
            else
            {
                CurrentTableTitle.Content = "База клиентов";
                MainGrid.ItemsSource = clientList;
                AddCol("ФИО", "FullName"); AddCol("Паспорт", "Passport"); AddCol("Адрес", "Address");
            }
        }

        private void AddCol(string header, string binding) =>
            MainGrid.Columns.Add(new DataGridTextColumn { Header = header, Binding = new Binding(binding), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = SearchBox.Text.ToLower();
            if (currentMode == ViewMode.Materials) MainGrid.ItemsSource = pricesList.Where(x => x.MaterialsName.ToLower().Contains(t)).ToList();
            else if (currentMode == ViewMode.Services) MainGrid.ItemsSource = serviceList.Where(x => x.Name.ToLower().Contains(t)).ToList();
            else MainGrid.ItemsSource = clientList.Where(x => x.FullName.ToLower().Contains(t)).ToList();
        }
        #endregion

        private void ClientSelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientSelectBox.SelectedItem is ClientItem client)
            {
                ClientNameBox.Text = client.FullName;
                PassportDataBox.Text = client.Passport;
                AddressBox.Text = client.Address;
            }
        }

        private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentMode == ViewMode.Clients && MainGrid.SelectedItem is ClientItem client)
                ClientSelectBox.SelectedItem = client;
        }

        private void Grid_Click(object sender, RoutedEventArgs e) => SelectRecord();
        private void Addbtnclicked(object sender, RoutedEventArgs e) => SelectRecord();

        private void SelectRecord()
        {
            if (currentMode == ViewMode.Materials && MainGrid.SelectedItem is MaterialPriceItem mat)
            {
                string input = Interaction.InputBox($"Кол-во для '{mat.MaterialsName}':", "Добавление", "1");
                if (int.TryParse(input, out int qty) && qty > 0 && qty <= mat.StockAmount)
                {
                    selectedMaterials.Add(mat);
                    SelectedItemsListBox.Items.Add($"Мат: {mat.MaterialsName} x{qty} | {mat.TotalPrice * qty} BYN");
                }
            }
            else if (currentMode == ViewMode.Services && MainGrid.SelectedItem is ServiceItem ser)
            {
                selectedServices.Add(ser);
                SelectedItemsListBox.Items.Add($"Усл: {ser.Name} | {ser.TotalPrice} BYN");
            }
        }

        // Заглушки для методов, чтобы код скомпилировался
        private void Deletebtn_Click(object sender, RoutedEventArgs e) { if (SelectedItemsListBox.SelectedIndex != -1) SelectedItemsListBox.Items.RemoveAt(SelectedItemsListBox.SelectedIndex); }
        private void SelectedItemsListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { }
        private void GenerateContract_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Договор сформирован!"); }
        private void SaveClient_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Данные сохранены"); LoadClients(); }
        private void Button_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void ShowContracts(object sender, RoutedEventArgs e) { }
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) { }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

        #region Классы данных
        public class MaterialPriceItem
        {
            public int PriceID { get; set; }
            public int MatId { get; set; }
            public string MaterialsName { get; set; } = "";
            public string UnitName { get; set; } = "";
            public decimal PriceWithoutNds { get; set; }
            public int StockAmount { get; set; }
            public decimal Nds { get; set; }
            public decimal TotalPrice { get; set; }
            public string IsActiveWord { get; set; } = "";
        }

        public class ServiceItem
        {
            public int ServiceId { get; set; }
            public string Name { get; set; } = "";
            public decimal BasePrice { get; set; }
            public decimal AddMaterials { get; set; }
            public decimal TotalPrice { get; set; }
            public string IsActiveWord { get; set; } = "";
        }

        public class ClientItem
        {
            public int id { get; set; }
            public string FullName { get; set; } = "";
            public string Passport { get; set; } = "";
            public string Address { get; set; } = "";
        }
        #endregion
    }
}