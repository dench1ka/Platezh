using System.Collections.Generic;
using Microsoft.Data.SqlClient; 
using System.Threading.Tasks;
using System.Configuration;

namespace Platezh.Services
{
    /// <summary>
    /// Сервис для работы с ролями
    /// </summary>
    public static class RoleService
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["PlatezhDB"].ConnectionString;

        /// <summary>
        /// Загружает все роли из базы и возвращает их списком
        /// </summary>
        public static async Task<List<RoleItem>> GetRolesAsync()
        {
            var roleList = new List<RoleItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT RoleID, RoleName FROM Role";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roleList.Add(new RoleItem
                        {
                            RoleID = reader.GetInt32(reader.GetOrdinal("RoleID")),
                            RoleName = reader.GetString(reader.GetOrdinal("RoleName"))
                        });
                    }
                }
            }

            return roleList;
        }
    }

    /// <summary>
    /// Класс для хранения информации о роли
    /// </summary>
    public class RoleItem
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; }
    }
}
