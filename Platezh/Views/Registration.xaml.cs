using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using Microsoft.VisualBasic;
using Platezh.Services;


namespace Platezh.Views
{
    public partial class Registration : Window
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string password = txtPassword.Password;
            string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string hashedPassword = Utils.HashPassword(password);

            string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(*) FROM dbo.Users WHERE Login = @Login";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@Login", login);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            MessageBox.Show("Пользователь с таким именем уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    string insertQuery = "INSERT INTO dbo.Users (Login, PasswordHash, RoleID, CreatedAt, IsActive) VALUES (@Login, @PasswordHash, @RoleID, @CreatedAt, @IsActive)";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                    {
                        DateTime createdAt = DateTime.UtcNow;

                        insertCmd.Parameters.AddWithValue("@Login", login);
                        insertCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@RoleID", role == "Экономист" ? 1 : 2);
                        insertCmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                        insertCmd.Parameters.AddWithValue("@IsActive", 1);

                        insertCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                AdminWindow adminWindow = new AdminWindow();   
                adminWindow.Show(); 
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AdminWindow adminWindow = new AdminWindow();
            adminWindow.Show();
            this.Close();
        }

        
    }
}
