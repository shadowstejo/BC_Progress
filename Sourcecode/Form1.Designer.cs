namespace BC_Progress
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuCatsOptions = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            importProgressJsonToolStripMenuItem = new ToolStripMenuItem();
            exportToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            refreshImagesToolStripMenuItem = new ToolStripMenuItem();
            toggleGridLockToolStripMenuItem = new ToolStripMenuItem();
            tabMain = new TabControl();
            tabCats = new TabPage();
            flowCats = new BufferedFlowLayoutPanel();
            tabProgress = new TabPage();
            panelProgressActions = new Panel();
            btnSaveProgress = new Button();
            gridCampaignProgress = new DataGridView();
            menuCatsOptions.SuspendLayout();
            tabMain.SuspendLayout();
            tabCats.SuspendLayout();
            tabProgress.SuspendLayout();
            panelProgressActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridCampaignProgress).BeginInit();
            SuspendLayout();
            // 
            // menuCatsOptions
            // 
            menuCatsOptions.ImageScalingSize = new Size(24, 24);
            menuCatsOptions.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, optionsToolStripMenuItem });
            menuCatsOptions.Location = new Point(0, 0);
            menuCatsOptions.Name = "menuCatsOptions";
            menuCatsOptions.Size = new Size(1184, 33);
            menuCatsOptions.TabIndex = 0;
            menuCatsOptions.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { importProgressJsonToolStripMenuItem, exportToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 29);
            fileToolStripMenuItem.Text = "File";
            // 
            // importProgressJsonToolStripMenuItem
            // 
            importProgressJsonToolStripMenuItem.Name = "importProgressJsonToolStripMenuItem";
            importProgressJsonToolStripMenuItem.Size = new Size(275, 34);
            importProgressJsonToolStripMenuItem.Text = "Import Progress.json";
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new Size(275, 34);
            exportToolStripMenuItem.Text = "Export";
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { refreshImagesToolStripMenuItem, toggleGridLockToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(92, 29);
            optionsToolStripMenuItem.Text = "Options";
            // 
            // refreshImagesToolStripMenuItem
            // 
            refreshImagesToolStripMenuItem.Name = "refreshImagesToolStripMenuItem";
            refreshImagesToolStripMenuItem.Size = new Size(244, 34);
            refreshImagesToolStripMenuItem.Text = "Refresh Images";
            // 
            // toggleGridLockToolStripMenuItem
            // 
            toggleGridLockToolStripMenuItem.Checked = true;
            toggleGridLockToolStripMenuItem.CheckOnClick = true;
            toggleGridLockToolStripMenuItem.CheckState = CheckState.Checked;
            toggleGridLockToolStripMenuItem.Name = "toggleGridLockToolStripMenuItem";
            toggleGridLockToolStripMenuItem.Size = new Size(244, 34);
            toggleGridLockToolStripMenuItem.Text = "Toggle grid-lock";
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabCats);
            tabMain.Controls.Add(tabProgress);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 33);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1184, 728);
            tabMain.TabIndex = 1;
            // 
            // tabCats
            // 
            tabCats.Controls.Add(flowCats);
            tabCats.Location = new Point(4, 34);
            tabCats.Name = "tabCats";
            tabCats.Padding = new Padding(3);
            tabCats.Size = new Size(1176, 723);
            tabCats.TabIndex = 0;
            tabCats.Text = "Cats";
            tabCats.UseVisualStyleBackColor = true;
            // 
            // flowCats
            // 
            flowCats.AutoScroll = true;
            flowCats.Dock = DockStyle.Fill;
            flowCats.Location = new Point(3, 3);
            flowCats.Name = "flowCats";
            flowCats.Padding = new Padding(12);
            flowCats.Size = new Size(1170, 717);
            flowCats.TabIndex = 0;
            flowCats.WrapContents = true;
            // 
            // tabProgress
            // 
            tabProgress.Controls.Add(gridCampaignProgress);
            tabProgress.Controls.Add(panelProgressActions);
            tabProgress.Location = new Point(4, 34);
            tabProgress.Name = "tabProgress";
            tabProgress.Padding = new Padding(3);
            tabProgress.Size = new Size(1176, 723);
            tabProgress.TabIndex = 1;
            tabProgress.Text = "Campaign Progress";
            tabProgress.UseVisualStyleBackColor = true;
            // 
            // panelProgressActions
            // 
            panelProgressActions.Controls.Add(btnSaveProgress);
            panelProgressActions.Dock = DockStyle.Bottom;
            panelProgressActions.Location = new Point(3, 665);
            panelProgressActions.Name = "panelProgressActions";
            panelProgressActions.Size = new Size(1170, 55);
            panelProgressActions.TabIndex = 1;
            // 
            // btnSaveProgress
            // 
            btnSaveProgress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveProgress.Location = new Point(996, 10);
            btnSaveProgress.Name = "btnSaveProgress";
            btnSaveProgress.Size = new Size(162, 35);
            btnSaveProgress.TabIndex = 0;
            btnSaveProgress.Text = "Save Progress";
            btnSaveProgress.UseVisualStyleBackColor = true;
            // 
            // gridCampaignProgress
            // 
            gridCampaignProgress.AllowUserToAddRows = false;
            gridCampaignProgress.AllowUserToDeleteRows = false;
            gridCampaignProgress.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridCampaignProgress.Dock = DockStyle.Fill;
            gridCampaignProgress.Location = new Point(3, 3);
            gridCampaignProgress.MultiSelect = false;
            gridCampaignProgress.Name = "gridCampaignProgress";
            gridCampaignProgress.RowHeadersVisible = false;
            gridCampaignProgress.RowHeadersWidth = 62;
            gridCampaignProgress.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridCampaignProgress.Size = new Size(1170, 717);
            gridCampaignProgress.TabIndex = 0;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 761);
            Controls.Add(tabMain);
            Controls.Add(menuCatsOptions);
            MainMenuStrip = menuCatsOptions;
            MinimumSize = new Size(1000, 600);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Battle Cats Progress";
            menuCatsOptions.ResumeLayout(false);
            menuCatsOptions.PerformLayout();
            tabMain.ResumeLayout(false);
            tabCats.ResumeLayout(false);
            tabProgress.ResumeLayout(false);
            panelProgressActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridCampaignProgress).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabMain;
        private TabPage tabCats;
        private MenuStrip menuCatsOptions;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem importProgressJsonToolStripMenuItem;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem refreshImagesToolStripMenuItem;
        private ToolStripMenuItem toggleGridLockToolStripMenuItem;
        private BufferedFlowLayoutPanel flowCats;
        private TabPage tabProgress;
        private DataGridView gridCampaignProgress;
        private Panel panelProgressActions;
        private Button btnSaveProgress;
    }
}
