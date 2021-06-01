using System;
using System.Windows.Forms;
using Grasshopper.GUI;

namespace SandWorm
{ 
public class GH_MenuSliderForm : Form
{
	private Button btnCancel;

	private Button btnOK;

	private GroupBox grpDomain;

	private Label lblLower;

	private Label lblUpper;

	private Label lblValue;

	private NumericUpDown numLower;

	private NumericUpDown numUpper;

	private NumericUpDown numValue;

	private Panel Panel1;

	private Panel Panel2;

	private Panel pnSep1;

	private TableLayoutPanel tblDomain;

	private TableLayoutPanel tblType;

	private TableLayoutPanel tblValue;

	private MenuSlider _slider;

	private double _minValue;

	private double _maxValue = 1.0;

	private double _value;

	public GH_MenuSliderForm(MenuSlider slider)
	{
		_slider = slider;
		base.Load += GH_DoubleSliderPopup_Load;
		InitializeComponent();
		update();
	}

	private void btnOK_Click(object sender, EventArgs e)
	{
		if (numLower.Value > numUpper.Value)
		{
			numLower.Value = numUpper.Value - -10m;
		}
		if (numValue.Value < numLower.Value)
		{
			numValue.Value = numLower.Value;
		}
		else if (numValue.Value > numUpper.Value)
		{
			numValue.Value = numUpper.Value;
		}
		_slider.MinValue = (double)numLower.Value;
		_slider.MaxValue = (double)numUpper.Value;
		_slider.Value = (double)numValue.Value;
		_slider.FireChangedEvent();
	}

	private void GH_DoubleSliderPopup_Load(object sender, EventArgs e)
	{
		GH_WindowsControlUtil.FixTextRenderingDefault(base.Controls);
	}

	private void InitializeComponent()
	{
		btnOK = new System.Windows.Forms.Button();
		btnCancel = new System.Windows.Forms.Button();
		numUpper = new System.Windows.Forms.NumericUpDown();
		numLower = new System.Windows.Forms.NumericUpDown();
		Panel2 = new System.Windows.Forms.Panel();
		numValue = new System.Windows.Forms.NumericUpDown();
		lblValue = new System.Windows.Forms.Label();
		tblType = new System.Windows.Forms.TableLayoutPanel();
		grpDomain = new System.Windows.Forms.GroupBox();
		tblDomain = new System.Windows.Forms.TableLayoutPanel();
		lblUpper = new System.Windows.Forms.Label();
		lblLower = new System.Windows.Forms.Label();
		tblValue = new System.Windows.Forms.TableLayoutPanel();
		lblValue = new System.Windows.Forms.Label();
		pnSep1 = new System.Windows.Forms.Panel();
		Panel1 = new System.Windows.Forms.Panel();
		numUpper.BeginInit();
		numLower.BeginInit();
		Panel2.SuspendLayout();
		numValue.BeginInit();
		tblType.SuspendLayout();
		grpDomain.SuspendLayout();
		tblDomain.SuspendLayout();
		tblValue.SuspendLayout();
		SuspendLayout();
		btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
		btnOK.Dock = System.Windows.Forms.DockStyle.Right;
		btnOK.Click += new System.EventHandler(btnOK_Click);
		System.Drawing.Point location = new System.Drawing.Point(120, 8);
		btnOK.Location = location;
		btnOK.Name = "btnOK";
		System.Drawing.Size size = new System.Drawing.Size(80, 24);
		btnOK.Size = size;
		btnOK.TabIndex = 5;
		btnOK.Text = "OK";
		btnOK.UseVisualStyleBackColor = true;
		numUpper.DecimalPlaces = 4;
		numUpper.Dock = System.Windows.Forms.DockStyle.Fill;
		System.Drawing.Point location2 = new System.Drawing.Point(94, 23);
		numUpper.Location = location2;
		System.Windows.Forms.Padding margin = new System.Windows.Forms.Padding(0);
		numUpper.Margin = margin;
		decimal maximum = decimal.MaxValue;
		numUpper.Maximum = maximum;
		lblValue.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(1, 1);
		lblValue.Location = location;
		margin = new System.Windows.Forms.Padding(1);
		lblValue.Margin = margin;
		lblValue.Name = "lblValue";
		size = new System.Drawing.Size(92, 23);
		lblValue.Size = size;
		lblValue.TabIndex = 4;
		lblValue.Text = "Value";
		lblValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		maximum = 0m;
		numUpper.Minimum = maximum;
		numUpper.Name = "numUpper";
		size = new System.Drawing.Size(176, 20);
		numUpper.Size = size;
		numUpper.TabIndex = 5;
		numUpper.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
		numUpper.ThousandsSeparator = true;
		maximum = 0m;
		numUpper.Value = maximum;
		numLower.DecimalPlaces = 4;
		numLower.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(94, 0);
		numLower.Location = location;
		margin = new System.Windows.Forms.Padding(0);
		numLower.Margin = margin;
		maximum = decimal.MaxValue;
		numLower.Maximum = maximum;
		maximum = decimal.MinValue;
		numLower.Minimum = maximum;
		numLower.Name = "numLower";
		size = new System.Drawing.Size(176, 20);
		numLower.Size = size;
		numLower.TabIndex = 3;
		numLower.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
		numLower.ThousandsSeparator = true;
		Panel2.Controls.Add(btnOK);
		Panel2.Controls.Add(btnCancel);
		Panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
		location = new System.Drawing.Point(5, 273);
		Panel2.Location = location;
		Panel2.Name = "Panel2";
		margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
		Panel2.Padding = margin;
		size = new System.Drawing.Size(280, 32);
		Panel2.Size = size;
		Panel2.TabIndex = 9;
		btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
		location = new System.Drawing.Point(200, 8);
		btnCancel.Location = location;
		btnCancel.Name = "btnCancel";
		size = new System.Drawing.Size(80, 24);
		btnCancel.Size = size;
		btnCancel.TabIndex = 6;
		btnCancel.Text = "Cancel";
		btnCancel.UseVisualStyleBackColor = true;
		numValue.DecimalPlaces = 4;
		numValue.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(94, 0);
		numValue.Location = location;
		margin = new System.Windows.Forms.Padding(0);
		numValue.Margin = margin;
		maximum = decimal.MaxValue;
		numValue.Maximum = maximum;
		maximum = decimal.MinValue;
		numValue.Minimum = maximum;
		numValue.Name = "numValue";
		size = new System.Drawing.Size(176, 20);
		numValue.Size = size;
		numValue.TabIndex = 11;
		numValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
		grpDomain.Controls.Add(tblDomain);
		grpDomain.Dock = System.Windows.Forms.DockStyle.Top;
		location = new System.Drawing.Point(5, 88);
		grpDomain.Location = location;
		grpDomain.Name = "grpDomain";
		margin = new System.Windows.Forms.Padding(5);
		grpDomain.Padding = margin;
		size = new System.Drawing.Size(280, 94);
		grpDomain.Size = size;
		grpDomain.TabIndex = 12;
		grpDomain.TabStop = false;
		grpDomain.Text = "Numeric domain";
		tblDomain.ColumnCount = 2;
		tblDomain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35f));
		tblDomain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65f));
		tblDomain.Controls.Add(lblLower, 0, 0);
		tblDomain.Controls.Add(numLower, 1, 0);
		tblDomain.Controls.Add(lblUpper, 0, 1);
		tblDomain.Controls.Add(numUpper, 1, 1);
		tblDomain.Controls.Add(lblValue, 0, 2);
		tblDomain.Controls.Add(numValue, 1, 2);
		tblDomain.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(5, 18);
		tblDomain.Location = location;
		tblDomain.Name = "tblDomain";
		tblDomain.RowCount = 3;
		tblDomain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		tblDomain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		tblDomain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		size = new System.Drawing.Size(270, 71);
		tblDomain.Size = size;
		tblDomain.TabIndex = 0;
		lblUpper.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(1, 24);
		lblUpper.Location = location;
		margin = new System.Windows.Forms.Padding(1);
		lblUpper.Margin = margin;
		lblUpper.Name = "lblUpper";
		size = new System.Drawing.Size(92, 21);
		lblUpper.Size = size;
		lblUpper.TabIndex = 12;
		lblUpper.Text = "Upper limit";
		lblUpper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		lblLower.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(1, 1);
		lblLower.Location = location;
		margin = new System.Windows.Forms.Padding(1);
		lblLower.Margin = margin;
		lblLower.Name = "lblLower";
		size = new System.Drawing.Size(92, 21);
		lblLower.Size = size;
		lblLower.TabIndex = 4;
		lblLower.Text = "Lower limit";
		lblLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		lblValue.Dock = System.Windows.Forms.DockStyle.Fill;
		location = new System.Drawing.Point(1, 1);
		lblValue.Location = location;
		margin = new System.Windows.Forms.Padding(1);
		lblValue.Margin = margin;
		lblValue.Name = "lblValue";
		size = new System.Drawing.Size(92, 23);
		lblValue.Size = size;
		lblValue.TabIndex = 4;
		lblValue.Text = "Value";
		lblValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		pnSep1.Dock = System.Windows.Forms.DockStyle.Top;
		location = new System.Drawing.Point(5, 78);
		pnSep1.Location = location;
		pnSep1.Name = "pnSep1";
		size = new System.Drawing.Size(280, 10);
		pnSep1.Size = size;
		pnSep1.TabIndex = 14;
		Panel1.Dock = System.Windows.Forms.DockStyle.Top;
		location = new System.Drawing.Point(5, 182);
		Panel1.Location = location;
		Panel1.Name = "Panel1";
		size = new System.Drawing.Size(280, 10);
		Panel1.Size = size;
		Panel1.TabIndex = 15;
		System.Drawing.SizeF sizeF2 = (base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f));
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		size = (base.ClientSize = new System.Drawing.Size(290, 155));
		base.Controls.Add(Panel1);
		base.Controls.Add(grpDomain);
		base.Controls.Add(Panel2);
		base.Controls.Add(pnSep1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.HelpButton = true;
		base.KeyPreview = true;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		size = (MinimumSize = new System.Drawing.Size(260, 140));
		base.Name = "GH_DoubleSliderPopup";
		margin = (base.Padding = new System.Windows.Forms.Padding(5));
		base.ShowIcon = false;
		base.ShowInTaskbar = false;
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		Text = "SliderSettings";
		numUpper.EndInit();
		numLower.EndInit();
		Panel2.ResumeLayout(false);
		numValue.EndInit();
		tblType.ResumeLayout(false);
		grpDomain.ResumeLayout(false);
		tblDomain.ResumeLayout(false);
		tblValue.ResumeLayout(false);
		ResumeLayout(false);
	}

	private void update()
	{
		_minValue = _slider.MinValue;
		_maxValue = _slider.MaxValue;
		_value = _slider.Value;
		numLower.Value = new decimal(_minValue);
		numUpper.Value = new decimal(_maxValue);
		numValue.Value = new decimal(_value);
	}

	private void numLower_ValueChanged(object sender, EventArgs e)
	{
	}
}
}