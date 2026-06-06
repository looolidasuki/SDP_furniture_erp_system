using System;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;
using FurnitureERP.Forms;

namespace FurnitureERP
{
    public partial class LoginForm : Form
    {
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private Button _btnLogin;
        private Label _lblError;
        private readonly StaffController _staffCtrl = new StaffController();

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "Premium Living Furniture — ERP Login";
            Size = new Size(440, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(245, 247, 252);
            Font = new Font("Segoe UI", 9.5f);

            // Logo panel
            Panel logoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.FromArgb(30, 60, 120)
            };
            Label logo1 = new Label
            {
                Text = "PREMIUM LIVING",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label logo2 = new Label
            {
                Text = "Furniture ERP System",
                ForeColor = Color.FromArgb(160, 190, 230),
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            logoPanel.Controls.Add(logo1);
            logoPanel.Controls.Add(logo2);

            // Form panel
            Panel formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40, 30, 40, 30)
            };

            Label lblTitle = new Label
            {
                Text = "Sign In",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                Location = new Point(40, 30),
                AutoSize = true
            };

            Label lblUser = new Label
            {
                Text = "Username or Email",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 90, 110),
                Location = new Point(40, 80),
                AutoSize = true
            };

            _txtUsername = new TextBox
            {
                Location = new Point(40, 100),
                Size = new Size(320, 32),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblPass = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 90, 110),
                Location = new Point(40, 150),
                AutoSize = true
            };

            _txtPassword = new TextBox
            {
                Location = new Point(40, 170),
                Size = new Size(320, 32),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            _lblError = new Label
            {
                Text = "",
                ForeColor = Color.Crimson,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(40, 212),
                Size = new Size(320, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnLogin = new Button
            {
                Text = "Login",
                Location = new Point(40, 240),
                Size = new Size(320, 42),
                BackColor = Color.FromArgb(30, 60, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += BtnLogin_Click;
            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };

            Label lblVersion = new Label
            {
                Text = "v2.0 © 2025 Premium Living Furniture",
                ForeColor = Color.FromArgb(160, 170, 190),
                Font = new Font("Segoe UI", 8),
                Location = new Point(40, 310),
                AutoSize = true
            };

            formPanel.Controls.Add(lblTitle);
            formPanel.Controls.Add(lblUser);
            formPanel.Controls.Add(_txtUsername);
            formPanel.Controls.Add(lblPass);
            formPanel.Controls.Add(_txtPassword);
            formPanel.Controls.Add(_lblError);
            formPanel.Controls.Add(_btnLogin);
            formPanel.Controls.Add(lblVersion);

            Controls.Add(formPanel);
            Controls.Add(logoPanel);
            ResumeLayout();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _lblError.Text = "Please enter username and password.";
                return;
            }

            try
            {
                Staff user = _staffCtrl.Login(username, password);
                if (user != null)
                {
                    var main = new Forms.MainForm();
                    main.SetCurrentUser(user);
                    main.Show();
                    Hide();
                }
                else
                {
                    _lblError.Text = "Invalid username/email or password.";
                    _txtPassword.Clear();
                }
            }
            catch (Exception ex)
            {
                _lblError.Text = "Connection error: " + ex.Message;
            }
        }
    }
}
