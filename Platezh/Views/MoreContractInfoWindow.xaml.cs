using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.SqlClient;
using Platezh.Services;
using Microsoft.VisualBasic;
using System.Configuration;
using Microsoft.Win32;


namespace Platezh.Views
{
    /// <summary>
    /// Логика взаимодействия для MoreContractInfoWindow.xaml
    /// </summary>
    public partial class MoreContractInfoWindow : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        private List<MoreInfoHistory> morehistoryList = new List<MoreInfoHistory>();

        private int _selectItem;
        public MoreContractInfoWindow(int selectItem)
        {
            InitializeComponent();
            _selectItem = selectItem;
            LoadDataIntoFields();
        }

        private void LoadDataIntoFields()
        {
            morehistoryList.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"select co.ContractNumber as con, co.ContractDate as cod, co.TotalAmount as ta, co.Status as cost, ci.ItemType as ciit, ci.Quantity as ciq, ci.Total as cit, ci.MaterialName as cim, ci.ServiceName as cis, c.Name as cn, c.Surname as cs, c.FatherName as cf, u.Login as ul from Contracts  as co
                    LEFT JOIN Clients as c on co.ClientID = c.ClientID
                    LEFT JOIN Users as u on co.CreatedBy = u.UserID
                    Left JOIN ContractItems as ci on co.ContractID = ci.ContractID where co.ContractID = " + Convert.ToString(_selectItem);



                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string lineItemName, surname, name, fatherName, contractNumber, status, createdBy;
                        if (reader.GetString(reader.GetOrdinal("ul")) == null)
                        {
                            lineItemName = reader.GetString(reader.GetOrdinal("ul"));
                            }
                        else
                        {
                            lineItemName = reader.GetString(reader.GetOrdinal("ul"));
                        }

                        surname = reader.GetString(reader.GetOrdinal("cs"));
                        name = reader.GetString(reader.GetOrdinal("cn"));
                        fatherName = reader.GetString(reader.GetOrdinal("cf"));
                        contractNumber = reader.GetString(reader.GetOrdinal("con"));
                        status = reader.GetString(reader.GetOrdinal("cost"));
                        createdBy = reader.GetString(reader.GetOrdinal("ul"));

                        DateTime contractDate;
                        contractDate = reader.GetDateTime(reader.GetOrdinal("cod"));

                        decimal total;
                        total = reader.GetDecimal(reader.GetOrdinal("ta"));

                        LabelNumberContract.Content = "Номер договора: " + contractNumber;
                        LabelFullName.Content = "ФИО клиента: " + surname + " " + name + " " + fatherName;
                        LabelUserName.Content = "Кассир: " + createdBy;
                        LabelTotalPrice.Content = "Итоговая стоимость: " + total;
                        LabelStatus.Content = "Сатус оплаты: " + status;
                        LabelContractDate.Content = "Дата составления договора: " + contractDate;


                        morehistoryList.Add(new MoreInfoHistory
                        {
                            ItemType = reader.GetString(reader.GetOrdinal("ciit")),
                            Quantity = reader.GetDecimal(reader.GetOrdinal("ciq")),
                            TotalPriceItem = reader.GetDecimal(reader.GetOrdinal("cit")),
                            ItemName = lineItemName
                        });
                    }
                }
            }

            var headers = new Dictionary<string, string>
            {
                { "ItemName", "Название элемента" },
                { "ItemType", "Тип элемента" },
                { "Quantity", "Количество" },
                { "TotalPriceItem", "Стоимость" },
             
            };

            CreateColumns(headers);
            MainGrid.ItemsSource = morehistoryList.ToList();
        
        }

        private void CreateColumns(Dictionary<string, string> headers)
        {
            MainGrid.Columns.Clear();
            foreach (var h in headers)
            {
                var col = new DataGridTextColumn
                {
                    Header = h.Value,
                    Binding = new Binding(h.Key),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };

                if (h.Key.Contains("Valid") || h.Key.Contains("Date"))
                    col.Binding.StringFormat = "dd.MM.yyyy";

                MainGrid.Columns.Add(col);
            }
        }

        public class MoreInfoHistory
        {
            public string ItemType { get; set; } = "";
            public decimal Quantity { get; set; }
            public decimal TotalPriceItem { get; set; }
            public string ItemName { get; set; } = "";
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
