using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;

namespace Platezh.Views
{
    public partial class AdminWindow : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        private ObservableCollection<UserItem> userList = new ObservableCollection<UserItem>();

        public AdminWindow()
        {
            InitializeComponent();
            UsersGrid.ItemsSource = userList;
            LoadUsers();
        }

        private void LoadUsers()
        {
            userList.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT UserID, Login, PasswordHash, RoleName, CreatedAt, IsActive FROM Users LEFT JOIN Role ON Users.RoleID = Role.RoleID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userList.Add(new UserItem
                        {
                            id = reader.GetInt32(reader.GetOrdinal("UserID")),
                            login = reader.GetString(reader.GetOrdinal("Login")),
                            passwordhash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                            roleName = reader.GetString(reader.GetOrdinal("RoleName")),
                            createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            isActive = reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Активен" : "Неактивен"
                        });
                    }
                }
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserItem selectedUser)
            {
                EditUserWindow editWindow = new EditUserWindow(selectedUser);
                editWindow.ShowDialog();
                LoadUsers();
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            Registration registration = new Registration();
            registration.ShowDialog();
            LoadUsers();              
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            UsersGrid.ItemsSource = userList
                .Where(m => m.login.ToLower().Contains(searchText))
                .ToList();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not UserItem selectedUser)
            {
                MessageBox.Show("Выберите пользователя для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
  
            var result = MessageBox.Show($"Вы действительно хотите удалить пользователя '{selectedUser.login}'?",
                                         "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string deleteQuery = "DELETE FROM Users WHERE UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", selectedUser.id);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Пользователь успешно удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUsers();
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден или уже удалён.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }

    public class UserItem
    {
        public int id { get; set; }
        public string login { get; set; }
        public string passwordhash { get; set; }
        public string roleName { get; set; }
        public DateTime createdAt { get; set; }
        public string isActive { get; set; }
    }
}
