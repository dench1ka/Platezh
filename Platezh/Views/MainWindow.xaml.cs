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
        }

        private async void LoadRolesAsync()
        {
            try
            {
                // Получаем роли через сервис
                roleList = await RoleService.GetRolesAsync();

                // Привязываем к ComboBox
                Role.ItemsSource = roleList;
                Role.DisplayMemberPath = "RoleName";
                Role.SelectedValuePath = "RoleID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Переключение видимости пароля
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

        // Вход в систему
        private async void Login(object sender, RoutedEventArgs e)
        {
            string login = loginTextBox.Text.Trim();
            string password = isPasswordVisible ? passwordTextBox.Text : passwordBox.Password;

            string hashedPassword = Utils.HashPassword(password);

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните логин и пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT IsActive, RoleID FROM Users WHERE Login = @Login AND PasswordHash = @PasswordHash";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                                int userRoleId = reader.GetInt32(reader.GetOrdinal("RoleID"));

                                if (!isActive)
                                {
                                    MessageBox.Show("Ваш аккаунт заблокирован или не активен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }

                                // Находим объект роли в загруженном списке
                                var roleItem = roleList.FirstOrDefault(r => r.RoleID == userRoleId);

                                if (roleItem == null)
                                {
                                    MessageBox.Show("Роль пользователя не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                string roleName = roleItem.RoleName;

                                // Если используешь ComboBox для выбора роли
                                if (Role.SelectedValue != null && (int)Role.SelectedValue != userRoleId)
                                {
                                    MessageBox.Show("Вы выбрали неверную роль для данного пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                // Открываем окно по названию роли
                                Window nextWindow = roleName switch
                                {
                                    "Экономист" => new Economist(),
                                    "Кассир" => new Casher(),
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
