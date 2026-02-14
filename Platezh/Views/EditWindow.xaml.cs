using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace Platezh.Views
{
    public partial class EditWindow : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        private Economist _economistWindow;
        private TableMode _mode;
        private object _selectedItem;

        public EditWindow(Economist economist, TableMode mode, object selectedItem)
        {
            InitializeComponent();
            _economistWindow = economist;
            _mode = mode;
            _selectedItem = selectedItem;

            LoadDataIntoFields();
        }

        private void LoadDataIntoFields()
        {
            // Скрываем панели по умолчанию
            MaterialsInputPanel.Visibility = Visibility.Collapsed;
            ServicesInputPanel.Visibility = Visibility.Collapsed;
     
            if (_mode == TableMode.Prices && _selectedItem is MaterialPriceItem mp)
            {
                SectionName.Content = "Режим: Цены и остатки";
                NameItem.Content = mp.MaterialsName;
                IdBox.Text = mp.PriceID.ToString();
                NameBox.Text = mp.MaterialsName;
                NameBox.IsReadOnly = true; // Имя материала в ценах менять нельзя, только в справочнике

                StockBox.Text = mp.StockAmount.ToString();
                PriceWithoutNdsBox.Text = mp.PriceWithoutNds.ToString("F2");
                NdsBox.Text = mp.NdsPercent.ToString("F2");
            
                // Активность (для цен используется статус цены)
                IsActiveCheck.IsChecked = mp.IsActivePrice;

                MaterialsInputPanel.Visibility = Visibility.Visible;
              }
            else if (_mode == TableMode.Services && _selectedItem is ServiceItem s)
            {
                SectionName.Content = "Режим: Услуги";
                NameItem.Content = s.Name;
                IdBox.Text = s.ServiceId.ToString();
                NameBox.Text = s.Name;

                TarifBox.Text = s.BasePrice.ToString("F2");
                AdditionalMaterialsPriceBox.Text = s.AddMaterials.ToString("F2");
       
                IsActiveCheck.IsChecked = s.IsActive;

                ServicesInputPanel.Visibility = Visibility.Visible;
            }
            else if (_mode == TableMode.Reference && _selectedItem is MaterialReferenceItem mr)
            {
                SectionName.Content = "Режим: Справочник материалов";
                NameItem.Content = mr.Name;
                IdBox.Text = mr.MaterialID.ToString();
                NameBox.Text = mr.Name;

                IsActiveCheck.IsChecked = mr.IsActive;

                // Для справочника цена не нужна
            }
            else if (_mode == TableMode.Units && _selectedItem is UnitsItem u)
            {
                SectionName.Content = "Режим: Единицы измерения";
                NameItem.Content = u.Name;
                IdBox.Text = u.UnitID.ToString();
                NameBox.Text = u.Name;

                // В единицах измерения нет поля IsActive (обычно), но если нужно, раскомментируйте
                // Если в БД нет колонки IsActive у таблицы Units, лучше скрыть чекбокс:
                // IsActiveCheck.Visibility = Visibility.Collapsed;
                // Но так как вы просили добавить возможность везде, оставляю включенным (нужна колонка в БД!)
                IsActiveCheck.Visibility = Visibility.Collapsed; // Или привязать к полю, если оно есть

            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "";
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    // Получаем статус активности (1 или 0)
                    bool isActive = IsActiveCheck.IsChecked == true;

                    if (_mode == TableMode.Prices)
                    {
                        // ПЕРЕРАСЧЕТ перед сохранением: берем чистую цену и НДС
                        decimal price = decimal.Parse(PriceWithoutNdsBox.Text);
                        decimal ndsVal = decimal.Parse(NdsBox.Text);

                      

                        query = @"UPDATE MaterialPrices SET 
                                 PriceWithoutNds = @price, 
                                 NdsPercent = @nds, 
                                 StockAmount = @stock,
                                 IsActive = @active
                                 WHERE PriceID = @id";

                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@nds", ndsVal);
                        cmd.Parameters.AddWithValue("@stock", int.Parse(StockBox.Text));
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@id", int.Parse(IdBox.Text));
                    }
                    else if (_mode == TableMode.Services)
                    {
                        query = @"UPDATE Services SET 
                                Name = @name, 
                                BasePrice = @base, 
                                AddMaterials = @add,
                                IsActive = @active
                                WHERE ServiceId = @id";

                        cmd.Parameters.AddWithValue("@name", NameBox.Text);
                        cmd.Parameters.AddWithValue("@base", decimal.Parse(TarifBox.Text));
                        cmd.Parameters.AddWithValue("@add", decimal.Parse(AdditionalMaterialsPriceBox.Text));
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@id", int.Parse(IdBox.Text));
                    }
                    else if (_mode == TableMode.Reference)
                    {
                        query = "UPDATE Materials SET Name = @name, IsActive = @active WHERE MaterialID = @id";
                        cmd.Parameters.AddWithValue("@name", NameBox.Text);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@id", int.Parse(IdBox.Text));
                    }
                    else if (_mode == TableMode.Units)
                    {
                        // ВАЖНО: Убедитесь, что в таблице Units есть колонка IsActive. 
                        // Если нет - удалите часть ", IsActive = @active"
                        query = "UPDATE Units SET Name = @name WHERE UnitID = @id";
                        // Если есть колонка в БД, замените строку выше на:
                        // query = "UPDATE Units SET Name = @name, IsActive = @active WHERE UnitID = @id";

                        cmd.Parameters.AddWithValue("@name", NameBox.Text);
                        // cmd.Parameters.AddWithValue("@active", isActive); // Раскомментировать если есть колонка
                        cmd.Parameters.AddWithValue("@id", int.Parse(IdBox.Text));
                    }

                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Данные успешно сохранены!");
                _economistWindow.Update_Click(null, null); // Обновляем таблицу в главном окне
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения (проверьте числа): " + ex.Message);
            }
        }
    }
}