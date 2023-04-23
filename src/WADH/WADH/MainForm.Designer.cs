namespace WADH
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panelWebView = new Panel();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            labelStatus = new Label();
            progressBar = new ProgressBar();
            buttonStart = new Button();
            labelWauz = new Label();
            labelConfigFolder = new Label();
            buttonClose = new Button();
            labelDownloadFolder = new Label();
            panelWebView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            // 
            // panelWebView
            // 
            panelWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelWebView.BorderStyle = BorderStyle.FixedSingle;
            panelWebView.Controls.Add(webView);
            panelWebView.Location = new Point(12, 12);
            panelWebView.Name = "panelWebView";
            panelWebView.Size = new Size(760, 375);
            panelWebView.TabIndex = 4;
            // 
            // webView
            // 
            webView.AllowExternalDrop = true;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.White;
            webView.Dock = DockStyle.Fill;
            webView.Location = new Point(0, 0);
            webView.Name = "webView";
            webView.Size = new Size(758, 373);
            webView.TabIndex = 5;
            webView.TabStop = false;
            webView.ZoomFactor = 1D;
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStatus.AutoSize = true;
            labelStatus.Location = new Point(9, 406);
            labelStatus.Margin = new Padding(0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(39, 15);
            labelStatus.TabIndex = 2;
            labelStatus.Text = "Ready";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(12, 424);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(760, 25);
            progressBar.TabIndex = 3;
            // 
            // buttonStart
            // 
            buttonStart.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonStart.Location = new Point(616, 393);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(75, 25);
            buttonStart.TabIndex = 0;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += ButtonStart_Click;
            // 
            // labelWauz
            // 
            labelWauz.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelWauz.AutoSize = true;
            labelWauz.Cursor = Cursors.Hand;
            labelWauz.Location = new Point(292, 398);
            labelWauz.Name = "labelWauz";
            labelWauz.Size = new Size(73, 15);
            labelWauz.TabIndex = 6;
            labelWauz.Text = "Open WAUZ";
            // 
            // labelConfigFolder
            // 
            labelConfigFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelConfigFolder.AutoSize = true;
            labelConfigFolder.Cursor = Cursors.Hand;
            labelConfigFolder.Location = new Point(503, 398);
            labelConfigFolder.Name = "labelConfigFolder";
            labelConfigFolder.Size = new Size(107, 15);
            labelConfigFolder.TabIndex = 7;
            labelConfigFolder.Text = "Open config folder";
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClose.Location = new Point(697, 393);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(75, 25);
            buttonClose.TabIndex = 1;
            buttonClose.Text = "Close";
            buttonClose.UseVisualStyleBackColor = true;
            buttonClose.Click += ButtonClose_Click;
            // 
            // labelDownloadFolder
            // 
            labelDownloadFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelDownloadFolder.AutoSize = true;
            labelDownloadFolder.Cursor = Cursors.Hand;
            labelDownloadFolder.Location = new Point(371, 398);
            labelDownloadFolder.Name = "labelDownloadFolder";
            labelDownloadFolder.Size = new Size(126, 15);
            labelDownloadFolder.TabIndex = 8;
            labelDownloadFolder.Text = "Open download folder";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(784, 461);
            Controls.Add(labelDownloadFolder);
            Controls.Add(buttonClose);
            Controls.Add(labelConfigFolder);
            Controls.Add(labelWauz);
            Controls.Add(buttonStart);
            Controls.Add(progressBar);
            Controls.Add(labelStatus);
            Controls.Add(panelWebView);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MainForm";
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            panelWebView.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelWebView;
        private Label labelStatus;
        private ProgressBar progressBar;
        private Button buttonStart;
        private Label labelWauz;
        private Label labelConfigFolder;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Button buttonClose;
        private Label labelDownloadFolder;
    }
}