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
        private List<RoleItem> roleList = new List<RoleItem>();
        public Registration()
        {
            InitializeComponent();
            LoadRolesAsync();
        }

        private async void LoadRolesAsync()
        {
            try
            {
                // Получаем роли через сервис
                roleList = await RoleService.GetRolesAsync();

                // Привязываем к ComboBox
                cmbRole.ItemsSource = roleList;
                cmbRole.DisplayMemberPath = "RoleName";
                cmbRole.SelectedValuePath = "RoleID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string password = txtPassword.Password;

            // Получаем выбранный объект роли
            var selectedRole = cmbRole.SelectedItem as RoleItem;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || selectedRole == null)
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

                    // Проверка, есть ли уже пользователь с таким логином
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

                    // Вставка нового пользователя
                    string insertQuery = @"INSERT INTO dbo.Users 
                                   (Login, PasswordHash, RoleID, CreatedAt, IsActive) 
                                   VALUES (@Login, @PasswordHash, @RoleID, @CreatedAt, @IsActive)";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@Login", login);
                        insertCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@RoleID", selectedRole.RoleID);
                        insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                        insertCmd.Parameters.AddWithValue("@IsActive", 1);

                        insertCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
    }
}
