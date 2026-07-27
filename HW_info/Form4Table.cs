using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace HW_info
{

    public partial class Form4Table : Form
    {

        public Form4Table()
        {
            InitializeComponent();
            DataLoad(Data1.Distinct);
        }

        private void DataLoad(IEnumerable<MyData> datas)
        {
            dataGridView1.DataSource = datas;
            Text = $"数据列表：{dataGridView1.Rows.Count}";
            if (dataGridView1.Columns.Contains("应用程序"))
            {
                dataGridView1.Columns["应用程序"].Visible = false;
            }

        }

        private void Form4_Load(object sender, EventArgs e)
        {
            KeyPreview = true;
            dataGridView1.CellContentDoubleClick += DataGridView1_CellContentDoubleClick;
        }

        private void Form4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape) { Close(); }
        }

        private void 导出OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                //桌面路径 
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                FileName = "HW_info数据.csv",
                Filter = "Csv 文档|*.csv"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var text = Data1.ToCsv(dataGridView1.DataSource as IEnumerable<MyData>);
                File.WriteAllText(dlg.FileName, text, Encoding.Default);
                if (File.Exists(dlg.FileName))
                {
                    MessageBox.Show("保存成功", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void 查看全部ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataLoad(Data1.Datas);
        }

        private void 去重ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataLoad(Data1.Distinct);
        }

        private void 最新提交数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataLoad(Data1.Latest);
        }

        private void DataGridView1_CellContentDoubleClick(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count <= 0) return;
            var sb = new StringBuilder();
            var r = dataGridView1.SelectedCells[0].RowIndex;
            var row = dataGridView1.Rows[r];
            foreach (DataGridViewCell cell in row.Cells)
            {
                var headerText = dataGridView1.Columns[cell.ColumnIndex].HeaderText;
                sb.Append(headerText).Append("：").AppendLine(cell.Value?.ToString().Replace("|", "\r\n\t"));
            }
            var title = "详细信息：";
            FormInfoDialog(sb.ToString(), title);
        }

        void FormInfoDialog(string infoText, string title)
        {
            var formInfo = new Form
            {
                Text = title,
                Size = this.Size,
                Font = this.Font,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowInTaskbar = false,
            };

            var textbox1 = new TextBox
            {
                Text = infoText,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
            };

            formInfo.Controls.Add(textbox1);
            formInfo.ShowDialog();
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            //绘制序号
            var grid = sender as DataGridView;
            var rowIdx = (e.RowIndex + 1).ToString();
            var centerFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, centerFormat);

        }


    }
}
