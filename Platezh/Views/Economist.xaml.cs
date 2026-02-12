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
        Prices,     // Цены и склад (SQL: MaterialPrices + Materials)
        Reference,  // Справочник материалов (SQL: Materials)
        Services,    // Услуги (SQL: Services)
        Units
    }

    public partial class Economist : Window
    {
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

            // Инициализация UI
            NdsChoice.SelectedIndex = 0;
            PriceChoice.SelectedIndex = 0;

            // Загружаем список единиц измерения из БД для ComboBox
            LoadUnits();

            // Запускаем режим по умолчанию
            SwitchMode(TableMode.Prices);
        }

        // Обработчик нажатия кнопок переключения
        private void ViewSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (sender == BtnPrices) SwitchMode(TableMode.Prices);
            else if (sender == BtnReference) SwitchMode(TableMode.Reference);
            else if (sender == BtnServices) SwitchMode(TableMode.Services);
            else if (sender == BtnUnits) SwitchMode(TableMode.Units);
        }

        // Основная логика переключения режимов
        private void SwitchMode(TableMode mode)
        {
            currentMode = mode;
            MainGrid.ItemsSource = null;
            MainGrid.Columns.Clear();

            // Сбрасываем видимость ВСЕХ панелей
            MaterialsInputPanel.Visibility = Visibility.Collapsed;
            ServicesInputPanel.Visibility = Visibility.Collapsed;
            ReferenceInputPanel.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case TableMode.Prices:
                    CurrentTableTitle.Content = "Цены и остатки";
                    MaterialsInputPanel.Visibility = Visibility.Visible;

                    // ВАЖНО: Загружаем список материалов для выбора и саму таблицу цен
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

        private void LoadUnits()
        {
            unitsList.Clear();

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
                    UnitChoice.ItemsSource = unitsList;
                    UnitChoice.DisplayMemberPath = "Name";
                    UnitChoice.SelectedValuePath = "UnitID";


                }
            }

            CreateColumns(new Dictionary<string, string> {
                {"UnitID", "ID"}, {"Name", "Название единицы измерения"}
            });

            MainGrid.ItemsSource = unitsList;

        }

        private void LoadPrices()
        {
            pricesList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT PriceID,  MaterialPrices.MaterialID as MatId, Materials.Name as MatName, Units.Name as UnitName, 
                               PriceWithoutNds, NdsPercent, ValidFrom, ValidTo, StockAmount, 
                               Materials.IsActive as MatActive 
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
                            IsActiveWord = reader.GetBoolean(reader.GetOrdinal("MatActive")) ? "Доступен" : "Недоступен"
                        });
                    }
                }
            }

            // Создаем колонки динамически
            CreateColumns(new Dictionary<string, string> {
                {"PriceID", "ID Цены"}, {"MaterialsName", "Материал"}, {"MatId","ID материала" }, {"StockAmount", "Остаток"},
                {"UnitName", "Ед."}, {"PriceWithoutNds", "Цена"}, {"Nds", "НДС"},
                {"TotalPrice", "Итого"}, {"ValidFrom", "Дата с"}, {"IsActiveWord", "Статус"}
            });

            MainGrid.ItemsSource = pricesList;
        }

        // 2. ЗАГРУЗКА СПРАВОЧНИКА (Новая таблица)
        private void LoadReference()
        {
            referenceList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Простой запрос к таблице Materials
                string query = "SELECT MaterialID, m.Name as mName, u.Name as uName, IsActive FROM Materials as m LEFT JOIN Units as u ON u.unitID = m.UnitID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        referenceList.Add(new MaterialReferenceItem
                        {
                            MaterialID = reader.GetInt32(reader.GetOrdinal("MaterialID")),
                            Name = reader.GetString(reader.GetOrdinal("mName")),
                            IsActiveWord = reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Активен" : "Неактивен",
                            UnitName = reader.GetString(reader.GetOrdinal("uName"))
                        });
                    }
                }
            }

            CreateColumns(new Dictionary<string, string> {
                {"MaterialID", "ID Мат."}, {"Name", "Название материала"}, {"UnitName", "Ед. измерения"}, {"IsActiveWord", "Статус"}
            });

            MainGrid.ItemsSource = referenceList;
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

                        serviceList.Add(new ServiceItem
                        {
                            ServiceId = reader.GetInt32(reader.GetOrdinal("ServiceId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            BasePrice = basePrice,
                            AddMaterials = addPrice,
                            TotalPrice = basePrice + addPrice,
                            IsActiveWord = reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Доступна" : "Недоступна"
                        });
                    }
                }
            }

            CreateColumns(new Dictionary<string, string> {
                {"ServiceId", "ID"}, {"Name", "Название"}, {"BasePrice", "Тариф"},
                {"AddMaterials", "Доп. мат."}, {"TotalPrice", "Итого"}, {"IsActiveWord", "Статус"}
            });

            MainGrid.ItemsSource = serviceList;
        }

        // Хелпер для создания колонок
        private void CreateColumns(Dictionary<string, string> headers)
        {
            foreach (var h in headers)
            {
                var col = new DataGridTextColumn
                {
                    Header = h.Value,
                    Binding = new Binding(h.Key),
                    Width = DataGridLength.Auto
                };
                // Форматирование дат
                if (h.Key.Contains("Valid"))
                    col.Binding.StringFormat = "dd.MM.yyyy";

                MainGrid.Columns.Add(col);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = SearchBox.Text.ToLower();

            if (currentMode == TableMode.Prices)
            {
                MainGrid.ItemsSource = pricesList.Where(p => p.MaterialsName.ToLower().Contains(txt)).ToList();
            }
            else if (currentMode == TableMode.Reference)
            {
                MainGrid.ItemsSource = referenceList.Where(r => r.Name.ToLower().Contains(txt)).ToList();
            }
            else if (currentMode == TableMode.Services)
            {
                MainGrid.ItemsSource = serviceList.Where(s => s.Name.ToLower().Contains(txt)).ToList();
            }
            else if (currentMode == TableMode.Units)
            {
                MainGrid.ItemsSource = unitsList.Where(s => s.Name.ToLower().Contains(txt)).ToList();
            }
        }

        // Кнопка Обновить
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode(currentMode); // Просто перезагружаем текущий режим
            MessageBox.Show("Данные обновлены.");
        }

        // Кнопка Удалить
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (MainGrid.SelectedItem == null) { MessageBox.Show("Выберите строку!"); return; }

            string table = "";
            string idCol = "";
            int idVal = 0;

            if (currentMode == TableMode.Prices && MainGrid.SelectedItem is MaterialPriceItem mp)
            {
                table = "MaterialPrices"; idCol = "PriceID"; idVal = mp.PriceID;
            }
            else if (currentMode == TableMode.Services && MainGrid.SelectedItem is ServiceItem s)
            {
                table = "Services"; idCol = "ServiceId"; idVal = s.ServiceId;
            }
            else if (currentMode == TableMode.Reference && MainGrid.SelectedItem is MaterialReferenceItem mr)
            {
                table = "Materials"; idCol = "MaterialID"; idVal = mr.MaterialID;
            }
            else if (currentMode == TableMode.Units && MainGrid.SelectedItem is UnitsItem u)

            if (string.IsNullOrEmpty(table)) return;

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

        private void LoadMaterialsForCombobox()
        {
            materialSelectionList.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Загружаем только активные материалы
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

                MaterialSelectBox.ItemsSource = materialSelectionList;
                // Указываем, какое поле показывать пользователю
                MaterialSelectBox.DisplayMemberPath = "Name";
                // Указываем, какое поле является значением (ID)
                MaterialSelectBox.SelectedValuePath = "MaterialID";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки списка материалов: " + ex.Message);
            }
        }


        // Заглушки для остальных кнопок (реализуйте по аналогии)
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

                    if (currentMode == TableMode.Prices) // ДОБАВЛЕНИЕ ЦЕНЫ
                    {
                        // Проверка: выбран ли материал
                        if (MaterialSelectBox.SelectedValue == null)
                        {
                            MessageBox.Show("Пожалуйста, выберите материал из списка!");
                            return;
                        }

                        // Получаем ID из ComboBox
                        int selectedMatId = (int)MaterialSelectBox.SelectedValue;

                        query = @"INSERT INTO MaterialPrices (MaterialID, PriceWithoutNds, NdsPercent, ValidFrom, ValidTo, StockAmount) 
                                  VALUES (@matId, @price, @nds, @from, @to, @stock)";

                        cmd.Parameters.AddWithValue("@matId", selectedMatId);

                        // Парсинг цены (желательно добавить try-parse проверку, но оставим как было для простоты)
                        if (!decimal.TryParse(PriceBox.Text, out decimal priceVal)) { MessageBox.Show("Некорректная цена"); return; }
                        cmd.Parameters.AddWithValue("@price", priceVal);

                        // НДС
                        decimal ndsVal = 0;
                        if (decimal.TryParse(NdsBox.Text, out decimal parsedNds)) ndsVal = parsedNds;

                        // Логика НДС (примерная, зависит от вашей бизнес-логики)
                        cmd.Parameters.AddWithValue("@nds", ndsVal);

                        cmd.Parameters.AddWithValue("@from", DateTime.Now);
                        cmd.Parameters.AddWithValue("@to", DateTime.Now.AddYears(1));

                        if (!int.TryParse(StockBox.Text, out int stockVal)) { MessageBox.Show("Некорректное количество"); return; }
                        cmd.Parameters.AddWithValue("@stock", stockVal);
                    }
                    else if (currentMode == TableMode.Reference)
                    {
                        // ... (код без изменений) ...
                        if (string.IsNullOrEmpty(UnitNameBox.Text) || UnitChoice.SelectedValue == null)
                        {
                            MessageBox.Show("Заполните название и выберите единицу измерения!"); return;
                        }
                        query = "INSERT INTO Materials (Name, UnitID, IsActive) VALUES (@name, @unit, @active)";
                        cmd.Parameters.AddWithValue("@name", UnitNameBox.Text);
                        cmd.Parameters.AddWithValue("@unit", UnitChoice.SelectedValue);
                        cmd.Parameters.AddWithValue("@active", IsActiveCheckBox.IsChecked ?? true);
                    }
                    else if (currentMode == TableMode.Services)
                    {
                        // ... (код без изменений) ...
                        query = "INSERT INTO Services (ServiceId, Name, BasePrice, AddMaterials, IsActive) VALUES (@serviceid, @name, @base, @add, @isactive)";
                        // Внимание: ServiceId лучше сделать автоинкрементом в БД, но если ввод ручной:
                        cmd.Parameters.AddWithValue("@serviceid", int.Parse(TarifIdBox.Text));
                        cmd.Parameters.AddWithValue("@name", TarifNameBox.Text);
                        cmd.Parameters.AddWithValue("@base", decimal.Parse(TarifBox.Text));
                        cmd.Parameters.AddWithValue("@add", decimal.Parse(AdditionalMaterialsPriceBox.Text));
                        cmd.Parameters.AddWithValue("@isactive", TarifIsActiveCheckBox.IsChecked ?? true);
                    }
                    else if (currentMode == TableMode.Units)
                    {
                        // ... (код без изменений) ...
                        query = "INSERT INTO Units (Name) VALUES (@name)";
                        cmd.Parameters.AddWithValue("@name", UnitBox.Text);
                    }

                    if (!string.IsNullOrEmpty(query))
                    {
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные успешно добавлены!");

                        // Если добавили материал в справочнике, нужно обновить и комбобокс выбора материалов
                        if (currentMode == TableMode.Reference) LoadMaterialsForCombobox();

                        SwitchMode(currentMode); // Обновляем таблицу
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        // ... (Остальные методы: MaterialSelectBox_KeyUp, Edit, Delete и классы данных остаются)
   

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            // Здесь ваша логика UPDATE
            MessageBox.Show("Функция редактирования для режима: " + currentMode.ToString());
        }

        private void Grid_Click(object sender, RoutedEventArgs e) { /* Двойной клик */ }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // MainWindow mw = new MainWindow(); mw.Show(); 
            this.Close();
        }

    }

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
    }

    public class ServiceItem
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public decimal BasePrice { get; set; }
        public decimal AddMaterials { get; set; }
        public decimal TotalPrice { get; set; }
        public string IsActiveWord { get; set; }
    }

    public class MaterialReferenceItem
    {
        public int MaterialID { get; set; }
        public string Name { get; set; }
        public string UnitName { get; set; } 
        public string IsActiveWord { get; set; }
    }

    public class UnitsItem
    {
        public int UnitID { get; set; }
        public string Name { get; set; }
    }
}