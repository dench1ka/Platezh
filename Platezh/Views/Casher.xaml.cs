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
using Microsoft.Win32;

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
        private List<BasketItem> basket = new List<BasketItem>();

        private List<MaterialPriceItem> selectedMaterials = new List<MaterialPriceItem>();
        private List<ServiceItem> selectedServices = new List<ServiceItem>();

        private List<SelectedMaterialEntry> selectedMaterialsWithQty = new List<SelectedMaterialEntry>();
        private List<ServiceItem> selectedServicesList = new List<ServiceItem>();

        private class SelectedMaterialEntry
        {
            public MaterialPriceItem Item { get; set; }
            public int Quantity { get; set; }
        }

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

        private void SwitchView(ViewMode mode)
        {
            currentMode = mode;

            // 1. Очистка таблицы перед сменой режима
            MainGrid.ItemsSource = null;
            MainGrid.Columns.Clear();

            // 2. Управление видимостью панелей в зависимости от режима
            if (mode == ViewMode.Clients)
            {
                // Показываем форму добавления клиента, скрываем корзину и договор
                if (BasketPanel != null) BasketPanel.Visibility = Visibility.Collapsed;
                if (ClientFormPanel != null) ClientFormPanel.Visibility = Visibility.Visible;
            }
            else
            {
                // Показываем корзину (для материалов/услуг), скрываем форму клиента
                if (BasketPanel != null) BasketPanel.Visibility = Visibility.Visible;
                if (ClientFormPanel != null) ClientFormPanel.Visibility = Visibility.Collapsed;
            }

            // 3. Загрузка данных и отрисовка колонок
            switch (mode)
            {
                case ViewMode.Materials:
                    CurrentTableTitle.Content = "Материалы";
                    LoadPrices();
                    break;

                case ViewMode.Services:
                    CurrentTableTitle.Content = "Услуги";
                    LoadServices();
                    break;

                case ViewMode.Clients:
                    CurrentTableTitle.Content = "База клиентов";
                    LoadClients();
                    break;
            }
        }

        private void ClearAllFields()
        {
            selectedMaterialsWithQty.Clear();
            selectedServicesList.Clear();
            SelectedItemsListBox.Items.Clear();

            // Очистка формы клиента
            SurnameBox.Clear();
            NameBox.Clear();
            FatherNameBox.Clear();
            PassportDataBox.Clear();
            IssuedByBox.Clear();
            AddressBox.Clear();

            ContractNumberBox.Clear();
            ClientSelectBox.SelectedItem = null;
            ClientSelectBox.Text = ""; // Очищаем текст поиска в комбобоксе

            LoadPrices();
            if (currentMode == ViewMode.Materials)
            {
                MainGrid.ItemsSource = null;
                MainGrid.ItemsSource = pricesList;
                MainGrid.Items.Refresh();
            }
        }

        private void CreateColumns(Dictionary<string, string> headers)
        {
            MainGrid.Columns.Clear();
            foreach (var h in headers)
            {
                var col = new DataGridTextColumn
                {
                    Header = h.Value,
                    Binding = new Binding(h.Key),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };

                // Если в ключе есть "Valid" или "Date", применяем формат даты
                if (h.Key.Contains("Valid") || h.Key.Contains("Date"))
                    col.Binding.StringFormat = "dd.MM.yyyy";

                MainGrid.Columns.Add(col);
            }
        }

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

            // Формируем словарь для Материалов
            var headers = new Dictionary<string, string>
            {
                { "PriceID", "ID Цены" },
                { "MatId", "ID Мат." },
                { "MaterialsName", "Наименование" },
                { "UnitName", "Ед. изм." },
                { "StockAmount", "Остаток" },
                { "PriceWithoutNds", "Цена (без НДС)" },
                { "Nds", "НДС (сумма)" },
                { "TotalPrice", "Итого (BYN)" },
                { "IsActiveWord", "Статус" }
            };

            CreateColumns(headers);
            MainGrid.ItemsSource = pricesList.ToList();
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

            // Формируем словарь для Услуг
            var headers = new Dictionary<string, string>
            {
                { "ServiceId", "ID Услуги" },
                { "Name", "Название услуги" },
                { "BasePrice", "Тариф (работа)" },
                { "AddMaterials", "Мат. затраты" },
                { "TotalPrice", "Итоговая цена" },
                { "IsActiveWord", "Доступность" }
            };

            CreateColumns(headers);
            MainGrid.ItemsSource = serviceList.ToList();
        }

        private void LoadClients()
        {
            clientList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Используем ваш запрос (убрал TOP 1000 для простоты, но можно оставить)
                string sql = @"SELECT [ClientID], [Name], [Surname], [FatherName], 
                              [PassportNumber], [IssuedBy], [IssuedDate], [Address] 
                       FROM [Clients]";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        clientList.Add(new ClientItem
                        {
                            id = r.GetInt32(r.GetOrdinal("ClientID")),
                            Name = r.IsDBNull(r.GetOrdinal("Name")) ? "" : r.GetString(r.GetOrdinal("Name")),
                            Surname = r.IsDBNull(r.GetOrdinal("Surname")) ? "" : r.GetString(r.GetOrdinal("Surname")),
                            FatherName = r.IsDBNull(r.GetOrdinal("FatherName")) ? "" : r.GetString(r.GetOrdinal("FatherName")),
                            Passport = r.IsDBNull(r.GetOrdinal("PassportNumber")) ? "—" : r.GetString(r.GetOrdinal("PassportNumber")),
                            IssuedBy = r.IsDBNull(r.GetOrdinal("IssuedBy")) ? "—" : r.GetString(r.GetOrdinal("IssuedBy")),
                            IssuedDate = r.IsDBNull(r.GetOrdinal("IssuedDate")) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("IssuedDate")),
                            Address = r.IsDBNull(r.GetOrdinal("Address")) ? "—" : r.GetString(r.GetOrdinal("Address"))
                        });
                    }
                }
            }

            // Обновляем комбобокс (он будет использовать свойство FullName)
            ClientSelectBox.ItemsSource = null;
            ClientSelectBox.ItemsSource = clientList;
            ClientSelectBox.DisplayMemberPath = "FullName"; // Указываем, что показывать в списке

            // Формируем таблицу, если выбран режим клиентов
            if (currentMode == ViewMode.Clients)
            {
                var headers = new Dictionary<string, string>
                {
                    { "id", "ID" },
                    { "Surname", "Фамилия" },
                    { "Name", "Имя" },
                    { "FatherName", "Отчество" },
                    { "Passport", "№ Паспорта" },
                    { "IssuedBy", "Кем выдан" },
                    { "IssuedDate", "Дата выдачи" },
                    { "Address", "Адрес" }
                };

                CreateColumns(headers);
                MainGrid.ItemsSource = clientList.ToList();
            }
        }
        private void ViewSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (sender == BtnMaterials) SwitchView(ViewMode.Materials);
            else if (sender == BtnServices) SwitchView(ViewMode.Services);
            else if (sender == BtnClients) SwitchView(ViewMode.Clients);
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = SearchBox.Text.ToLower();
            if (currentMode == ViewMode.Materials) MainGrid.ItemsSource = pricesList.Where(x => x.MaterialsName.ToLower().Contains(t)).ToList();
            else if (currentMode == ViewMode.Services) MainGrid.ItemsSource = serviceList.Where(x => x.Name.ToLower().Contains(t)).ToList();
            else MainGrid.ItemsSource = clientList.Where(x => x.FullName.ToLower().Contains(t)).ToList();
        }

        private void ClientSelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (ClientSelectBox.SelectedItem is ClientItem client)
            //{
            //    ClienNameBox.Text = client.FullName;
            //    PassportDataBox.Text = client.Passport;
            //    AddressBox.Text = client.Address;
            //}
        }

        private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentMode == ViewMode.Clients && MainGrid.SelectedItem is ClientItem client)
                ClientSelectBox.SelectedItem = client;
        }

        private void Addbtnclicked(object sender, RoutedEventArgs e) => SelectRecord();

        // Обработчик события из XAML
        private void Grid_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectRecord();
        }

        private void SelectRecord()
        {
            // Проверка: выделена ли строка?
            if (MainGrid.SelectedItem == null) return;

            // --- ЛОГИКА ДЛЯ МАТЕРИАЛОВ ---
            if (currentMode == ViewMode.Materials && MainGrid.SelectedItem is MaterialPriceItem mat)
            {
                if (mat.IsActiveWord == "Недоступен")
                {
                    MessageBox.Show($"Материал '{mat.MaterialsName}' недоступен для заказа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string input = Interaction.InputBox($"Кол-во для '{mat.MaterialsName}' (На складе: {mat.StockAmount}):", "Добавление", "1");

                if (int.TryParse(input, out int qty) && qty > 0)
                {
                    if (qty > mat.StockAmount)
                    {
                        MessageBox.Show($"Недостаточно на складе! Доступно: {mat.StockAmount}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    mat.StockAmount -= qty;

                    var newItem = new BasketItem
                    {
                        DisplayName = $"Мат: {mat.MaterialsName} x{qty} | {mat.TotalPrice * qty} BYN",
                        Material = mat,
                        Quantity = qty
                    };
                    basket.Add(newItem);
                    SelectedItemsListBox.Items.Add(newItem.DisplayName);

                    // Теперь это безопасно, так как IsReadOnly="True"
                    MainGrid.Items.Refresh();
                }
            }
            // --- ЛОГИКА ДЛЯ УСЛУГ ---
            else if (currentMode == ViewMode.Services && MainGrid.SelectedItem is ServiceItem ser)
            {
                if (ser.IsActiveWord == "Недоступна")
                {
                    MessageBox.Show($"Услуга '{ser.Name}' на данный момент недоступна!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newItem = new BasketItem
                {
                    DisplayName = $"Усл: {ser.Name} | {ser.TotalPrice} BYN",
                    Service = ser
                };
                basket.Add(newItem);
                SelectedItemsListBox.Items.Add(newItem.DisplayName);
            }
        }



        // ОБНОВЛЕННЫЙ метод удаления (чтобы списки не рассинхронизировались)
        private void Deletebtn_Click(object sender, RoutedEventArgs e)
        {
            int index = SelectedItemsListBox.SelectedIndex;
            if (index != -1)
            {
                var itemToRemove = basket[index];

                // Если это был материал — возвращаем остаток
                if (itemToRemove.IsMaterial)
                {
                    itemToRemove.Material.StockAmount += itemToRemove.Quantity;

                    // Важно: обновляем таблицу сразу
                    if (currentMode == ViewMode.Materials)
                    {
                        MainGrid.Items.Refresh();
                    }
                }

                // Удаляем из обоих списков по одному и тому же индексу
                basket.RemoveAt(index);
                SelectedItemsListBox.Items.RemoveAt(index);
            }
        }
        private void SelectedItemsListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { }

        private void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. ПРОВЕРКИ ПЕРЕД НАЧАЛОМ

                // Проверка выбора клиента
                var selectedClient = ClientSelectBox.SelectedItem as ClientItem;
                if (selectedClient == null)
                {
                    MessageBox.Show("Пожалуйста, выберите клиента из выпадающего списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка корзины
                if (basket.Count == 0)
                {
                    MessageBox.Show("Корзина пуста! Добавьте услуги или материалы.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. ВЫБОР ПУТИ СОХРАНЕНИЯ
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"Договор_{selectedClient.Surname}_{DateTime.Now:ddMMyyyy_HHmm}"
                };

                if (saveDialog.ShowDialog() != true) return;
                string filePath = saveDialog.FileName;
                string directory = System.IO.Path.GetDirectoryName(filePath);

                // 3. ТРАНЗАКЦИЯ: СПИСАНИЕ СО СКЛАДА В БД
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var item in basket.Where(x => x.IsMaterial))
                            {
                                // Проверяем актуальный остаток в базе прямо перед списанием
                                string checkSql = "SELECT StockAmount FROM MaterialPrices WHERE PriceID = @id";
                                int dbStock = 0;
                                using (SqlCommand cmdCheck = new SqlCommand(checkSql, conn, transaction))
                                {
                                    cmdCheck.Parameters.AddWithValue("@id", item.Material.PriceID);
                                    dbStock = (int)cmdCheck.ExecuteScalar();
                                }

                                if (dbStock < item.Quantity)
                                {
                                    throw new Exception($"Недостаточно товара '{item.Material.MaterialsName}' на складе.\n" +
                                                        $"Доступно: {dbStock}, требуется: {item.Quantity}");
                                }

                                // Уменьшаем остаток
                                string updateSql = "UPDATE MaterialPrices SET StockAmount = StockAmount - @qty WHERE PriceID = @id";
                                using (SqlCommand cmdUpdate = new SqlCommand(updateSql, conn, transaction))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@qty", item.Quantity);
                                    cmdUpdate.Parameters.AddWithValue("@id", item.Material.PriceID);
                                    cmdUpdate.ExecuteNonQuery();
                                }
                            }
                            // Если всё ок — фиксируем изменения в БД
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Ошибка при обновлении склада: " + ex.Message);
                        }
                    }
                }

                // 4. ПОДГОТОВКА ДАННЫХ ДЛЯ EXCEL
                var excelService = new ExcelService();

                // Преобразование материалов из корзины в формат для ExcelService
                var materialsForExcel = basket
                    .Where(b => b.IsMaterial)
                    .Select(m => new Platezh.Services.Material
                    {
                        id = m.Material.MatId,
                        name = m.Material.MaterialsName,
                        count = m.Quantity,
                        nds = m.Material.Nds,
                        totalPrice = m.Material.TotalPrice * m.Quantity
                    }).ToList();

                // Преобразование услуг из корзины в формат для ExcelService
                var servicesForExcel = basket
                    .Where(b => !b.IsMaterial)
                    .Select(s => new Platezh.Services.Service
                    {
                        id = s.Service.ServiceId,
                        name = s.Service.Name,
                        count = 1,
                        tariff = s.Service.BasePrice,
                        additionalMaterialCost = s.Service.AddMaterials,
                        totalCost = s.Service.TotalPrice
                    }).ToList();

                // Формирование общих данных договора
                var contractData = new ContractData
                {
                    // Если номер договора не введён вручную — генерируем автоматически
                    contractNumber = string.IsNullOrWhiteSpace(ContractNumberBox.Text)
                                     ? "№" + DateTime.Now.ToString("yyyyMMdd-HHmm")
                                     : ContractNumberBox.Text,
                    clientName = selectedClient.FullName,
                    passportNumber = selectedClient.Passport,
                    address = selectedClient.Address,
                    issuedBy = selectedClient.IssuedBy,
                    dateIssued = selectedClient.IssuedDate ?? DateTime.Now
                };

                // 5. ГЕНЕРАЦИЯ ФАЙЛА
                // Предполагается, что FillContract принимает (List<Service>, List<Material>, ContractData, string path)
                excelService.FillContract(servicesForExcel, materialsForExcel, contractData, directory);

                // 6. ФИНАЛИЗАЦИЯ
                MessageBox.Show($"Договор {contractData.contractNumber} успешно сформирован и сохранен!",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Полная очистка формы для следующего клиента
                ClearAllFields();

                // Обновляем таблицу, чтобы увидеть новые остатки
                if (currentMode == ViewMode.Materials) LoadPrices();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка формирования договора: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Валидация (минимальная проверка на заполнение)
                if (string.IsNullOrWhiteSpace(SurnameBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    MessageBox.Show("Фамилия и Имя обязательны для заполнения!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Определяем: мы редактируем существующего или создаем нового?
                var selectedClient = MainGrid.SelectedItem as ClientItem;
                bool isUpdate = selectedClient != null;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql;

                    if (isUpdate)
                    {
                        // SQL для обновления
                        sql = @"UPDATE Clients 
                        SET Name = @name, Surname = @surname, FatherName = @father, 
                            PassportNumber = @pass, IssuedBy = @issued, Address = @addr
                        WHERE ClientID = @id";
                    }
                    else
                    {
                        // SQL для вставки нового
                        sql = @"INSERT INTO Clients (Name, Surname, FatherName, PassportNumber, IssuedBy, Address) 
                        VALUES (@name, @surname, @father, @pass, @issued, @addr)";
                    }

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        // Привязываем параметры (защита от SQL-инъекций)
                        cmd.Parameters.AddWithValue("@name", NameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@surname", SurnameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@father", FatherNameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@pass", PassportDataBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@issued", IssuedByBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@addr", AddressBox.Text.Trim());

                        if (isUpdate)
                        {
                            cmd.Parameters.AddWithValue("@id", selectedClient.id);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(isUpdate ? "Данные клиента успешно обновлены" : "Новый клиент успешно добавлен в базу",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // 2. Очищаем поля формы
                ClearClientForm();

                // 3. ПЕРЕЗАГРУЖАЕМ данные, чтобы они появились везде (в таблице и в ComboBox)
                LoadClients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении в базу данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный метод для очистки только полей клиента
        private void ClearClientForm()
        {
            SurnameBox.Clear();
            NameBox.Clear();
            FatherNameBox.Clear();
            PassportDataBox.Clear();
            IssuedByBox.Clear();
            AddressBox.Clear();
            MainGrid.SelectedItem = null; // Снимаем выделение, чтобы следующий клик "Сохранить" создавал нового
        }

         private void Button_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void ShowContracts(object sender, RoutedEventArgs e) { }
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) { }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }


        private class BasketItem
        {
            public string DisplayName { get; set; } // То, что видит пользователь
            public MaterialPriceItem Material { get; set; } // Ссылка на материал (если это он)
            public ServiceItem Service { get; set; } // Ссылка на услугу (если это она)
            public int Quantity { get; set; }
            public bool IsMaterial => Material != null;
        }

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
            public string Surname { get; set; } = "";
            public string Name { get; set; } = "";
            public string FatherName { get; set; } = "";

            // Вспомогательное свойство для отображения полного имени в комбобоксе
            public string FullName => $"{Surname} {Name} {FatherName}".Trim();

            public string Passport { get; set; } = ""; // Это будет PassportNumber
            public string IssuedBy { get; set; } = "";
            public DateTime? IssuedDate { get; set; } // Дата выдачи
            public string Address { get; set; } = "";
        }
    }
}