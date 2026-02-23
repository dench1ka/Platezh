using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.SqlClient;

namespace Platezh.Views
{
    // Перечисление режимов таблицы
    public enum TableMode
    {
        Prices,     // Цены и склад
        Reference,  // Справочник материалов
        Services,   // Услуги
        Units       // Единицы измерения
    }

    public partial class Economist : Window
    {
        // Строка подключения из App.config
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        // Текущий режим работы
        private TableMode currentMode;

        // Списки данных для каждого режима
        private List<MaterialPriceItem> pricesList = new List<MaterialPriceItem>();
        private List<MaterialReferenceItem> referenceList = new List<MaterialReferenceItem>();
        private List<ServiceItem> serviceList = new List<ServiceItem>();
        private List<UnitsItem> unitsList = new List<UnitsItem>();
        private List<MaterialReferenceItem> materialSelectionList = new List<MaterialReferenceItem>();

        public Economist()
        {
            InitializeComponent();

            // Предварительная загрузка справочников для ComboBox
            LoadUnits();

            // Запускаем режим по умолчанию (Цены)
            SwitchMode(TableMode.Prices);
        }

        // --- ЛОГИКА ПЕРЕКЛЮЧЕНИЯ РЕЖИМОВ ---

        private void ViewSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (sender == BtnPrices) SwitchMode(TableMode.Prices);
            else if (sender == BtnReference) SwitchMode(TableMode.Reference);
            else if (sender == BtnServices) SwitchMode(TableMode.Services);
            else if (sender == BtnUnits) SwitchMode(TableMode.Units);
        }

        private void SwitchMode(TableMode mode)
        {
            currentMode = mode;

            // Очистка таблицы
            MainGrid.ItemsSource = null;
            MainGrid.Columns.Clear();

            // Сброс видимости всех панелей ввода
            MaterialsInputPanel.Visibility = Visibility.Collapsed;
            ServicesInputPanel.Visibility = Visibility.Collapsed;
            ReferenceInputPanel.Visibility = Visibility.Collapsed;
            UnitsInputPanel.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case TableMode.Prices:
                    CurrentTableTitle.Content = "Цены и остатки";
                    MaterialsInputPanel.Visibility = Visibility.Visible;
                    LoadMaterialsForCombobox();
                    LoadPrices();
                    break;

                case TableMode.Reference:
                    CurrentTableTitle.Content = "Справочник материалов";
                    ReferenceInputPanel.Visibility = Visibility.Visible;
                    LoadReference();
                    break;

                case TableMode.Services:
                    CurrentTableTitle.Content = "Услуги";
                    ServicesInputPanel.Visibility = Visibility.Visible;
                    LoadServices();
                    break;

                case TableMode.Units:
                    CurrentTableTitle.Content = "Единицы измерения";
                    UnitsInputPanel.Visibility = Visibility.Visible;
                    LoadUnits();
                    break;
            }
        }

        // --- МЕТОДЫ ЗАГРУЗКИ ДАННЫХ (SQL) ---

        private void LoadUnits()
        {
            unitsList.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT UnitID, Name FROM Units";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            unitsList.Add(new UnitsItem
                            {
                                UnitID = reader.GetInt32(reader.GetOrdinal("UnitID")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }
                // Привязка к ComboBox в панели материалов
                UnitChoice.ItemsSource = null;
                UnitChoice.ItemsSource = unitsList;
                UnitChoice.DisplayMemberPath = "Name";
                UnitChoice.SelectedValuePath = "UnitID";

                if (currentMode == TableMode.Units)
                {
                    CreateColumns(new Dictionary<string, string> {
                        {"UnitID", "ID"}, {"Name", "Название единицы измерения"}
                    });
                    MainGrid.ItemsSource = unitsList.ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка Units: " + ex.Message); }
        }

        private void LoadPrices()
        {
            pricesList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT PriceID, MaterialPrices.MaterialID as MatId, Materials.Name as MatName, Units.Name as UnitName, 
                               PriceWithoutNds, NdsPercent, ValidFrom, ValidTo, StockAmount, 
                               Materials.IsActive as MatActive,
                               MaterialPrices.IsActive as PriceActive 
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

                        bool isMatActive = reader.GetBoolean(reader.GetOrdinal("MatActive"));
                        bool isPriceActive = reader.GetBoolean(reader.GetOrdinal("PriceActive"));

                        pricesList.Add(new MaterialPriceItem
                        {
                            PriceID = reader.GetInt32(reader.GetOrdinal("PriceID")),
                            MatId = reader.GetInt32(reader.GetOrdinal("MatId")),
                            MaterialsName = reader.IsDBNull(reader.GetOrdinal("MatName")) ? "—" : reader.GetString(reader.GetOrdinal("MatName")),
                            UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? "" : reader.GetString(reader.GetOrdinal("UnitName")),
                            PriceWithoutNds = price,
                            NdsPercent = percent,
                            ValidFrom = reader.GetDateTime(reader.GetOrdinal("ValidFrom")),
                            ValidTo = reader.GetDateTime(reader.GetOrdinal("ValidTo")),
                            StockAmount = reader.GetInt32(reader.GetOrdinal("StockAmount")),
                            Nds = ndsVal,
                            TotalPrice = price + ndsVal,
                            IsActiveMat = isMatActive,
                            IsActiveWord = isMatActive ? "Доступен" : "Недоступен",
                            IsActivePrice = isPriceActive,
                            IsActivePriceWord = isPriceActive ? "Доступен" : "Недоступен",
                        });
                    }
                }
            }

            CreateColumns(new Dictionary<string, string> {
                {"PriceID", "ID Цены"}, {"MaterialsName", "Материал"}, {"MatId","ID материала" }, {"StockAmount", "Остаток"},
                {"UnitName", "Ед."}, {"PriceWithoutNds", "Цена"}, {"Nds", "НДС"},
                {"TotalPrice", "Итого"}, {"ValidFrom", "Дата с"}, {"ValidTo", "Дата до"},
                {"IsActiveWord", "Статус материала"}, {"IsActivePriceWord", "Статус цены"}
            });

            MainGrid.ItemsSource = pricesList.ToList();
        }

        private void LoadReference()
        {
            referenceList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT MaterialID, m.Name as mName, u.Name as uName, IsActive FROM Materials as m LEFT JOIN Units as u ON u.unitID = m.UnitID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                        referenceList.Add(new MaterialReferenceItem
                        {
                            MaterialID = reader.GetInt32(reader.GetOrdinal("MaterialID")),
                            Name = reader.GetString(reader.GetOrdinal("mName")),
                            UnitName = reader.IsDBNull(reader.GetOrdinal("uName")) ? "" : reader.GetString(reader.GetOrdinal("uName")),
                            IsActive = isActive,
                            IsActiveWord = isActive ? "Активен" : "Неактивен"
                        });
                    }
                }
            }

            CreateColumns(new Dictionary<string, string> {
                {"MaterialID", "ID Мат."}, {"Name", "Название материала"}, {"UnitName", "Ед. измерения"}, {"IsActiveWord", "Статус"}
            });

            MainGrid.ItemsSource = referenceList.ToList();
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
                        decimal basePrice = reader.GetDecimal(reader.GetOrdinal("BasePrice"));
                        decimal addPrice = reader.GetDecimal(reader.GetOrdinal("AddMaterials"));
                        bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

                        serviceList.Add(new ServiceItem
                        {
                            ServiceId = reader.GetInt32(reader.GetOrdinal("ServiceId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            BasePrice = basePrice,
                            AddMaterials = addPrice,
                            TotalPrice = basePrice + addPrice,
                            IsActive = isActive,
                            IsActiveWord = isActive ? "Доступна" : "Недоступна"
                        });
                    }
                }
            }

            CreateColumns(new Dictionary<string, string> {
                {"ServiceId", "ID"}, {"Name", "Название"}, {"BasePrice", "Тариф"},
                {"AddMaterials", "Доп. мат."}, {"TotalPrice", "Итого"}, {"IsActiveWord", "Статус"}
            });

            MainGrid.ItemsSource = serviceList.ToList();
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        private void CreateColumns(Dictionary<string, string> headers)
        {
            MainGrid.Columns.Clear();
            foreach (var h in headers)
            {
                var col = new DataGridTextColumn
                {
                    Header = h.Value,
                    Binding = new Binding(h.Key),
                    Width = DataGridLength.Auto
                };
                if (h.Key.Contains("Valid"))
                    col.Binding.StringFormat = "dd.MM.yyyy";

                MainGrid.Columns.Add(col);
            }
        }

        private void LoadMaterialsForCombobox()
        {
            materialSelectionList.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaterialID, Name FROM Materials WHERE IsActive = 1 ORDER BY Name";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            materialSelectionList.Add(new MaterialReferenceItem
                            {
                                MaterialID = reader.GetInt32(reader.GetOrdinal("MaterialID")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }
                MaterialSelectBox.ItemsSource = null;
                MaterialSelectBox.ItemsSource = materialSelectionList;
                MaterialSelectBox.DisplayMemberPath = "Name";
                MaterialSelectBox.SelectedValuePath = "MaterialID";
            }
            catch (Exception ex) { MessageBox.Show("Ошибка MaterialCombo: " + ex.Message); }
        }

        // --- ОБРАБОТЧИКИ КНОПОК CRUD ---

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "";
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    if (currentMode == TableMode.Prices)
                    {
                        if (MaterialSelectBox.SelectedValue == null) { MessageBox.Show("Выберите материал!"); return; }
                        query = @"INSERT INTO MaterialPrices (MaterialID, PriceWithoutNds, NdsPercent, ValidFrom, ValidTo, StockAmount, IsActive) 
                                  VALUES (@matId, @price, @nds, @from, @to, @stock, @isActive)";

                        cmd.Parameters.AddWithValue("@matId", (int)MaterialSelectBox.SelectedValue);
                        cmd.Parameters.AddWithValue("@price", decimal.Parse(PriceBox.Text));
                        cmd.Parameters.AddWithValue("@nds", decimal.Parse(NdsBox.Text));
                        cmd.Parameters.AddWithValue("@from", DateFrom.SelectedDate ?? DateTime.Now);
                        cmd.Parameters.AddWithValue("@to", DateTo.SelectedDate ?? DateTime.Now.AddYears(1));
                        cmd.Parameters.AddWithValue("@stock", int.Parse(StockBox.Text));
                        cmd.Parameters.AddWithValue("@isActive", PriceIsActiveCheckBox.IsChecked ?? true);
                    }
                    else if (currentMode == TableMode.Reference)
                    {
                        query = "INSERT INTO Materials (Name, UnitID, IsActive) VALUES (@name, @unit, @active)";
                        cmd.Parameters.AddWithValue("@name", UnitNameBox.Text);
                        cmd.Parameters.AddWithValue("@unit", UnitChoice.SelectedValue);
                        cmd.Parameters.AddWithValue("@active", IsActiveCheckBox.IsChecked ?? true);
                    }
                    else if (currentMode == TableMode.Services)
                    {
                        query = "INSERT INTO Services (ServiceId, Name, BasePrice, AddMaterials, IsActive) VALUES (@sid, @name, @base, @add, @active)";
                        cmd.Parameters.AddWithValue("@sid", int.Parse(TarifIdBox.Text));
                        cmd.Parameters.AddWithValue("@name", TarifNameBox.Text);
                        cmd.Parameters.AddWithValue("@base", decimal.Parse(TarifBox.Text));
                        cmd.Parameters.AddWithValue("@add", decimal.Parse(AdditionalMaterialsPriceBox.Text));
                        cmd.Parameters.AddWithValue("@active", TarifIsActiveCheckBox.IsChecked ?? true);
                    }
                    else if (currentMode == TableMode.Units)
                    {
                        query = "INSERT INTO Units (Name) VALUES (@name)";
                        cmd.Parameters.AddWithValue("@name", UnitBox.Text);
                    }

                    if (!string.IsNullOrEmpty(query))
                    {
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Добавлено!");
                        SwitchMode(currentMode);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка добавления: " + ex.Message); }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (MainGrid.SelectedItem == null) { MessageBox.Show("Выберите строку!"); return; }

            string table = "";
            string idCol = "";
            int idVal = 0;

            if (currentMode == TableMode.Prices && MainGrid.SelectedItem is MaterialPriceItem mp)
            { table = "MaterialPrices"; idCol = "PriceID"; idVal = mp.PriceID; }
            else if (currentMode == TableMode.Services && MainGrid.SelectedItem is ServiceItem s)
            { table = "Services"; idCol = "ServiceId"; idVal = s.ServiceId; }
            else if (currentMode == TableMode.Reference && MainGrid.SelectedItem is MaterialReferenceItem mr)
            { table = "Materials"; idCol = "MaterialID"; idVal = mr.MaterialID; }
            else if (currentMode == TableMode.Units && MainGrid.SelectedItem is UnitsItem u)
            { table = "Units"; idCol = "UnitID"; idVal = u.UnitID; }

            if (string.IsNullOrEmpty(table)) return;

            if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.No) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"DELETE FROM {table} WHERE {idCol} = @id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idVal);
                        cmd.ExecuteNonQuery();
                    }
                }
                SwitchMode(currentMode);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка удаления: " + ex.Message); }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = SearchBox.Text.ToLower();

            if (currentMode == TableMode.Prices)
                MainGrid.ItemsSource = pricesList.Where(p => p.MaterialsName.ToLower().Contains(txt)).ToList();
            else if (currentMode == TableMode.Reference)
                MainGrid.ItemsSource = referenceList.Where(r => r.Name.ToLower().Contains(txt)).ToList();
            else if (currentMode == TableMode.Services)
                MainGrid.ItemsSource = serviceList.Where(s => s.Name.ToLower().Contains(txt)).ToList();
            else if (currentMode == TableMode.Units)
                MainGrid.ItemsSource = unitsList.Where(u => u.Name.ToLower().Contains(txt)).ToList();
        }


        public void Update_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode(currentMode); 
                                     
        }

        private void Grid_Click(object sender, RoutedEventArgs e)
        {
            if (MainGrid.SelectedItem == null) return;

            EditWindow editWin = new EditWindow(this, currentMode, MainGrid.SelectedItem);
            editWin.ShowDialog(); 
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (MainGrid.SelectedItem != null)
            {
                Grid_Click(sender, null); 
            }
            else
            {
                MessageBox.Show("Выберите строку для редактирования!");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // --- МОДЕЛИ ДАННЫХ ---

    public class MaterialPriceItem
    {
        public int PriceID { get; set; }
        public int MatId { get; set; }
        public string MaterialsName { get; set; }
        public string UnitName { get; set; }
        public decimal PriceWithoutNds { get; set; }
        public decimal NdsPercent { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int StockAmount { get; set; }
        public decimal Nds { get; set; }
        public decimal TotalPrice { get; set; }
        public string IsActiveWord { get; set; }
        public bool IsActiveMat { get; set; }
        public bool IsActivePrice { get; set; }
        public string IsActivePriceWord { get; set; }
    }

    public class ServiceItem
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public decimal BasePrice { get; set; }
        public decimal AddMaterials { get; set; }
        public decimal TotalPrice { get; set; }
        public string IsActiveWord { get; set; }
        public bool IsActive { get; set; }
    }

    public class MaterialReferenceItem
    {
        public int MaterialID { get; set; }
        public string Name { get; set; }
        public string UnitName { get; set; }
        public string IsActiveWord { get; set; }
        public bool IsActive { get; set; }
    }

    public class UnitsItem
    {
        public int UnitID { get; set; }
        public string Name { get; set; }
    }
}