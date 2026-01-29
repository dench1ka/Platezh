using System;
using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Platezh.Services;

namespace Platezh.Views
{
    public partial class EditUserWindow : Window
    {
        private UserItem user; // текущий редактируемый пользователь
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        // Конструктор без параметров (для дизайнеров WPF)
        public EditUserWindow()
        {
            InitializeComponent();
        }

        // Новый конструктор с UserItem
        public EditUserWindow(UserItem selectedUser)
        {
            InitializeComponent();
            user = selectedUser;

            // Заполняем поля окна данными пользователя
            LoginTextBox.Text = user.login;
            IsActiveCheckBox.IsChecked = user.isActive == "Активен";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newLogin = LoginTextBox.Text.Trim();
            string newPassword = PasswordBox.Password.Trim();
            bool isActive = IsActiveCheckBox.IsChecked ?? false;

            if (string.IsNullOrEmpty(newLogin))
            {
                MessageBox.Show("Логин не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = !string.IsNullOrEmpty(newPassword)
                        ? "UPDATE Users SET Login = @Login, PasswordHash = @PasswordHash, IsActive = @IsActive WHERE UserID = @UserID"
                        : "UPDATE Users SET Login = @Login, IsActive = @IsActive WHERE UserID = @UserID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Login", newLogin);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UserID", user.id);

                        if (!string.IsNullOrEmpty(newPassword))
                            cmd.Parameters.AddWithValue("@PasswordHash", Utils.HashPassword(newPassword));

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Данные пользователя успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

       
    }
}
