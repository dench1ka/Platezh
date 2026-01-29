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
            LoadRoles();
        }

        private void LoadRoles()
        {
            roleList.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT RoleID, RoleName FROM Role", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roleList.Add(new RoleItem
                        {
                            RoleID = reader.GetInt32(reader.GetOrdinal("RoleID")),
                            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                        });
                    }
                }
            }

            // Вместо DataSource используем ItemsSource
            Role.ItemsSource = null;
            Role.ItemsSource = roleList;

            // Вместо DisplayMember используем DisplayMemberPath
            Role.DisplayMemberPath = "RoleName";

            // Вместо ValueMember используем SelectedValuePath
            Role.SelectedValuePath = "RoleID";
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

        // Хеширование пароля
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        // Вход в систему
        private async void Login(object sender, RoutedEventArgs e)
        {
            string login = loginTextBox.Text.Trim();
            string password = isPasswordVisible ? passwordTextBox.Text : passwordBox.Password;
            string hashedPassword = HashPassword(password);

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

                    string query = "SELECT RoleID FROM Users WHERE Login = @Login AND PasswordHash = @PasswordHash";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                        var result = await command.ExecuteScalarAsync();

                        if (result != null)
                        {
                            int userRoleId = Convert.ToInt32(result);

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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }



        // Переход к регистрации
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string adminpassword = Interaction.InputBox(
                $"Введите пароль администратора:",
                "Регистрация пользователя"
            );

            string hashadminpassword = HashPassword(adminpassword);
            int roleId = 3;
            string login = "admin";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT RoleID FROM Users WHERE Login = @Login AND PasswordHash = @PasswordHash";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@PasswordHash", hashadminpassword);

                        var result = await command.ExecuteScalarAsync();

                        if (result != null)
                        {
                            int dbRoleId = Convert.ToInt32(result);

                            if (dbRoleId == roleId)
                            {
                                MessageBox.Show("Успешный вход!");
                                Registration registration = new Registration();
                                registration.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Вы не являетесь администратором!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Неверный пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RoleItem
    {
        public int RoleID {  get; set; }
        public string RoleName { get; set; }
    }
}
