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
        private int _currentUserId;

        private ViewMode currentMode = ViewMode.Materials;
        private enum ViewMode { Materials, Services, Clients, PaymentTypes, History }

        private List<PaymentTypeItem> paymentTypesList = new List<PaymentTypeItem>();
        public class PaymentTypeItem
        {
            public int PaymentTypeID { get; set; }
            public string PaymentName { get; set; } = "";
        }

        private List<MaterialPriceItem> pricesList = new List<MaterialPriceItem>();
        private List<ServiceItem> serviceList = new List<ServiceItem>();
        private List<ClientItem> clientList = new List<ClientItem>();
        private List<BasketItem> basket = new List<BasketItem>();
        private List<HistoryItem> historyList = new List<HistoryItem>();

        private List<SelectedMaterialEntry> selectedMaterialsWithQty = new List<SelectedMaterialEntry>();
        private List<ServiceItem> selectedServicesList = new List<ServiceItem>();

        public Casher(int loggedInUserId)
        {
            InitializeComponent();
            LoadAllData();
            SwitchView(ViewMode.Materials);
            _currentUserId = loggedInUserId; // Сохраняем ID того, кто вошел
        }

        private void LoadAllData()
        {
            try
            {
                LoadPrices();
                LoadServices();
                LoadClients();
                LoadPaymentTypes();
                LoadHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void SwitchView(ViewMode mode)
        {
            currentMode = mode;

            MainGrid.ItemsSource = null;
            MainGrid.Columns.Clear();

            if (mode == ViewMode.Clients)
            {
                if (BasketPanel != null) BasketPanel.Visibility = Visibility.Collapsed;
                if (ClientFormPanel != null) ClientFormPanel.Visibility = Visibility.Visible;
                if (PaymentTypeFormPanel != null) PaymentTypeFormPanel.Visibility = Visibility.Collapsed;
                if (HistoryContractsFormPanel != null) HistoryContractsFormPanel.Visibility = Visibility.Collapsed;
            }
            else if (mode == ViewMode.PaymentTypes)
            {
                if (BasketPanel != null) BasketPanel.Visibility = Visibility.Collapsed;
                if (ClientFormPanel != null) ClientFormPanel.Visibility = Visibility.Collapsed;
                if (PaymentTypeFormPanel != null) PaymentTypeFormPanel.Visibility = Visibility.Visible;
                if (HistoryContractsFormPanel != null) HistoryContractsFormPanel.Visibility = Visibility.Collapsed;
            }
            else if (mode == ViewMode.Materials || mode == ViewMode.Services)
            {
                if (BasketPanel != null) BasketPanel.Visibility = Visibility.Visible;
                if (ClientFormPanel != null) ClientFormPanel.Visibility = Visibility.Collapsed;
                if (PaymentTypeFormPanel != null) PaymentTypeFormPanel.Visibility = Visibility.Collapsed;
                if (HistoryContractsFormPanel != null) HistoryContractsFormPanel.Visibility = Visibility.Collapsed;
            }
            else if (mode == ViewMode.History)
            {
                if (BasketPanel != null) BasketPanel.Visibility = Visibility.Collapsed;
                if (ClientFormPanel != null) ClientFormPanel.Visibility = Visibility.Collapsed;
                if (PaymentTypeFormPanel != null) PaymentTypeFormPanel.Visibility = Visibility.Collapsed;
                if (HistoryContractsFormPanel != null) HistoryContractsFormPanel.Visibility = Visibility.Visible;
            }

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
                    case ViewMode.PaymentTypes:
                        CurrentTableTitle.Content = "Способы платежей";
                        LoadPaymentTypes();
                        break;
                    case ViewMode.History:
                        CurrentTableTitle.Content = "История договоров";
                        LoadHistory();
                        break;
                }
        }

        private void ClearAllFields()
        {
            basket.Clear(); 
            selectedMaterialsWithQty.Clear();
            selectedServicesList.Clear();

            SelectedItemsListBox.Items.Clear();

            SurnameBox.Clear();
            NameBox.Clear();
            FatherNameBox.Clear();
            PassportDataBox.Clear();
            IssuedByBox.Clear();
            AddressBox.Clear();

            ContractNumberBox.Clear();
            ClientSelectBox.SelectedItem = null;
            ClientSelectBox.Text = "";

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

                if (h.Key.Contains("Valid") || h.Key.Contains("Date"))
                    col.Binding.StringFormat = "dd.MM.yyyy";

                MainGrid.Columns.Add(col);
            }
        }

        private void LoadPaymentTypes()
        {
            paymentTypesList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT PaymentTypeID, PaymentName FROM PaymentType";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        paymentTypesList.Add(new PaymentTypeItem
                        {
                            PaymentTypeID = r.GetInt32(0),
                            PaymentName = r.GetString(1)
                        });
                    }
                }
            }

            PaymentMethodComboBox.ItemsSource = null;
            PaymentMethodComboBox.ItemsSource = paymentTypesList;

            if (currentMode == ViewMode.PaymentTypes) 
            {
                var headers = new Dictionary<string, string>
                {
                    { "PaymentTypeID", "ID" },
                    { "PaymentName", "Название способа оплаты" }
                };
                CreateColumns(headers);
                MainGrid.ItemsSource = paymentTypesList.ToList();
            }
        }

        private void LoadHistory()
        {
            historyList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT co.ContractID as coId, co.ContractNumber as coN, c.Name as cN, c.Surname as cS, c.FatherName as cF, co.ContractDate as coD, co.TotalWithoutNds as coT, co.TotalNds as coTN, co.TotalAmount as coTA, co.Status as coS, u.Login as uL FROM Contracts as co 
                    LEFT JOIN Clients as c ON co.ClientID = c.ClientID
                    LEFT JOIN Users as u ON co.CreatedBy = u.UserID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    { 
                        string fullName = reader.GetString(reader.GetOrdinal("cN")) + " " + reader.GetString(reader.GetOrdinal("cS")) + " " + reader.GetString(reader.GetOrdinal("cF"));

                        historyList.Add(new HistoryItem
                        {
                            ContractID = reader.GetInt32(reader.GetOrdinal("coId")),
                            ContractNumber = reader.GetString(reader.GetOrdinal("coN")),
                            ClientName = fullName,
                            ContractDate = reader.GetDateTime(reader.GetOrdinal("coD")),
                            TotalWithoutNds = reader.GetDecimal(reader.GetOrdinal("coT")),
                            TotalNds = reader.GetDecimal(reader.GetOrdinal("coTN")),
                            TotalPrice = reader.GetDecimal(reader.GetOrdinal("coTA")),
                            Status = reader.GetString(reader.GetOrdinal("coS")),
                            CreatedBy = reader.GetString(reader.GetOrdinal("uL")),
                        });
                    }
                }
            }

            var headers = new Dictionary<string, string>
            {
                { "ContractID", "ID Договора" },
                { "ContractNumber", "Номер договора" },
                { "ClientName", "ФИО клиента" },
                { "ContractDate", "Дата договора" },
                { "TotalWithoutNds", "Цена без НДС (BYN)" },
                { "TotalNds", "НДС (BYN)" },
                { "TotalPrice", "Итого (BYN)" },
                { "Status", "Статус" },
                { "CreatedBy", "Оформлен" }
            };

            CreateColumns(headers);
            MainGrid.ItemsSource = historyList.ToList();
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
                string sql = @"SELECT ClientID, Name, Surname, FatherName, 
                              PassportNumber, IssuedBy, IssuedDate, Address 
                       FROM Clients";

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

            ClientSelectBox.ItemsSource = null;
            ClientSelectBox.ItemsSource = clientList;
            ClientSelectBox.DisplayMemberPath = "FullName";

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
            else if (sender == BtnPayments) SwitchView(ViewMode.PaymentTypes);
            else if (sender == BtnHistory) SwitchView(ViewMode.History);
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = SearchBox.Text.ToLower();
            if (currentMode == ViewMode.Materials) MainGrid.ItemsSource = pricesList.Where(x => x.MaterialsName.ToLower().Contains(t)).ToList();
            else if (currentMode == ViewMode.Services) MainGrid.ItemsSource = serviceList.Where(x => x.Name.ToLower().Contains(t)).ToList();
            else if (currentMode == ViewMode.PaymentTypes) MainGrid.ItemsSource = paymentTypesList.Where(x => x.PaymentName.ToLower().Contains(t)).ToList();
            else if (currentMode == ViewMode.History) MainGrid.ItemsSource = historyList.Where(x => x.ContractNumber.ToLower().Contains(t)).ToList();
            else MainGrid.ItemsSource = clientList.Where(x => x.FullName.ToLower().Contains(t)).ToList();
        }


        private void SavePayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PaymentTypeNameBox.Text))
                {
                    MessageBox.Show("Название оплаты обязательно для заполнения!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedPaymentType = MainGrid.SelectedItem as PaymentTypeItem;
                bool isUpdate = selectedPaymentType != null;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO PaymentType (PaymentName) VALUES (@pn)";


                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pn", PaymentTypeNameBox.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(isUpdate ? "Данные способа оплыт успешно обновлены" : "Новый способ оплаты успешно добавлен в базу",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearPayForm();

                LoadPaymentTypes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении в базу данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainGrid.SelectedItem is PaymentTypeItem selectedPaymentType))
            {
                MessageBox.Show("Пожалуйста, выберите способ оплаты из списка для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить способ оплаты {selectedPaymentType.PaymentName}?",
                                         "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = "DELETE FROM PaymentType WHERE PaymentTypeID = @id";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedPaymentType.PaymentTypeID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Способ оплаты успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearPayForm();
                    LoadPaymentTypes();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        


        private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentMode == ViewMode.Clients && MainGrid.SelectedItem is ClientItem client)
            {
                ClientSelectBox.SelectedItem = client;
            } else if (currentMode == ViewMode.History && MainGrid.SelectedItem is HistoryItem history) {
                
            }
            
        }

        private void Addbtnclicked(object sender, RoutedEventArgs e) => SelectRecord();

        private void Grid_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectRecord();
        }

        private void SelectRecord()
        {
            if (MainGrid.SelectedItem == null) return;

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

                    MainGrid.Items.Refresh();
                }
            }
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



        private void Deletebtn_Click(object sender, RoutedEventArgs e)
        {
            int index = SelectedItemsListBox.SelectedIndex;
            if (index != -1)
            {
                var itemToRemove = basket[index];

                if (itemToRemove.IsMaterial)
                {
                    itemToRemove.Material.StockAmount += itemToRemove.Quantity;

                    if (currentMode == ViewMode.Materials)
                    {
                        MainGrid.Items.Refresh();
                    }
                }

                basket.RemoveAt(index);
                SelectedItemsListBox.Items.RemoveAt(index);
            }
        }
        private void SelectedItemsListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { }

        private void MoreInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == ViewMode.History && MainGrid.SelectedItem is HistoryItem selectedHistory)
            {
                var moreContractInfoWindow = new MoreContractInfoWindow(selectedHistory.ContractID);
                moreContractInfoWindow.Owner = this;
                moreContractInfoWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите договор из списка для подробной информации.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedClient = ClientSelectBox.SelectedItem as ClientItem;
                if (selectedClient == null)
                {
                    MessageBox.Show("Пожалуйста, выберите клиента из выпадающего списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (basket.Count == 0)
                {
                    MessageBox.Show("Корзина пуста! Добавьте услуги или материалы.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"Договор_{selectedClient.Surname}_{DateTime.Now:ddMMyyyy_HHmm}"
                };

                if (saveDialog.ShowDialog() != true) return;
                string filePath = saveDialog.FileName;
                string directory = System.IO.Path.GetDirectoryName(filePath);

                var excelService = new ExcelService();

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

                var contractData = new ContractData
                {
                    contractNumber = string.IsNullOrWhiteSpace(ContractNumberBox.Text)
                                     ? "№" + DateTime.Now.ToString("yyyyMMdd-HHmm")
                                     : ContractNumberBox.Text,
                    clientName = selectedClient.FullName,
                    passportNumber = selectedClient.Passport,
                    address = selectedClient.Address,
                    issuedBy = selectedClient.IssuedBy,
                    dateIssued = selectedClient.IssuedDate ?? DateTime.Now
                };

                bool isPaid = IsPaidCheckBox.IsChecked == true;
                string statusString = isPaid ? "Оплачено" : "Не оплачено";

                var selectedPaymentType = PaymentMethodComboBox.SelectedItem as PaymentTypeItem;
                int? selectedPaymentTypeId = selectedPaymentType?.PaymentTypeID;
                
                if (isPaid && selectedPaymentTypeId == null)
                {
                    MessageBox.Show("Выберите способ оплаты!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // --- 1. Подсчет итоговых сумм ---
                            decimal totalWithoutNds = 0;
                            decimal totalNds = 0;
                            decimal totalAmount = 0;

                            foreach (var item in basket)
                            {
                                if (item.IsMaterial)
                                {
                                    totalWithoutNds += item.Material.PriceWithoutNds * item.Quantity;
                                    totalNds += item.Material.Nds * item.Quantity;
                                    totalAmount += item.Material.TotalPrice * item.Quantity;
                                }
                                else
                                {
                                    totalWithoutNds += item.Service.TotalPrice;
                                    totalAmount += item.Service.TotalPrice;
                                }
                            }

                            string insertContractSql = @"
                            INSERT INTO Contracts (ContractNumber, ClientID, ContractDate, TotalWithoutNds, TotalNds, TotalAmount, Status, CreatedBy)
                            VALUES (@num, @clientId, @date, @totNoNds, @totNds, @totAmt, @status, @createdBy);
                            SELECT SCOPE_IDENTITY();";

                            int newContractId = 0;
                            using (SqlCommand cmdContract = new SqlCommand(insertContractSql, conn, transaction))
                            {
                                cmdContract.Parameters.AddWithValue("@num", contractData.contractNumber);
                                cmdContract.Parameters.AddWithValue("@clientId", selectedClient.id);
                                cmdContract.Parameters.AddWithValue("@date", DateTime.Now);
                                cmdContract.Parameters.AddWithValue("@totNoNds", totalWithoutNds);
                                cmdContract.Parameters.AddWithValue("@totNds", totalNds);
                                cmdContract.Parameters.AddWithValue("@totAmt", totalAmount);
                                cmdContract.Parameters.AddWithValue("@status", statusString);
                                cmdContract.Parameters.AddWithValue("@createdBy", _currentUserId);

                                newContractId = Convert.ToInt32(cmdContract.ExecuteScalar());
                            }

                            // --- 3. Добавление элементов корзины в ContractItems и обновление остатков ---
                            foreach (var item in basket)
                            {
                                string insertItemSql = @"
                                    INSERT INTO ContractItems (ContractID, ItemType, Quantity, PriceWithoutNds, NdsPercent, Total, MaterialName, ServiceName)
                                    VALUES (@cid, @type, @qty, @price, @nds, @total, @mn, @sn)";

                                using (SqlCommand cmdItem = new SqlCommand(insertItemSql, conn, transaction))
                                {
                                    cmdItem.Parameters.AddWithValue("@cid", newContractId);
                                    cmdItem.Parameters.AddWithValue("@qty", item.Quantity > 0 ? item.Quantity : 1);

                                    if (item.IsMaterial)
                                    {
                                        // Логика для МАТЕРИАЛА
                                        cmdItem.Parameters.AddWithValue("@type", "Material");
                                        cmdItem.Parameters.AddWithValue("@mn", item.Material!.MaterialsName); // Передаем ID материала
                                        cmdItem.Parameters.AddWithValue("@sn", DBNull.Value);        // Услуги нет
                                        cmdItem.Parameters.AddWithValue("@price", item.Material.PriceWithoutNds);

                                        decimal ndsPercent = item.Material.PriceWithoutNds > 0
                                            ? (item.Material.Nds / item.Material.PriceWithoutNds) * 100
                                            : 0;
                                        cmdItem.Parameters.AddWithValue("@nds", ndsPercent);
                                        cmdItem.Parameters.AddWithValue("@total", item.Material.TotalPrice * item.Quantity);

                                        // Обновление остатков (уже есть в вашем коде)
                                        string updateSql = "UPDATE MaterialPrices SET StockAmount = StockAmount - @qty WHERE PriceID = @id";
                                        using (SqlCommand cmdUpdate = new SqlCommand(updateSql, conn, transaction))
                                        {
                                            cmdUpdate.Parameters.AddWithValue("@qty", item.Quantity);
                                            cmdUpdate.Parameters.AddWithValue("@id", item.Material.PriceID);
                                            cmdUpdate.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        // Логика для УСЛУГИ
                                        cmdItem.Parameters.AddWithValue("@type", "Service");
                                        cmdItem.Parameters.AddWithValue("@mn", DBNull.Value);        // Материала нет
                                        cmdItem.Parameters.AddWithValue("@sn", item.Service!.Name); 
                                        cmdItem.Parameters.AddWithValue("@price", item.Service.TotalPrice);
                                        cmdItem.Parameters.AddWithValue("@nds", 0);
                                        cmdItem.Parameters.AddWithValue("@total", item.Service.TotalPrice);
                                    }

                                    cmdItem.ExecuteNonQuery(); // Теперь @si и @mi всегда объявлены
                                }
                            }

                            // --- 4. Добавление записи в Payments (если оплачено) ---
                            if (isPaid && selectedPaymentTypeId.HasValue)
                            {
                                string insertPaymentSql = @"
                                INSERT INTO Payments (ContractID, PaymentDate, Amount, PaymentTypeID)
                                VALUES (@cid, @date, @amount, @pid)";

                                using (SqlCommand cmdPayment = new SqlCommand(insertPaymentSql, conn, transaction))
                                {
                                    cmdPayment.Parameters.AddWithValue("@cid", newContractId);
                                    cmdPayment.Parameters.AddWithValue("@date", DateTime.Now);
                                    cmdPayment.Parameters.AddWithValue("@amount", totalAmount);
                                    cmdPayment.Parameters.AddWithValue("@pid", selectedPaymentTypeId.Value);
                                    cmdPayment.ExecuteNonQuery();
                                }
                            }

                            // Подтверждаем транзакцию
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Ошибка при оформлении договора в БД: " + ex.Message);
                        }
                    }
                }

                excelService.FillContract(servicesForExcel, materialsForExcel, contractData, directory);

                MessageBox.Show($"Договор {contractData.contractNumber} успешно сформирован и сохранен!",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearAllFields();

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
                if (string.IsNullOrWhiteSpace(SurnameBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    MessageBox.Show("Фамилия и Имя обязательны для заполнения!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedClient = MainGrid.SelectedItem as ClientItem;
                bool isUpdate = selectedClient != null;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                        string sql = @"INSERT INTO Clients (Name, Surname, FatherName, PassportNumber, IssuedBy, Address, IssuedDate) 
                        VALUES (@name, @surname, @father, @pass, @issued, @addr, @issuedDate)";
    

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@surname", SurnameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@father", FatherNameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@pass", PassportDataBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@issued", IssuedByBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@addr", AddressBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@issuedDate", DateIssued.SelectedDate);

                        if (isUpdate)
                        {
                            cmd.Parameters.AddWithValue("@id", selectedClient.id);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(isUpdate ? "Данные клиента успешно обновлены" : "Новый клиент успешно добавлен в базу",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearClientForm();

                LoadClients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении в базу данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == ViewMode.Clients && MainGrid.SelectedItem is ClientItem selectedClient)
            {
                var editWindow = new EditClientWindow(selectedClient);
                editWindow.Owner = this; 
                editWindow.ShowDialog(); 

                if (editWindow.DataChanged)
                {
                    LoadClients();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите клиента из списка для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteClientBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainGrid.SelectedItem is ClientItem selectedClient))
            {
                MessageBox.Show("Пожалуйста, выберите клиента из списка для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить клиента {selectedClient.FullName}?",
                                         "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = "DELETE FROM Clients WHERE ClientID = @id";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedClient.id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Клиент успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearClientForm();
                    LoadClients();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearClientForm()
        {
            SurnameBox.Clear();
            NameBox.Clear();
            FatherNameBox.Clear();
            PassportDataBox.Clear();
            IssuedByBox.Clear();
            AddressBox.Clear();
            MainGrid.SelectedItem = null;
            DateIssued.SelectedDate = null;
        }

        private void ClearPayForm()
        {
            PaymentTypeNameBox.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void ShowContracts(object sender, RoutedEventArgs e) { }
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e) { }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

        public class MaterialPriceItem
        {
            public int PriceID { get; set; }
            public int MatId { get; set; }
            public string MaterialsName { get; set; } = ""; // Инициализация строкой
            public string UnitName { get; set; } = "";
            public decimal PriceWithoutNds { get; set; }
            public int StockAmount { get; set; }
            public decimal Nds { get; set; }
            public decimal TotalPrice { get; set; }
            public string IsActiveWord { get; set; } = "";
            public string IsActivePriceWord { get; set; } = ""; // Добавьте, если используется
        }

        private class BasketItem
        {
            public string DisplayName { get; set; } = "";
            public MaterialPriceItem? Material { get; set; } // Может быть NULL, если это услуга
            public ServiceItem? Service { get; set; }        // Может быть NULL, если это материал
            public int Quantity { get; set; }
            public bool IsMaterial => Material != null;
        }

        private class SelectedMaterialEntry
        {
            public MaterialPriceItem Item { get; set; } = null!; // null! говорит компилятору: "я знаю, что тут будет объект"
            public int Quantity { get; set; }
        }

        public class HistoryItem
        {
            public int ContractID { get; set; }
            public string ContractNumber { get; set; } = "";
            public string ClientName { get; set; } = "";
            public DateTime ContractDate { get; set; }
            public decimal TotalWithoutNds { get; set; }
            public decimal TotalNds { get; set; }
            public decimal TotalPrice { get; set; }
            public string Status { get; set; } = "";
            public string CreatedBy { get; set; } = "";

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

            public string FullName => $"{Surname} {Name} {FatherName}".Trim();

            public string Passport { get; set; } = ""; 
            public string IssuedBy { get; set; } = "";
            public DateTime? IssuedDate { get; set; } 
            public string Address { get; set; } = "";
        }
    }
}