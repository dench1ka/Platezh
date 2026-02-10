using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Microsoft.Data.SqlClient;
using System.Configuration;

namespace Platezh.Views
{
    public partial class EditWindow : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;
        public bool materialsVisible;
        private bool NdsChoice;

        private Economist economistWindow;

        public EditWindow(Economist economist)
        {
            //InitializeComponent();
            //economistWindow = economist;
            //CalcChoice.SelectedIndex = 0;
            //NdsComboBox.SelectedIndex = 0;
            //if (materialsVisible)
            //{
            //    NdsComboBox.SelectedIndex = 0;
            //}
        }


        public void UpdateVisibility()
        {
            //MaterialsInputPanel.Visibility = materialsVisible ? Visibility.Visible : Visibility.Collapsed;
            //CalcChoice.Visibility = materialsVisible ? Visibility.Visible : Visibility.Collapsed;
            //ServicesInputPanel.Visibility = !materialsVisible ? Visibility.Visible : Visibility.Collapsed;
            //CalcChoice.SelectedIndex = !materialsVisible ? 0 : 0;
        }

        private void ClearFields()
        {
            //IdBox.Clear();
            //NameBox.Clear();
            //StockBox.Clear();
            //PriceWithoutNdsBox.Clear();
            //NdsBox.Clear();
            //TarifBox.Clear();
            //AdditionalMaterialsPriceBox.Clear();
        }

        private void CalcPrices()
        {
            //decimal totalPrice = Convert.ToDecimal(TotalPriceBox.Text);
            //if (materialsVisible)
            //{
            //    decimal priceWithoutNds = Convert.ToDecimal(PriceWithoutNdsBox.Text);
            //    if (CalcChoice.SelectedIndex == 0)
            //    {
            //        totalPrice = 0;
            //        decimal nds = Convert.ToDecimal(NdsBox.Text);
            //        if (NdsComboBox.SelectedIndex == 0)
            //        {
            //            totalPrice = Math.Round(priceWithoutNds + (priceWithoutNds * (nds / 100)), 2);
            //        }
            //        else if (NdsComboBox.SelectedIndex == 1)
            //        {
            //            totalPrice = Math.Round(priceWithoutNds + nds, 2);
            //        }

            //        TotalPriceBox.Text = totalPrice.ToString();
            //    }
            //    else 
            //    {
            //        priceWithoutNds = 0;
            //        decimal nds = Convert.ToDecimal(NdsBox.Text);

            //        if (NdsComboBox.SelectedIndex == 0)
            //        {
            //            priceWithoutNds = totalPrice / (1 + (nds / 100));
            //        }
            //        else if (NdsComboBox.SelectedIndex == 1)
            //        {
            //            priceWithoutNds = totalPrice - nds;
            //        }
            //        PriceWithoutNdsBox.Text = priceWithoutNds.ToString();

            //    }
            //}
            //else 
            //{
            //    decimal addMP = Convert.ToDecimal(AdditionalMaterialsPriceBox.Text);
            //    decimal tarif = Convert.ToDecimal(TarifBox.Text);
            //    totalPrice = tarif + addMP;
            //    TotalPriceBox.Text = totalPrice.ToString();
            //}

        }

        private void CalcilationBtn_Click(object sender, RoutedEventArgs e)
        {
            //CalcPrices();
        }

        public void SaveChanges()
        {
            //if (materialsVisible)
            //{
            //    int id = Convert.ToInt32(IdBox.Text);
            //    string name = NameBox.Text;
            //    int stockCount = Convert.ToInt32(StockBox.Text);
            //    decimal priceWithoutNds = Convert.ToDecimal(PriceWithoutNdsBox.Text);
            //    decimal nds = Convert.ToDecimal(NdsBox.Text);
            //    decimal totalPrice = Convert.ToDecimal(TotalPriceBox.Text);
            //    decimal ndsByn = 0, ndsPercent = 0;
            //    if (NdsComboBox.SelectedIndex == 0)
            //    {
            //        ndsPercent = nds;
            //        ndsByn = priceWithoutNds * (ndsPercent / 100);
            //    }
            //    else if (NdsComboBox.SelectedIndex == 1)
            //    {
            //        ndsByn = nds;
            //        ndsPercent = (nds / priceWithoutNds) * 100;
            //    }

            //    using (SqlConnection conn = new SqlConnection(connectionString))
            //    {
            //        conn.Open();
            //        string query = "UPDATE Materials SET name = @name, price_without_nds = @priceWithoutNds, stock = @stock, nds = @nds, nds_percent = @ndspercent, total_price = @totalPrice WHERE id_materials = @id";

            //        using (SqlCommand cmd = new SqlCommand(query, conn))
            //        {
            //            cmd.Parameters.AddWithValue("@id", id);
            //            cmd.Parameters.AddWithValue("@name", name);
            //            cmd.Parameters.AddWithValue("@priceWithoutNds", priceWithoutNds);
            //            cmd.Parameters.AddWithValue("@stock", stockCount);
            //            cmd.Parameters.AddWithValue("@nds", ndsByn);
            //            cmd.Parameters.AddWithValue("@ndspercent", ndsPercent);
            //            cmd.Parameters.AddWithValue("@totalPrice", totalPrice);
            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //    economistWindow.Materials.CommitEdit(DataGridEditingUnit.Row, true);
            //    economistWindow.Materials.CommitEdit();
            //    economistWindow?.Update_Table();
            //}
            //else
            //{
            //    int id = Convert.ToInt32(IdBox.Text);
            //    string name = NameBox.Text;
            //    decimal tarif = Convert.ToDecimal(TarifBox.Text);
            //    decimal additionalMaterialsPrice = Convert.ToDecimal(AdditionalMaterialsPriceBox.Text);
            //    decimal totalPrice = Convert.ToDecimal(TotalPriceBox.Text);

            //    using (SqlConnection conn = new SqlConnection(connectionString))
            //    {
            //        conn.Open();
            //        string query = "UPDATE Services SET name = @name, tarif = @tarif, additonal_materials_price = @additonal_materials_price, total_price = @totalPrice WHERE id_services = @id";

            //        using (SqlCommand cmd = new SqlCommand(query, conn))
            //        {
            //            cmd.Parameters.AddWithValue("@id", id);
            //            cmd.Parameters.AddWithValue("@name", name);
            //            cmd.Parameters.AddWithValue("@tarif", tarif);
            //            cmd.Parameters.AddWithValue("@additonal_materials_price", additionalMaterialsPrice);
            //            cmd.Parameters.AddWithValue("@totalPrice", totalPrice);
            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //    economistWindow.Services.CommitEdit(DataGridEditingUnit.Row, true);
            //    economistWindow.Services.CommitEdit();
            //    economistWindow?.Update_Table();



            //}
            //ClearFields();
            //this.Close();
        }
        private void NdsChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (materialsVisible)
            //{
            //    using (SqlConnection conn = new SqlConnection(connectionString))
            //    {
            //        conn.Open();
            //        using (SqlCommand cmd = new SqlCommand("SELECT nds, nds_percent FROM Materials WHERE id_materials = @id", conn))
            //        {
            //            cmd.Parameters.AddWithValue("@id", IdBox.Text);
            //            using (SqlDataReader reader = cmd.ExecuteReader())
            //            {
            //                if (reader.Read())
            //                {
            //                    if (NdsComboBox.SelectedIndex == 0)
            //                    {
            //                        NdsBox.Text = reader["nds_percent"].ToString();
            //                    }
            //                    else if (NdsComboBox.SelectedIndex == 1)
            //                    {
            //                        NdsBox.Text = reader["nds"].ToString();
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            //SaveChanges();
        }
    }
}