using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.SqlClient;
using Platezh.Services;
using Ookii.Dialogs.Wpf;
using Microsoft.VisualBasic;
using System.IO;
using System.Configuration;



namespace Platezh.Views
{
    public partial class Casher : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;
        private bool materialsVisible = true;

        private List<MaterialItem> materialList = new List<MaterialItem>();
        private List<ServiceItem> serviceList = new List<ServiceItem>();

        private List<MaterialItem> selectedMaterials = new List<MaterialItem>();
        private List<ServiceItem> selectedServices = new List<ServiceItem>();

        private readonly ExcelService excelService = new ExcelService();

        private AppSettings appSettings;
        public Casher()
        {
            InitializeComponent();
            appSettings = SettingsManager.LoadSettings();

           
            LoadMaterials();
            LoadServices();
            TypeCategories();
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
                Materials.Visibility = Visibility.Visible;
                TypeCategoriesbtn.Content = "Показать услуги";
                NameOfCategory.Content = "Материалы";
            }
            else
            {
                Materials.Visibility = Visibility.Collapsed;
                Services.Visibility = Visibility.Visible;
                TypeCategoriesbtn.Content = "Показать материалы";
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
                    .Where(s => s.name.ToLower().Contains(searchText))
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
            { "id", "ID" },
            { "name", "Название" },
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

            Materials.ItemsSource = materialList;
            Materials.AutoGenerateColumns = false;
            Materials.Columns.Clear();

            foreach (var column in columnHeadersMaterials)
            {
                Materials.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Value,
                    Binding = new Binding(column.Key),
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
                            id = reader.GetInt32(reader.GetOrdinal("id_services")),
                            name = reader.GetString(reader.GetOrdinal("name")),
                            additionalMaterialsPrice = reader.GetDecimal(reader.GetOrdinal("additonal_materials_price")),
                            tarif = reader.GetDecimal(reader.GetOrdinal("tarif")),
                            totalPrice = reader.GetDecimal(reader.GetOrdinal("total_price"))
                        });
                    }
                }
            }

            Services.ItemsSource = serviceList;
            Services.AutoGenerateColumns = false;
            Services.Columns.Clear();

            foreach (var column in columnHeadersServices)
            {
                Services.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Value,
                    Binding = new Binding(column.Key),
                    Width = DataGridLength.Auto
                });
            }
        }

        private void Addbtnclicked(object sender, RoutedEventArgs e)
        {
            SelectRecord();
        }

        private void Grid_Click(object sender, RoutedEventArgs e)
        {
            SelectRecord();
        }

        private void SelectRecord()
        {

            if (materialsVisible && Materials.SelectedItem is MaterialItem material)
            {
                int actualStock = 0;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT stock FROM Materials WHERE id_materials = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", material.id);
                        object result = cmd.ExecuteScalar();
                        if (result != null && int.TryParse(result.ToString(), out int stockFromDb))
                        {
                            actualStock = stockFromDb;
                        }
                        else
                        {
                            MessageBox.Show("Не удалось получить данные о наличии материала на складе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                string input = Interaction.InputBox(
                    $"Введите количество материала \"{material.name}\" для добавления (в наличии: {actualStock}):",
                    "Добавление материала",
                    "1"
                );

                if (int.TryParse(input, out int quantity) && quantity > 0)
                {
                    if (actualStock >= quantity)
                    {
                        if (MessageBox.Show($"Добавить {quantity} шт. \"{material.name}\" в список?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            if (!selectedMaterials.Any(m => m.id == material.id))
                            {
                                material.count = quantity;
                                selectedMaterials.Add(material);
                                SelectedItemsListBox.Items.Add($"Материал: {material.name} x{quantity} | Цена: {material.totalPrice * quantity} BYN");

                                using (SqlConnection conn = new SqlConnection(connectionString))
                                {
                                    conn.Open();
                                    using (SqlCommand cmd = new SqlCommand("UPDATE Materials SET stock = stock - @quantity WHERE id_materials = @id", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@quantity", quantity);
                                        cmd.Parameters.AddWithValue("@id", material.id);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("Этот материал уже добавлен в список.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Недостаточно материала на складе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Некорректное количество.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LoadMaterials();
                Materials.Items.Refresh();
            }

            else if (!materialsVisible && Services.SelectedItem is ServiceItem service)
            {
                if (MessageBox.Show($"Добавить \"{service.name}\" в список?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (!selectedServices.Any(s => s.id == service.id))
                    {
                        selectedServices.Add(service);
                        SelectedItemsListBox.Items.Add($"Услуга: {service.name} | Цена: {service.totalPrice} BYN");
                    }
                }
            }


        }

        private void Deletebtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItemsListBox.SelectedItem != null)
            {
                Delete_Record();
            }
            else
            {
                MessageBox.Show("Выберите запись которую надо удалить из списка выбранных!");
            }
        }

        private void SelectedItemsListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && SelectedItemsListBox.SelectedItem != null)
            {
                Delete_Record();
            } else
            {
                MessageBox.Show("Выберите запись которую надо удалить из списка выбранных!");
            }
        }

        private void Delete_Record()
        {
            if (SelectedItemsListBox.SelectedItem == null)
                return;

            string selectedText = SelectedItemsListBox.SelectedItem.ToString();

            if (selectedText.StartsWith("Материал:"))
            {
                string[] parts = selectedText.Split('|');
                string namePart = parts[0].Replace("Материал:", "").Trim(); 
                string[] nameAndQty = namePart.Split('x'); 

                string name = nameAndQty[0].Trim();
                int quantity = 1;

                if (nameAndQty.Length > 1 && int.TryParse(nameAndQty[1].Trim(), out int parsedQty))
                    quantity = parsedQty;

                var itemToRemove = selectedMaterials.FirstOrDefault(m => m.name == name);
                if (itemToRemove != null)
                {
                    selectedMaterials.Remove(itemToRemove);

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("UPDATE Materials SET stock = stock + @quantity WHERE id_materials = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@quantity", quantity);
                            cmd.Parameters.AddWithValue("@id", itemToRemove.id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                LoadMaterials();
                Materials.Items.Refresh();
            }
            else if (selectedText.StartsWith("Услуга:"))
            {
                string name = selectedText.Split('|')[0].Replace("Услуга:", "").Trim();
                var itemToRemove = selectedServices.FirstOrDefault(s => s.name == name);
                if (itemToRemove != null)
                    selectedServices.Remove(itemToRemove);
            }

            SelectedItemsListBox.Items.Remove(SelectedItemsListBox.SelectedItem);
        }



        private void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            var servicesToSave = new List<Platezh.Services.Service>();
            var materialsToSave = new List<Platezh.Services.Material>();

            foreach (var service in selectedServices)
            {
                servicesToSave.Add(new Platezh.Services.Service
                {
                    id = service.id,
                    name = service.name,
                    count = 1,
                    tariff = service.tarif,
                    additionalMaterialCost = service.additionalMaterialsPrice,
                    totalCost = service.totalPrice
                });
            }

            foreach (var material in selectedMaterials)
            {
                materialsToSave.Add(new Platezh.Services.Material
                {
                    id = material.id,
                    name = material.name,
                    nds = material.nds,
                    totalPrice = material.totalPrice,
                    count = material.count
                });
            }

                if (servicesToSave.Count == 0 && materialsToSave.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы одну услугу или материал перед формированием договора.");
                    return;
                }


            string folderPath = appSettings.LastContractFolderPath;

            if (folderPath != "")
            {

                    try
                    {
                        var contractData = new ContractData
                        {
                            contractNumber = decimal.TryParse(ContractNumberBox.Text, out var num) ? num : 0,
                            clientName = ClientNameBox.Text,
                            passportNumber = PassportNumberBox.Text,
                            issuedBy = IssuedByBox.Text,
                            dateIssued = DateIssueBox.SelectedDate,
                            address = AddresBox.Text
                        };

                        excelService.FillContract(servicesToSave, materialsToSave, contractData, folderPath);

                        MessageBox.Show("Договор успешно сформирован и сохранен.");

                        materialsToSave.Clear();
                        selectedMaterials.Clear();
                        servicesToSave.Clear();
                        selectedServices.Clear();
                        SelectedItemsListBox.Items.Clear();

                        ContractNumberBox.Clear();
                        ClientNameBox.Clear();
                        PassportNumberBox.Clear();
                        IssuedByBox.Clear();
                        AddresBox.Clear();
                        DateIssueBox.SelectedDate = null;



                }
                catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при формировании договора: " + ex.Message);
                    }
            } else
            {
                if (string.IsNullOrWhiteSpace(appSettings.LastContractFolderPath))
                {
                    var dialog = new VistaFolderBrowserDialog
                    {
                        Description = "Выберите папку для сохранения договоров",
                        UseDescriptionForTitle = true,
                        ShowNewFolderButton = true
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        appSettings.LastContractFolderPath = dialog.SelectedPath;
                        SettingsManager.SaveSettings(appSettings);
                        MessageBox.Show("Пусть сохранен. Нажмите повторно кнопку \"Сформировать договор\"");
                    }
                    else
                    {
                        MessageBox.Show("Папка для договоров не выбрана. Программа не сможет сохранять договоры.", "Внимание");
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (selectedMaterials.Any())
            {
                var result = MessageBox.Show(
                    "У вас есть неоформленные материалы. Закрыть и вернуть их на склад?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        foreach (var material in selectedMaterials)
                        {
                            var listItem = SelectedItemsListBox.Items
                                .OfType<string>()
                                .FirstOrDefault(i => i.StartsWith($"Материал: {material.name}"));

                            int quantity = 1; 
                            if (listItem != null)
                            {
                                var parts = listItem.Split('|')[0].Replace("Материал:", "").Trim().Split('x');
                                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int parsedQty))
                                    quantity = parsedQty;
                            }

                            using (SqlCommand cmd = new SqlCommand("UPDATE Materials SET stock = stock + @quantity WHERE id_materials = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@quantity", quantity);
                                cmd.Parameters.AddWithValue("@id", material.id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    selectedMaterials.Clear();
                }
                else
                {
                    e.Cancel = true; 
                }
            }
        }


        private void ShowContracts(object sender, RoutedEventArgs e)
        {
            string folderPath = appSettings.LastContractFolderPath;

            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Папка с договорами не найдена. Выберите папку через генерацию нового договора.");
            }
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку для сохранения договоров",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == true)
            {
                appSettings.LastContractFolderPath = dialog.SelectedPath;
                SettingsManager.SaveSettings(appSettings);

                MessageBox.Show($"Путь успешно сохранён:\n{appSettings.LastContractFolderPath}", "Готово");
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
            public int stock { get; set; }
            public int count { get; set; }
        }

        public class ServiceItem
        {
            public int id { get; set; }
            public string name { get; set; }
            public decimal tarif { get; set; }
            public decimal additionalMaterialsPrice { get; set; }
            public decimal totalPrice { get; set; }
        }

        public class ContractData
        {
            public decimal contractNumber { get; set; }
            public string clientName { get; set; }
            public string passportNumber { get; set; }
            public string issuedBy { get; set; }
            public DateTime? dateIssued { get; set; }
            public string address { get; set; }
        }


    }
}
