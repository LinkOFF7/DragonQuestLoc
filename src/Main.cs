using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DragonQuestLoc
{
    public partial class Main : Form
    {
        struct MesListItem
        {
            public bool visible;
            public string key;
            public string original;
            public string translated;

            public string search_data;
        }

        MesDatatype CurrentMesFile;
        MesListItem[] CurrentMesItems;
        
        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CurrentMesFile = null;
            CurrentMesItems = null;

            ResetUI();
            ResetTitle();
        }

        private void ResetTitle(string fn = "")
        {
            string default_name = "Dragon Quest Monsters: Joker 3";

            if ((CurrentMesFile != null) && fn.Length > 0)
            {
                Text = string.Format("{0} - {1}", Path.GetFileName(fn), default_name);
            }
            else
            {
                Text = default_name;
            }
        }

        private void ResetUI()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            if (CurrentMesItems != null)
            {
                foreach (MesListItem item in CurrentMesItems )
                {
                    if( item.visible )
                    {
                        var new_item = listView1.Items.Add(item.key).SubItems;
                        new_item.Add(item.original);        // original value
                        new_item.Add(item.translated);      // modified value
                    }
                }
                
                exportToolStripMenuItem.Enabled = true;
            }
            else
            {
                exportToolStripMenuItem.Enabled = false;
            }

            listView1.EndUpdate();

            toolStripLabel2.Text = "";

            if (CurrentMesItems != null)
            {
                int num = listView1.Items.Count;
                int den = CurrentMesItems.Length;

                if (toolStripTextBox1.Text.Length > 0)
                {
                    toolStripLabel2.Text = string.Format("{0} of {1} items", num, den);
                }
            }
        }

        private int GetVisibleIndex(int idx)
        {
            if (CurrentMesItems != null)
            {
                int it = 0;
                int i = 0;
                while (i < CurrentMesItems.Length)
                {
                    if (CurrentMesItems[i].visible)
                    {
                        if (idx == it)
                        {
                            return i;
                        }

                        ++it;
                    }

                    ++i;
                }
            }

            return -1;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ConfirmAction("load another file"))
            {
                return;
            }

            if( openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                string fn1 = openFileDialog1.FileNames[0];

                if( File.Exists(fn1) )
                {
                    FileStream fs = File.OpenRead(fn1);

                    if( fs.CanRead )
                    {
                        var br = new BinaryReader(fs);

                        var mes = new MesDatatype();
                        mes.Read(br);

                        CurrentMesFile = mes;
                        CurrentMesItems = null;

                        var list_items = new List<MesListItem>();
                        var mes_items = CurrentMesFile.GetAllValues();

                        foreach (Tuple<string, string> pair in mes_items)
                        {
                            var li = new MesListItem();

                            li.visible = true;
                            li.key = pair.Item1;
                            li.original = pair.Item2;
                            li.translated = "";

                            // keywords for searching
                            li.search_data = string.Concat(li.key.ToLower(), "\0", li.original.ToLower());

                            list_items.Add(li);
                        }

                        CurrentMesItems = list_items.ToArray();

                        ResetUI();
                        ResetTitle(fn1);
                    }

                    fs.Close();
                }
                export_txt.Enabled = true;
                import_txt.Enabled = true;
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if( listView1.SelectedIndices.Count == 1 )
            {
                int index = GetVisibleIndex(listView1.SelectedIndices[0]);

                if (index == -1)
                {
                    return;
                }

                EditDialog ed = new EditDialog();

                var single = CurrentMesFile.GetSingleValue(index);

                ed.Setup(single.Item1, single.Item2);

                if (ed.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var result = ed.UserInput();

                    if (CurrentMesFile.UpdateValue(index, result.Item1, result.Item2))
                    {
                        CurrentMesItems[index].translated = result.Item2;

                        //ResetUI();
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private bool ConfirmAction(string action)
        {
            bool quit_allowed = true;

            if (CurrentMesFile != null)
            {
                int edit_count = CurrentMesFile.NumEdits();

                if (edit_count > 0)
                {
                    string saver_str = string.Format("You have made changes to {0} line(s)\n\nAre you sure you want to {1}?", edit_count, action);

                    if (MessageBox.Show(saver_str, "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.Yes)
                    {
                        quit_allowed = false;
                    }
                }
            }

            return quit_allowed;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmAction("quit"))
            {
                e.Cancel = true;
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentMesFile != null)
            {
                if( saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if( saveFileDialog1.FileNames.Length == 1 )
                    {
                        string fn1 = saveFileDialog1.FileNames[0];

                        FileStream fs = File.Create(fn1);
                        if( fs != null )
                        {
                            var bw = new BinaryWriter(fs);

                            CurrentMesFile.Write(bw);

                            ResetUI();

                            fs.Close();
                        }
                    }
                }
            }
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            if( CurrentMesItems == null )
            {
                return;
            }

            var box = sender as ToolStripTextBox;

            if(box == null)
            {
                return;
            }

            string query = box.Text.ToLower();

            if (query.Length == 0)
            {
                for (int i = 0; i < CurrentMesItems.Length; ++i)
                {
                    CurrentMesItems[i].visible = true;
                }
            }
            else
            {
                for (int i = 0; i < CurrentMesItems.Length; ++i)
                {
                    CurrentMesItems[i].visible = CurrentMesItems[i].search_data.Contains(query);
                }
            }

            ResetUI();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ResetUI();
        }

        private void export_txt_Click(object sender, EventArgs e)
        {
            if(CurrentMesItems != null)
            {
                List<string> text = new List<string>();
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "Text files .txt|*.txt";
                if(save.ShowDialog() == DialogResult.OK)
                {
                    foreach(var mes in CurrentMesItems)
                    {
                        text.Add(mes.original.Replace("\n", "{CL}").Replace("\r", "{RF}")); ;
                    }
                    File.WriteAllLines(save.FileName, text);
                }
            }
        }

        private void import_txt_Click(object sender, EventArgs e)
        {
            if (CurrentMesItems != null)
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "Text files .txt|*.txt";
                if(open.ShowDialog() == DialogResult.OK)
                {
                    List<string> text = File.ReadAllLines(open.FileName).ToList();
                    for(int i = 0; i < CurrentMesItems.Length; i++)
                    {
                        CurrentMesItems[i].translated = text[i].Replace("{CL}", "\n").Replace("{RF}", "\r");
                        if (CurrentMesFile.UpdateValue(i, CurrentMesItems[i].key, CurrentMesItems[i].translated))
                        {
                            
                        }
                    }
                    ResetUI();
                }
            }
        }
    }
}
