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
            buttonDownload = new Button();
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
            panelWebView.Location = new Point(17, 20);
            panelWebView.Margin = new Padding(4, 5, 4, 5);
            panelWebView.Name = "panelWebView";
            panelWebView.Size = new Size(1085, 624);
            panelWebView.TabIndex = 4;
            // 
            // webView
            // 
            webView.AllowExternalDrop = true;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.White;
            webView.Dock = DockStyle.Fill;
            webView.Location = new Point(0, 0);
            webView.Margin = new Padding(4, 5, 4, 5);
            webView.Name = "webView";
            webView.Size = new Size(1083, 622);
            webView.TabIndex = 5;
            webView.TabStop = false;
            webView.ZoomFactor = 1D;
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStatus.AutoSize = true;
            labelStatus.Location = new Point(13, 677);
            labelStatus.Margin = new Padding(0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(60, 25);
            labelStatus.TabIndex = 2;
            labelStatus.Text = "Ready";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(17, 707);
            progressBar.Margin = new Padding(4, 5, 4, 5);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(1086, 42);
            progressBar.TabIndex = 3;
            // 
            // buttonDownload
            // 
            buttonDownload.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonDownload.Location = new Point(880, 655);
            buttonDownload.Margin = new Padding(14, 5, 4, 5);
            buttonDownload.Name = "buttonDownload";
            buttonDownload.Size = new Size(107, 42);
            buttonDownload.TabIndex = 0;
            buttonDownload.Text = "Download";
            buttonDownload.UseVisualStyleBackColor = true;
            buttonDownload.Click += ButtonDownload_Click;
            // 
            // labelWauz
            // 
            labelWauz.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelWauz.AutoSize = true;
            labelWauz.Cursor = Cursors.Hand;
            labelWauz.Location = new Point(483, 663);
            labelWauz.Margin = new Padding(4, 0, 4, 0);
            labelWauz.Name = "labelWauz";
            labelWauz.Size = new Size(111, 25);
            labelWauz.TabIndex = 6;
            labelWauz.Text = "Open WAUZ";
            // 
            // labelConfigFolder
            // 
            labelConfigFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelConfigFolder.AutoSize = true;
            labelConfigFolder.Cursor = Cursors.Hand;
            labelConfigFolder.Location = new Point(746, 663);
            labelConfigFolder.Margin = new Padding(4, 0, 4, 0);
            labelConfigFolder.Name = "labelConfigFolder";
            labelConfigFolder.Size = new Size(122, 25);
            labelConfigFolder.TabIndex = 8;
            labelConfigFolder.Text = "Config-Folder";
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClose.Location = new Point(996, 655);
            buttonClose.Margin = new Padding(4, 5, 4, 5);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(107, 42);
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
            labelDownloadFolder.Location = new Point(596, 663);
            labelDownloadFolder.Margin = new Padding(4, 0, 4, 0);
            labelDownloadFolder.Name = "labelDownloadFolder";
            labelDownloadFolder.Size = new Size(151, 25);
            labelDownloadFolder.TabIndex = 7;
            labelDownloadFolder.Text = "Download-Folder";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1120, 768);
            Controls.Add(labelDownloadFolder);
            Controls.Add(buttonClose);
            Controls.Add(labelConfigFolder);
            Controls.Add(labelWauz);
            Controls.Add(buttonDownload);
            Controls.Add(progressBar);
            Controls.Add(labelStatus);
            Controls.Add(panelWebView);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
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
        private Button buttonDownload;
        private Label labelWauz;
        private Label labelConfigFolder;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Button buttonClose;
        private Label labelDownloadFolder;
    }
}