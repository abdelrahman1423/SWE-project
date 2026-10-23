// Form2.cs
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace SWE_project
{
    public partial class Form2 : Form
    {
       
        private readonly string connectionString = "Data Source = ORCL1 ; User Id =hr  ; Password = hr";
        private int _userId;
        private string _username;
        private string _attachmentPath;
        private string _attachmentName;

      
        public Form2()
        {
            InitializeComponent();
            this.Load += Form2_Load;

           
            if (this.Controls.ContainsKey("button1")) ((Button)this.Controls["button1"]).Click += button1_Click;
            if (this.Controls.ContainsKey("button2")) ((Button)this.Controls["button2"]).Click += button2_Click;
            if (this.Controls.ContainsKey("button3")) ((Button)this.Controls["button3"]).Click += button3_Click_1;
            if (this.Controls.ContainsKey("dataGridView1")) ((DataGridView)this.Controls["dataGridView1"]).CellContentClick += dataGridView1_CellContentClick;
            if (this.Controls.ContainsKey("comboBox1")) ((ComboBox)this.Controls["comboBox1"]).SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        
        public Form2(int userId, string username) : this()
        {
            InitializeForUser(userId, username);
        }

       
        public void InitializeForUser(int userId, string username)
        {
            _userId = userId;
            _username = username;
            this.Text = $"Diary - {username}";
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            InitializeMoodComboBox();
            if (_userId != 0) LoadDiaries();
        }

        private void InitializeMoodComboBox()
        {
            if (!this.Controls.ContainsKey("comboBox1")) return;
            var cb = (ComboBox)this.Controls["comboBox1"];
            cb.Items.Clear();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Items.Add("Sad");
            cb.Items.Add("Happy");
            cb.Items.Add("Average");
            cb.SelectedIndex = 2; 
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Choose attachment (optional)";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _attachmentPath = ofd.FileName;
                    _attachmentName = Path.GetFileName(_attachmentPath);

                    if (this.Controls.ContainsKey("lblFileName"))
                        ((Label)this.Controls["lblFileName"]).Text = _attachmentName;
                    else if (this.Controls.ContainsKey("textBox2"))
                        ((TextBox)this.Controls["textBox2"]).Text = _attachmentName;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string diaryText = this.Controls.ContainsKey("textBox1") ? ((TextBox)this.Controls["textBox1"]).Text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(diaryText))
            {
                MessageBox.Show("Please enter diary text.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string mood = "";
            if (this.Controls.ContainsKey("comboBox1"))
            {
                var cb = (ComboBox)this.Controls["comboBox1"];
                mood = cb.SelectedItem?.ToString() ?? "";
            }

            try
            {
                using (var conn = new OracleConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.BindByName = true;

                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"INSERT INTO app_diaries 
                                (diary_id, user_id, diary_text, attachment, attachment_name, mood, created_at) 
                                VALUES 
                                (app_diaries_seq.NEXTVAL, :p_user_id, :p_text, :p_attachment, :p_att_name, :p_mood, SYSDATE) 
                                RETURNING diary_id INTO :p_new_diaryid";

                  
                    cmd.Parameters.Add(new OracleParameter("p_user_id", OracleDbType.Int32)).Value = _userId;

                    var pText = new OracleParameter("p_text", OracleDbType.Clob) { Value = diaryText };
                    cmd.Parameters.Add(pText);

                    var pAtt = new OracleParameter("p_attachment", OracleDbType.Blob);
                    if (!string.IsNullOrEmpty(_attachmentPath) && File.Exists(_attachmentPath))
                        pAtt.Value = File.ReadAllBytes(_attachmentPath);
                    else
                        pAtt.Value = DBNull.Value;
                    cmd.Parameters.Add(pAtt);

                    cmd.Parameters.Add(new OracleParameter("p_att_name", OracleDbType.Varchar2)).Value = (object)_attachmentName ?? DBNull.Value;

                    cmd.Parameters.Add(new OracleParameter("p_mood", OracleDbType.Varchar2)).Value = string.IsNullOrEmpty(mood) ? DBNull.Value : (object)mood;

                 
                    var outParam = new OracleParameter("p_new_diaryid", OracleDbType.Int32) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(outParam);

                    cmd.ExecuteNonQuery();

                    int newDiaryId = OracleValueToInt(outParam.Value);
                    MessageBox.Show("Diary saved successfully! (Direct SQL) Id = " + newDiaryId, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

               
                    if (this.Controls.ContainsKey("textBox1")) ((TextBox)this.Controls["textBox1"]).Clear();
                    _attachmentPath = null;
                    _attachmentName = null;
                    if (this.Controls.ContainsKey("lblFileName")) ((Label)this.Controls["lblFileName"]).Text = "";
                    else if (this.Controls.ContainsKey("textBox2")) ((TextBox)this.Controls["textBox2"]).Text = "";

                    if (this.Controls.ContainsKey("comboBox1"))
                    {
                        var cb = (ComboBox)this.Controls["comboBox1"];
                        cb.SelectedIndex = 2;
                    }

                    LoadDiaries();
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

   
        private void LoadDiaries()
        {
            if (_userId == 0)
            {
                var gridEmpty = this.Controls.ContainsKey("dataGridView1") ? (DataGridView)this.Controls["dataGridView1"] : null;
                if (gridEmpty != null) gridEmpty.DataSource = null;
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("diary_id", typeof(int));
            dt.Columns.Add("created_at", typeof(DateTime));
            dt.Columns.Add("diary_text", typeof(string));
            dt.Columns.Add("mood", typeof(string));
            dt.Columns.Add("has_attachment", typeof(bool));
            dt.Columns.Add("attachment_name", typeof(string));

            try
            {
                using (var conn = new OracleConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.BindByName = true;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "proc_get_diaries";

                    cmd.Parameters.Add(new OracleParameter("p_user_id", OracleDbType.Int32)).Value = _userId;

                    var refCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(refCursor);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object idObj = reader.GetValue(0);
                            int id = OracleValueToInt(idObj);

                            DateTime created = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);

                            string preview = "";
                            if (!reader.IsDBNull(2))
                            {
                                object previewObj = reader.GetValue(2);
                                preview = previewObj?.ToString() ?? "";
                            }

                            string mood = reader.IsDBNull(3) ? "" : reader.GetString(3);

                            bool hasAtt = false;
                            if (!reader.IsDBNull(4))
                            {
                                object hasObj = reader.GetValue(4);
                                hasAtt = (OracleValueToInt(hasObj) == 1);
                            }

                            string attName = reader.IsDBNull(5) ? "" : reader.GetString(5);

                            dt.Rows.Add(id, created, preview, mood, hasAtt, attName);
                        }
                    }
                }

                var grid = (DataGridView)this.Controls["dataGridView1"];
                grid.AutoGenerateColumns = true;
                grid.DataSource = dt;
                grid.Refresh();

                if (grid.Columns.Contains("OpenAttachment")) grid.Columns.Remove("OpenAttachment");
                if (grid.Columns.Contains("diary_id")) grid.Columns["diary_id"].Visible = false;

                if (grid.Columns.Contains("created_at"))
                {
                    grid.Columns["created_at"].HeaderText = "Date";
                    grid.Columns["created_at"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                    grid.Columns["created_at"].Width = 140;
                }

                if (grid.Columns.Contains("diary_text"))
                {
                    grid.Columns["diary_text"].HeaderText = "Diary (preview)";
                    grid.Columns["diary_text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                if (grid.Columns.Contains("mood"))
                {
                    grid.Columns["mood"].HeaderText = "Mood";
                    grid.Columns["mood"].Width = 80;
                }

                var linkCol = new DataGridViewLinkColumn
                {
                    Name = "OpenAttachment",
                    HeaderText = "Attachment",
                    UseColumnTextForLinkValue = false
                };
                grid.Columns.Add(linkCol);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    bool hasAtt = Convert.ToBoolean(row.Cells["has_attachment"].Value);
                    row.Cells["OpenAttachment"].Value = hasAtt ? (row.Cells["attachment_name"].Value ?? "Open") : "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load diaries: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DataGridView)this.Controls["dataGridView1"];
            if (grid.Columns[e.ColumnIndex].Name != "OpenAttachment") return;

            var row = grid.Rows[e.RowIndex];
            bool hasAtt = Convert.ToBoolean(row.Cells["has_attachment"].Value);
            if (!hasAtt) return;

            int diaryId = OracleValueToInt(row.Cells["diary_id"].Value);
            string attName = row.Cells["attachment_name"].Value?.ToString();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.BindByName = true;
                    cmd.CommandText = "SELECT attachment, attachment_name FROM app_diaries WHERE diary_id = :p_id";
                    cmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int32)).Value = diaryId;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            object blobObj = reader.GetValue(0);
                            byte[] blobBytes = null;

                            if (blobObj is byte[] bArr)
                            {
                                blobBytes = bArr;
                            }
                            else if (blobObj is OracleBlob ob)
                            {
                                try { blobBytes = ob.Value; }
                                catch
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        ob.CopyTo(ms);
                                        blobBytes = ms.ToArray();
                                    }
                                }
                            }

                            if (blobBytes != null)
                            {
                                string filename = reader.IsDBNull(1) ? attName ?? "attachment" : reader.GetString(1);
                                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_" + filename);
                                File.WriteAllBytes(tempPath, blobBytes);

                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = tempPath,
                                    UseShellExecute = true
                                });
                            }
                            else
                            {
                                MessageBox.Show("Attachment format not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Attachment not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open attachment: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (_userId == 0) return;

            CrystalReport2 myReport = new CrystalReport2();
            myReport.SetParameterValue("p_user_id", _userId);
            Form4 reportForm = new Form4();
            reportForm.setReport(myReport);
            reportForm.Show();

        }

        private int OracleValueToInt(object oracleValue)
        {
            if (oracleValue == null || oracleValue == DBNull.Value) return 0;

            if (oracleValue is OracleDecimal od)
            {
                try { return od.ToInt32(); }
                catch { return Convert.ToInt32(od.Value); }
            }

            if (oracleValue is decimal dec) return Convert.ToInt32(dec);
            if (oracleValue is double d) return Convert.ToInt32(d);
            if (oracleValue is int i) return i;
            if (oracleValue is long l) return Convert.ToInt32(l);

            if (int.TryParse(oracleValue.ToString(), out int parsed)) return parsed;
            return 0;
        }

        private void textBox1_TextChanged(object sender, EventArgs e) { }

        private void button4_Click(object sender, EventArgs e)
        {

            int user = _userId;

            var f3 = new Form3(user);
            f3.Show();
            this.Hide();

        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (_userId == 0) return;

            CrystalReport1 myReport = new CrystalReport1();
            myReport.SetParameterValue("p_user_id", _userId);
            Form4 reportForm = new Form4();
            reportForm.setReport(myReport);
            reportForm.Show();

        }

        
    }
}
