using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Oracle.DataAccess.Client; 

namespace SWE_project
{
    public partial class Form3 : Form
    {
        private int _userId;
        private readonly string connectionString = "Data Source = ORCL1 ; User Id =hr ; Password = hr";

        private List<int> _diaryIdsForSelectedDate = new List<int>();

       
        public Form3(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            if (_userId == 0)
            {
                MessageBox.Show("User not identified. Returning to main screen.");
                GoBackToForm2();
                return;
            }
            LoadDates();
        }

    
        private void LoadDates()
        {
            comboBox2.Items.Clear();
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    string sql = "SELECT DISTINCT TRUNC(created_at) FROM app_diaries WHERE user_id = :id ORDER BY TRUNC(created_at) DESC";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add(":id", _userId);

                    conn.Open();
                    OracleDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        comboBox2.Items.Add(reader.GetDateTime(0).ToShortDateString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dates: " + ex.Message);
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            _diaryIdsForSelectedDate.Clear();
            textBox1.Clear();

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                  
                    string sql = "SELECT diary_id FROM app_diaries WHERE user_id = :id AND TRUNC(created_at) = TO_DATE(:dt, 'MM/DD/YYYY') ORDER BY created_at ASC";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add(":id", _userId);
                    cmd.Parameters.Add(":dt", comboBox2.SelectedItem.ToString());

                    conn.Open();
                    OracleDataReader reader = cmd.ExecuteReader();
                    int count = 1;
                    while (reader.Read())
                    {
                        _diaryIdsForSelectedDate.Add(Convert.ToInt32(reader["diary_id"]));
                        comboBox1.Items.Add(count.ToString());
                        count++;
                    }
                }

                if (comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading entries for date: " + ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1) return;

            int selectedDbId = _diaryIdsForSelectedDate[comboBox1.SelectedIndex];

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    string sql = "SELECT diary_text FROM app_diaries WHERE diary_id = :id";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add(":id", selectedDbId);

                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    textBox1.Text = result?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading diary text: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1) return;

            int selectedDbId = _diaryIdsForSelectedDate[comboBox1.SelectedIndex];

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    string sql = "UPDATE app_diaries SET diary_text = :txt WHERE diary_id = :id";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add(":txt", OracleDbType.Clob).Value = textBox1.Text;
                    cmd.Parameters.Add(":id", selectedDbId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Diary updated successfully!");
                    GoBackToForm2();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: " + ex.Message);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1) return;

            var confirm = MessageBox.Show("Are you sure you want to delete this entry?", "Confirm", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;

            int selectedDbId = _diaryIdsForSelectedDate[comboBox1.SelectedIndex];

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    string sql = "DELETE FROM app_diaries WHERE diary_id = :id";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add(":id", selectedDbId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Diary deleted.");
                    GoBackToForm2();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting: " + ex.Message);
            }
        }

        private void GoBackToForm2()
        {
         
            Form2 f2 = (Form2)Application.OpenForms["Form2"];
            if (f2 == null)
            {
                f2 = new Form2(_userId, "User"); 
            }

            f2.Show();
            this.Close();
        }

        // Unused handlers
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
    }
}