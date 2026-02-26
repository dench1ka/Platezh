using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Logging;
using Platezh.Views;
using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Platezh.Services;

namespace Platezh
{
    public partial class MainWindow : Window
    {
        private bool isPasswordVisible = false;
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;
        private List<RoleItem> roleList = new List<RoleItem>();
        public MainWindow()
        {
            InitializeComponent();
            LoadRolesAsync();
            Role.SelectedIndex = -1;
        }

        private async void LoadRolesAsync()
        {
            try
            {
                roleList = await RoleService.GetRolesAsync();

                Role.ItemsSource = roleList;
                Role.DisplayMemberPath = "RoleName";
                Role.SelectedValuePath = "RoleID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            if (isPasswordVisible)
            {
                passwordTextBox.Text = passwordBox.Password;
                passwordTextBox.Visibility = Visibility.Visible;
                passwordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                passwordBox.Password = passwordTextBox.Text;
                passwordTextBox.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;
            }
        }

        private async void Login(object sender, RoutedEventArgs e)
        {
            string login = loginTextBox.Text.Trim();
            string password = isPasswordVisible ? passwordTextBox.Text : passwordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните логин и пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Role.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите роль из выпадающего списка!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string hashedPassword = Utils.HashPassword(password);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT UserID, IsActive, RoleID FROM Users WHERE Login = @Login AND PasswordHash = @PasswordHash";
               

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int userId = reader.GetInt32(reader.GetOrdinal("UserID"));
                                bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                                int userRoleId = reader.GetInt32(reader.GetOrdinal("RoleID"));

                                if (!isActive)
                                {
                                    MessageBox.Show("Ваш аккаунт заблокирован или не активен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }

                                var roleItem = roleList.FirstOrDefault(r => r.RoleID == userRoleId);

                                if (roleItem == null)
                                {
                                    MessageBox.Show("Роль пользователя не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                string roleName = roleItem.RoleName;

                                if ((int)Role.SelectedValue != userRoleId)
                                {
                                    MessageBox.Show("Вы выбрали неверную роль для данного пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                Window nextWindow = roleName switch
                                {
                                    "Экономист" => new Economist(),
                                    "Кассир" => new Casher(userId),
                                    "Администратор" => new AdminWindow(),
                                    _ => throw new Exception("Неизвестная роль")
                                };

                                MessageBox.Show("Успешный вход!");
                                nextWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
