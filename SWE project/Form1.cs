using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace SWE_project
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = "Data Source = ORCL1 ; User Id =hr  ; Password = hr";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) { }


        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string passwordHash = ComputeSha256Hash(password);

            try
            {
                using (var conn = new OracleConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.BindByName = true;
                    cmd.CommandText = "SELECT user_id FROM app_users WHERE username = :p_user AND password_hash = :p_hash";
                    cmd.Parameters.Add(new OracleParameter("p_user", OracleDbType.Varchar2)).Value = username;
                    cmd.Parameters.Add(new OracleParameter("p_hash", OracleDbType.Varchar2)).Value = passwordHash;

                    conn.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        int userId = OracleValueToInt(result);

                        var f2 = new Form2(userId, username);
                        f2.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (OracleException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string passwordHash = ComputeSha256Hash(password);

            try
            {
                using (var conn = new OracleConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.BindByName = true;
                    cmd.CommandType = System.Data.CommandType.Text;

                   
                    cmd.CommandText = @"
                INSERT INTO app_users (user_id, username, password_hash, created_at)
                VALUES (app_users_seq.NEXTVAL, :p_username, :p_password, SYSDATE)
                RETURNING user_id INTO :p_new_userid";

                    cmd.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2)).Value = username;
                    cmd.Parameters.Add(new OracleParameter("p_password", OracleDbType.Varchar2)).Value = passwordHash;

                    var outParam = new OracleParameter("p_new_userid", OracleDbType.Int32);
                    outParam.Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(outParam);

                    cmd.ExecuteNonQuery();

                    int newUserId = OracleValueToInt(outParam.Value);
                    MessageBox.Show($"Registration successful. Your user id is {newUserId}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    var f2 = new Form2(newUserId, username);
                    f2.Show();
                    this.Hide();
                }
            }
            catch (OracleException ex)
            {
                
                if (ex.Number == 1) 
                {
                    MessageBox.Show("Username already exists. Choose a different username.", "Registration Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    


        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private int OracleValueToInt(object oracleValue)
        {
            if (oracleValue == null || oracleValue == DBNull.Value) return 0;
            if (oracleValue is OracleDecimal od) return od.ToInt32();
            return Convert.ToInt32(oracleValue);
        }
    }
}
