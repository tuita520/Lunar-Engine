﻿/** Copyright 2018 John Lamontagne https://www.mmorpgcreation.com

	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0

	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
*/
using System;
using DarkUI.Forms;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Lunar.Core.World;
using Lunar.Editor.World;
using Microsoft.Xna.Framework.Graphics;
using ScintillaNET;

namespace Lunar.Editor.Controls
{
    public partial class DockNPCEditor : SavableDocument
    {
        private FileInfo _file;
        private string _regularDockText;
        private string _unsavedDockText;
        private bool _unsaved;
        private string _activeScript;

        private Project _project;

        private ItemDescriptor _item;

        private DockNPCEditor()
        {
            InitializeComponent();

            _activeScript = "";

            this.txtEditor.Lexer = Lexer.Lua;

            this.txtEditor.StyleResetDefault();

            this.txtEditor.Styles[Style.Default].Font = "Consolas";
            this.txtEditor.Styles[Style.Default].Size = 12;

            this.txtEditor.Styles[Style.Default].BackColor = Color.FromArgb(29, 31, 33);
            this.txtEditor.Styles[Style.Default].ForeColor = Color.FromArgb(197, 200, 198);

            this.txtEditor.StyleClearAll();

            this.txtEditor.Styles[Style.Lua.Comment].ForeColor = Color.FromArgb(181, 189, 104);
            this.txtEditor.Styles[Style.Lua.CommentLine].ForeColor = Color.FromArgb(181, 189, 104);
            this.txtEditor.Styles[Style.Lua.CommentLine].Italic = true;

            this.txtEditor.Styles[Style.Lua.String].ForeColor = Color.FromArgb(222, 147, 95);

            this.txtEditor.Styles[Style.Lua.Operator].ForeColor = Color.FromArgb(240, 198, 116);

            this.txtEditor.Styles[Style.Lua.Number].ForeColor = Color.FromArgb(138, 190, 183);

            this.txtEditor.Styles[Style.Lua.Preprocessor].ForeColor = Color.FromArgb(129, 162, 190);

            this.txtEditor.Styles[Style.Lua.Identifier].ForeColor = Color.FromArgb(178, 148, 187);

            this.txtEditor.Styles[Style.Lua.Word].ForeColor = Color.FromArgb(130, 239, 104);

            this.txtEditor.SetKeywords(0, "if then end not function");
        }

        public DockNPCEditor(Project project, string text, Image icon, FileInfo file)
            : this()
        {
            _project = project;

            _regularDockText = text;
            _unsavedDockText = text + "*";


            DockText = text;
            Icon = icon;

            _file = file;

            _item = ItemDescriptor.Load(file.FullName);

            this.txtName.Text = _item.Name;
            this.radioStackable.Checked = _item.Stackable;
            this.radioNotStackable.Checked = !_item.Stackable;
            this.cmbEquipSlot.DataSource = Enum.GetValues(typeof(EquipmentSlots));
            this.cmbEquipSlot.SelectedItem = EquipmentSlots.Chest;
            this.txtStr.Text = _item.Strength.ToString();
            this.txtInt.Text = _item.Intelligence.ToString();
            this.txtDef.Text = _item.Defence.ToString();
            this.txtHealth.Text = _item.Health.ToString();
            this.txtDex.Text = _item.Dexterity.ToString();

            onUseToolStripMenuItem.Checked = true;
            if (_item.Scripts.ContainsKey("OnUse"))
            {
                this.txtEditor.Text = _item.Scripts["OnUse"];
            }
            else
            {
                this.txtEditor.Text = "function OnUse(args) \n end";
            }
        }

        public override void Close()
        {
            if (_unsaved)
            {
                var result = DarkMessageBox.ShowWarning(@"You will lose any unsaved changes. Continue?", @"Close document", DarkDialogButton.YesNo);
                if (result == DialogResult.No)
                    return;
            }
         
            base.Close();
        }

        private void DockItemEditor_Load(object sender, System.EventArgs e)
        {
            this.DockText = _regularDockText;
            _unsaved = false;
        }

        public void Save()
        {
           
            this.DockText = _regularDockText;
            _unsaved = false;
            _item.Save(_file.FullName);
        }

        private void txtEditor_TextChanged(object sender, System.EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

            if (_item.Scripts.ContainsKey(_activeScript))
            {
                _item.Scripts[_activeScript] = txtEditor.Text;
            }
        }


        private void buttonSave_Click(object sender, System.EventArgs e)
        {
            this.Save();
        }

       

        private void picTexture_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.InitialDirectory = _project.ClientRootDirectory.FullName;
                dialog.Filter = @"Image Files (*.png)|*.png";
                dialog.DefaultExt = ".png";
                dialog.AddExtension = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    this.DockText = _unsavedDockText;
                    _unsaved = true;

                    string path = dialog.FileName; 

                    _item.TexturePath = path;

                    
                }
            }
        }

        private void txtEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.S)
            {
                this.Save();
                e.SuppressKeyPress = true;
            }

        }

        private void DockItemEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.S)
            {
                this.Save();
                e.SuppressKeyPress = true;
            }
        }

        private void txtStr_TextChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

        }

        private void txtInt_TextChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

        }

        private void txtDex_TextChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

        }

        private void txtDef_TextChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

        }

        private void txtHealth_TextChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

            
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

            
        }

        private void radioStackable_CheckedChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

        }

        private void radioNotStackable_CheckedChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

            
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

        }


        private void cmbEquipmentSlot_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DockText = _unsavedDockText;
            _unsaved = true;

            
        }

        private void onUseToolStripMenuItem_Click(object sender, EventArgs e)
        {

            onUseToolStripMenuItem.Checked = true;
            onEquipToolStripMenuItem.Checked = false;
            onAcquiredToolStripMenuItem.Checked = false;
            onDroppedToolStripMenuItem.Checked = false;
            onCreatedToolStripMenuItem.Checked = false;

            if (_item.Scripts.ContainsKey("OnUse"))
            {
                this.txtEditor.Text = _item.Scripts["OnUse"];
            }
            else
            {
                this.txtEditor.Text = "function OnUse(args) \nend";
                _item.Scripts.Add("OnUse", this.txtEditor.Text);
            }

            _activeScript = "OnUse";
        }

        private void onEquipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onEquipToolStripMenuItem.Checked = true;
            onUseToolStripMenuItem.Checked = false;
            onAcquiredToolStripMenuItem.Checked = false;
            onDroppedToolStripMenuItem.Checked = false;
            onCreatedToolStripMenuItem.Checked = false;

            if (_item.Scripts.ContainsKey("OnEquip"))
            {
                this.txtEditor.Text = _item.Scripts["OnEqip"];
            }
            else
            {
                this.txtEditor.Text = "function OnEquip(args) \nend";
                _item.Scripts.Add("OnEqip", this.txtEditor.Text);
            }

            _activeScript = "OnEquip";
        }

        private void onAcquiredToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onAcquiredToolStripMenuItem.Checked = true;
            onEquipToolStripMenuItem.Checked = false;
            onUseToolStripMenuItem.Checked = false;
            onDroppedToolStripMenuItem.Checked = false;
            onCreatedToolStripMenuItem.Checked = false;

            if (_item.Scripts.ContainsKey("OnAcquired"))
            {
                this.txtEditor.Text = _item.Scripts["OnAcquired"];
            }
            else
            {
                this.txtEditor.Text = "function OnAcquired(args) \nend";
                _item.Scripts.Add("OnAcquired", this.txtEditor.Text);
            }

            _activeScript = "OnAcquired";
        }

        private void onDroppedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onDroppedToolStripMenuItem.Checked = true;
            onAcquiredToolStripMenuItem.Checked = false;
            onEquipToolStripMenuItem.Checked = false;
            onUseToolStripMenuItem.Checked = false;
            onCreatedToolStripMenuItem.Checked = false;

            if (_item.Scripts.ContainsKey("OnDropped"))
            {
                this.txtEditor.Text = _item.Scripts["OnDropped"];
            }
            else
            {
                this.txtEditor.Text = "function OnDropped(args) \nend";
                _item.Scripts.Add("OnDropped", this.txtEditor.Text);
            }

            _activeScript = "OnDropped";
        }

        private void onCreatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onCreatedToolStripMenuItem.Checked = true;
            onDroppedToolStripMenuItem.Checked = false;
            onAcquiredToolStripMenuItem.Checked = false;
            onEquipToolStripMenuItem.Checked = false;
            onUseToolStripMenuItem.Checked = false;

            if (_item.Scripts.ContainsKey("OnCreated"))
            {
                this.txtEditor.Text = _item.Scripts["OnCreated"];
            }
            else
            {
                this.txtEditor.Text = "function OnCreated(args) \nend";
                _item.Scripts.Add("OnCreated", this.txtEditor.Text);
            }

            _activeScript = "OnCreated";
        }

    }
}
