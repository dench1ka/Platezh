using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Configuration;
using System.Data;

namespace Platezh.Views
{
    /// <summary>
    /// Логика взаимодействия для AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {

        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;
        private List<UserItem> userList = new List<UserItem>();


        public AdminWindow()
        {
           
            InitializeComponent();
            LoadUsers();
        }




        private void Deactivate_Click(object sender, RoutedEventArgs e)
        {


        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void Activate_Click(object sender, RoutedEventArgs e)
        {

        }


        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //sdd
        }

        private void UsersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserItem selectedUser)
            {
                // Создаём и открываем окно редактирования пользователя
                EditUserWindow editWindow = new EditUserWindow(selectedUser);
                editWindow.ShowDialog();

                // После закрытия окна можно обновить список пользователей
                LoadUsers();
            }
        }



        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();

            UsersGrid.ItemsSource = userList
            .Where(m => m.login.ToLower().Contains(searchText))
            .ToList();
            
        }

        private void LoadUsers()
        {
            userList.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT UserID, Login, PasswordHash, RoleName, CreatedAt, IsActive FROM Users LEFT JOIN Role ON Users.RoleID = Role.RoleID", conn))
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

            UsersGrid.ItemsSource = userList;

        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {

            if (UsersGrid.SelectedItem is UserItem selectedUser)
            {
                // Создаём и открываем окно редактирования пользователя
                EditUserWindow editWindow = new EditUserWindow(selectedUser);
                editWindow.ShowDialog();

                // После закрытия окна можно обновить список пользователей
                LoadUsers();
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            Registration registration = new Registration();
            registration.ShowDialog();

            LoadUsers();
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
