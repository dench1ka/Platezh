using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using System.Configuration;
// Замените на ваши пространства имен, если они отличаются
using static Platezh.Views.Casher;

namespace Platezh.Views
{
    public partial class EditClientWindow : Window
    {
        private ClientItem _client;

        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;
        // Флаг, который скажет главному окну, что данные изменились и таблицу нужно обновить
        public bool DataChanged { get; private set; } = false;

        public EditClientWindow(ClientItem client)
        {
            InitializeComponent();
            _client = client;

            // Заполняем поля данными клиента при открытии окна
            PopulateFields();
        }

        private void PopulateFields()
        {
            SurnameBox.Text = _client.Surname;
            NameBox.Text = _client.Name;
            FatherNameBox.Text = _client.FatherName;
            PassportDataBox.Text = _client.Passport;
            IssuedByBox.Text = _client.IssuedBy;
            DateIssued.SelectedDate = _client.IssuedDate;
            AddressBox.Text = _client.Address;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SurnameBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Фамилия и Имя обязательны для заполнения!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE Clients 
                                   SET Name = @name, Surname = @surname, FatherName = @father, 
                                       PassportNumber = @pass, IssuedBy = @issued, Address = @addr, IssuedDate = @issuedDate 
                                   WHERE ClientID = @id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@surname", SurnameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@father", FatherNameBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@pass", PassportDataBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@issued", IssuedByBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@addr", AddressBox.Text.Trim());

                        // Обработка возможного null для даты
                        if (DateIssued.SelectedDate.HasValue)
                            cmd.Parameters.AddWithValue("@issuedDate", DateIssued.SelectedDate.Value);
                        else
                            cmd.Parameters.AddWithValue("@issuedDate", DBNull.Value);

                        cmd.Parameters.AddWithValue("@id", _client.id);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Данные клиента успешно обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DataChanged = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}