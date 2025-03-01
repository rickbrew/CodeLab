﻿/////////////////////////////////////////////////////////////////////////////////
// CodeLab for Paint.NET
// Portions Copyright ©2007-2017 BoltBait. All Rights Reserved.
// Portions Copyright ©2018 Jason Wendt. All Rights Reserved.
// Portions Copyright ©Microsoft Corporation. All Rights Reserved.
//
// THE CODELAB DEVELOPERS MAKE NO WARRANTY OF ANY KIND REGARDING THE CODE. THEY
// SPECIFICALLY DISCLAIM ANY WARRANTY OF FITNESS FOR ANY PARTICULAR PURPOSE OR
// ANY OTHER WARRANTY.  THE CODELAB DEVELOPERS DISCLAIM ALL LIABILITY RELATING
// TO THE USE OF THIS CODE.  NO LICENSE, EXPRESS OR IMPLIED, BY ESTOPPEL OR
// OTHERWISE, TO ANY INTELLECTUAL PROPERTY RIGHTS IS GRANTED HEREIN.
//
// Latest distribution: https://www.BoltBait.com/pdn/codelab
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PdnCodeLab
{
    internal partial class UIBuilder : ChildFormBase
    {
        internal string UIControlsText;
        private readonly IEffectEnvironment environmentParameters;
        private readonly IServiceProvider serviceProvider;
        private readonly ProjectType projectType;
        private bool dirty = false;
        private readonly List<UIElement> MasterList = new List<UIElement>();
        private readonly HashSet<string> IDList = new HashSet<string>();
        private string currentID;

        internal static Image[] ControlIcons = new Image[]
        {
            UIUtil.GetImage("00int"),
            UIUtil.GetImage("01CheckBox"),
            UIUtil.GetImage("02ColorWheel"),
            UIUtil.GetImage("03AngleChooser"),
            UIUtil.GetImage("04PanSlider"),
            UIUtil.GetImage("05TextBox"),
            UIUtil.GetImage("06DoubleSlider"),
            UIUtil.GetImage("07DropDown"),
            UIUtil.GetImage("08BlendOps"),
            UIUtil.GetImage("09Fonts"),
            UIUtil.GetImage("10RadioButton"),
            UIUtil.GetImage("11ReseedButton"),
            UIUtil.GetImage("12MultiTextBox"),
            UIUtil.GetImage("13RollControl"),
            UIUtil.GetImage("14FilenameControl"),
            UIUtil.GetImage("15Uri"),
            UIUtil.GetImage("16FolderControl"),
            UIUtil.GetImage("17Comment"),
            UIUtil.GetImage("18Layers")
        };

        internal UIBuilder(string UserScriptText, ProjectType projectType, IServiceProvider serviceProvider, IEffectEnvironment environmentParameters)
        {
            InitializeComponent();

            // PDN Theme
            ControlListView.ForeColor = this.ForeColor;
            ControlListView.BackColor = this.BackColor;
            foreach (Control control in this.Controls)
            {
                if (control is TextBox || control is ComboBox)
                {
                    control.ForeColor = this.ForeColor;
                    control.BackColor = this.BackColor;
                }
            }

            ControlListView.Font = new Font(Settings.FontFamily, ControlListView.Font.SizeInPoints);

            // Populate the ControlType dropdown based on allowed ElementTypes
            this.ControlType.ProjectType = projectType;

            if (ControlType.ItemHeight < 18)
            {
                ControlType.ItemHeight = 18;
            }
            ControlType.SelectedIndex = 0;

            ControlStyle.ItemHeight = ControlType.ItemHeight;
            ControlStyle.Height = ControlType.Height;
            ControlStyle.SelectedIndex = 0;

            enabledWhenField.DropDownWidth = enabledWhenField.Width * 2;
            enabledWhenField.SelectedIndex = 0;
            enabledWhenField.Top = rbEnabled.Top; // HiDPI fix

            enabledWhenCondition.SelectedIndex = 0;
            enabledWhenCondition.Top = rbEnabled.Top; // HiDPI fix

            UpdateEnabledFields();

            imgList.ImageSize = UIUtil.ScaleSize(16, 16);
            imgList.Images.AddRange(ControlIcons);

            DefaultColorComboBox.DropDownWidth = DefaultColorComboBox.Width * 2;
            DefaultColorComboBox.Items.Add("None");
            DefaultColorComboBox.Items.Add("PrimaryColor");
            DefaultColorComboBox.Items.Add("SecondaryColor");
            DefaultColorComboBox.Items.AddRange(UIUtil.GetColorNames(false));

            MasterList.AddRange(UIElement.ProcessUIControls(UserScriptText, projectType));

            foreach (UIElement element in MasterList)
            {
                IDList.Add(element.Identifier);
            }
            refreshListView(0);
            dirty = false;
            this.environmentParameters = environmentParameters.CreateRef();
            this.serviceProvider = serviceProvider;
            this.projectType = projectType;
        }

        private void refreshListView(int SelectItemIndex)
        {
            ControlListView.Clear();
            enabledWhenField.Items.Clear();

            foreach (UIElement uie in MasterList)
            {
                ControlListView.Items.Add(uie.ToString(), (int)uie.ElementType);
                enabledWhenField.Items.Add(uie.Identifier + " - " + uie.Name);
            }

            if (enabledWhenField.Items.Count > 0)
            {
                enabledWhenField.SelectedIndex = 0;
            }

            if (SelectItemIndex >= 0 && SelectItemIndex < ControlListView.Items.Count)
            {
                ControlListView.Items[SelectItemIndex].Selected = true;
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            if (dirty)
            {
                Update_Click(null, null);
            }
            UIControlsText = "";
            foreach (UIElement uie in MasterList)
            {
                UIControlsText += uie.ToSourceString(true, this.projectType);
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            int CurrentItem = (ControlListView.SelectedItems.Count > 0) ? ControlListView.SelectedItems[0].Index : -1;
            if (CurrentItem > -1)
            {
                IDList.Remove(MasterList[CurrentItem].Identifier);
                MasterList.RemoveAt(CurrentItem);
            }
            if (CurrentItem >= MasterList.Count)
            {
                CurrentItem--;
            }
            refreshListView(CurrentItem);
            dirty = false;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            ElementType elementType = ControlType.SelectedElementType;
            string defaultStr = (elementType == ElementType.ColorWheel) ? DefaultColorComboBox.SelectedItem.ToString() : ControlDef.Text;
            if (elementType == ElementType.Uri || elementType == ElementType.LabelComment) defaultStr = OptionsText.Text.Trim();
            string identifier = ControlID.Text.Trim();
            if (identifier.Length == 0 || IDList.Contains(identifier))
            {
                identifier = "Amount" + (MasterList.Count + 1);
            }
            string enableIdentifier = (this.rbEnabledWhen.Checked) ? MasterList[enabledWhenField.SelectedIndex].Identifier : string.Empty;
            MasterList.Add(new UIElement(elementType, ControlName.Text, ControlMin.Text, ControlMax.Text, defaultStr, OptionsText.Text, ControlStyle.SelectedIndex, rbEnabledWhen.Checked, enableIdentifier, (enabledWhenCondition.SelectedIndex != 0), identifier, null));
            IDList.Add(identifier);
            refreshListView(MasterList.Count - 1);
            dirty = false;
        }

        private void ControlType_SelectedIndexChanged(object sender, EventArgs e)
        {
            dirty = true;

            // reset options
            OptionsLabel.Text = "Options:";
            toolTip1.SetToolTip(this.OptionsText, "Separate options with the vertical bar character (|)");

            // setup UI based on selected control type
            switch (ControlType.SelectedElementType)
            {
                case ElementType.IntSlider:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = true;
                    ControlMax.Enabled = true;
                    ControlMin.Enabled = true;
                    ControlMax.Text = "100";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = true;
                    ControlStyle.Enabled = true;
                    FillStyleDropDown(0);
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.Checkbox:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlMin.Text = "0";
                    ControlMin.Enabled = false;
                    ControlMax.Text = "1";
                    ControlMax.Enabled = false;
                    ControlDef.Text = (int.TryParse(ControlDef.Text, out int result) && result > 0) ? "1" : "0";
                    ControlDef.Enabled = true;
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.ColorWheel:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    DefaultColorComboBox.Text = "None";
                    DefaultColorComboBox.Visible = true;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "16777215";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = true;
                    ControlStyle.Enabled = true;
                    FillStyleDropDown(1);
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.AngleChooser:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = true;
                    ControlMax.Enabled = true;
                    ControlMin.Enabled = true;
                    ControlMax.Text = "180";
                    ControlMin.Text = "-180";
                    ControlDef.Text = "45";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.PanSlider:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "1";
                    ControlMin.Text = "-1";
                    ControlDef.Text = "0.0,0.0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.Textbox:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = true;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "255";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.DoubleSlider:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = true;
                    ControlMax.Enabled = true;
                    ControlMin.Enabled = true;
                    ControlMax.Text = "10";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = true;
                    ControlStyle.Enabled = true;
                    FillStyleDropDown(0);
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.DropDown:
                    OptionsLabel.Visible = true;
                    OptionsLabel.Enabled = true;
                    OptionsText.Visible = true;
                    OptionsText.Enabled = true;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = false;
                    ControlMax.Visible = false;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = false;
                    MaximumLabel.Visible = false;
                    DefaultLabel.Visible = false;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "0";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    OptionsText.Text = "Option1|Option2";
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.BinaryPixelOp:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "0";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.FontFamily:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "0";
                    ControlMin.Text = "0";
                    ControlDef.Text = "Arial";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.RadioButtons:
                    OptionsLabel.Visible = true;
                    OptionsLabel.Enabled = true;
                    OptionsText.Visible = true;
                    OptionsText.Enabled = true;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = false;
                    ControlMax.Visible = false;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = false;
                    MaximumLabel.Visible = false;
                    DefaultLabel.Visible = false;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "0";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    OptionsText.Text = "Option1|Option2";
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.ReseedButton:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "255";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.MultiLineTextbox:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = true;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "32767";
                    ControlMin.Text = "1";
                    ControlDef.Text = "1";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.RollBall:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "1";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.Filename:
                    OptionsLabel.Visible = true;
                    OptionsLabel.Enabled = true;
                    OptionsText.Visible = true;
                    OptionsText.Enabled = true;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = false;
                    ControlMax.Visible = false;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = false;
                    MaximumLabel.Visible = false;
                    DefaultLabel.Visible = false;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "255";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    OptionsText.Text = "png|jpg|gif|bmp";
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.Uri:
                    OptionsLabel.Visible = true;
                    OptionsLabel.Enabled = true;
                    OptionsText.Visible = true;
                    OptionsText.Enabled = true;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = false;
                    ControlMax.Visible = false;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = false;
                    MaximumLabel.Visible = false;
                    DefaultLabel.Visible = false;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "255";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    OptionsText.Text = "https://www.GetPaint.net";
                    OptionsLabel.Text = "URL:";
                    toolTip1.SetToolTip(this.OptionsText, "URL must begin with 'http://' or 'https://' to be valid.");
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.Folder:
                    OptionsLabel.Visible = true;
                    OptionsLabel.Enabled = false;
                    OptionsText.Visible = true;
                    OptionsText.Enabled = false;
                    OptionsText.Text = string.Empty;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = false;
                    ControlMax.Visible = false;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = false;
                    MaximumLabel.Visible = false;
                    DefaultLabel.Visible = false;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "255";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
                case ElementType.LabelComment:
                    OptionsLabel.Visible = true;
                    OptionsLabel.Enabled = true;
                    OptionsText.Visible = true;
                    OptionsText.Enabled = true;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = false;
                    ControlMax.Visible = false;
                    ControlDef.Visible = false;
                    MinimumLabel.Visible = false;
                    MaximumLabel.Visible = false;
                    DefaultLabel.Visible = false;
                    ControlDef.Enabled = false;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "255";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    OptionsText.Text = "";
                    OptionsLabel.Text = "Comment:";
                    toolTip1.SetToolTip(this.OptionsText, "This is the comment that will appear in your UI.");
                    rbEnabled.Checked = true;
                    rbEnabled.Enabled = false;
                    rbEnabledWhen.Enabled = false;
                    break;
                case ElementType.LayerChooser:
                    OptionsLabel.Visible = false;
                    OptionsText.Visible = false;
                    DefaultColorComboBox.Visible = false;
                    ControlMin.Visible = true;
                    ControlMax.Visible = true;
                    ControlDef.Visible = true;
                    MinimumLabel.Visible = true;
                    MaximumLabel.Visible = true;
                    DefaultLabel.Visible = true;
                    ControlDef.Enabled = true;
                    ControlMax.Enabled = false;
                    ControlMin.Enabled = false;
                    ControlMax.Text = "9999";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                    StyleLabel.Enabled = false;
                    ControlStyle.Enabled = false;
                    ControlStyle.SelectedIndex = 0;
                    rbEnabled.Enabled = true;
                    rbEnabledWhen.Enabled = true;
                    break;
            }
        }

        private void FillStyleDropDown(int Style)
        {
            ControlStyle.Items.Clear();
            switch (Style)
            {
                case 1:
                    ControlStyle.Items.AddRange(new string[] {
                        "Default",
                        "Alpha",
                        "Default no Reset",
                        "Alpha no Reset"
                    });
                    break;
                case 0:
                default:
                    ControlStyle.Items.AddRange(new string[] {
                        "Default",
                        "Hue",
                        "Hue Centered",
                        "Saturation",
                        "White - Black",
                        "Black - White",
                        "Cyan - Red",
                        "Magenta - Green",
                        "Yellow - Blue",
                        "Cyan - Orange",
                        "White - Red",
                        "White - Green",
                        "White - Blue"
                    });
                    break;
            }
            ControlStyle.SelectedIndex = 0;
        }

        private void Update_Click(object sender, EventArgs e)
        {
            int CurrentItem = (ControlListView.SelectedItems.Count > 0) ? ControlListView.SelectedItems[0].Index : -1;

            ElementType elementType = ControlType.SelectedElementType;
            string defaultStr = (elementType == ElementType.ColorWheel) ? DefaultColorComboBox.SelectedItem.ToString() : ControlDef.Text;
            if (elementType == ElementType.Uri || elementType == ElementType.LabelComment) defaultStr = OptionsText.Text.Trim();
            string identifier = !string.IsNullOrWhiteSpace(ControlID.Text) ? ControlID.Text.Trim() : "Amount" + (MasterList.Count + 1);
            string enableIdentifier = (this.rbEnabledWhen.Checked) ? MasterList[enabledWhenField.SelectedIndex].Identifier : string.Empty;
            string typeEnum = (CurrentItem > -1) ? MasterList[CurrentItem].TEnum : null;
            UIElement uiElement = new UIElement(elementType, ControlName.Text, ControlMin.Text, ControlMax.Text, defaultStr, OptionsText.Text, ControlStyle.SelectedIndex, rbEnabledWhen.Checked, enableIdentifier, (enabledWhenCondition.SelectedIndex != 0), identifier, typeEnum);

            if (CurrentItem > -1)
            {
                MasterList.RemoveAt(CurrentItem);
                MasterList.Insert(CurrentItem, uiElement);
                refreshListView(CurrentItem);
            }
            else
            {
                MasterList.Add(uiElement);
                IDList.Add(identifier);
                refreshListView(MasterList.Count - 1);
            }
            dirty = false;
        }

        private void ControlName_TextChanged(object sender, EventArgs e)
        {
            dirty = true;
        }

        private void MoveUp_Click(object sender, EventArgs e)
        {
            if (ControlListView.SelectedItems.Count == 0)
            {
                return;
            }
            int CurrentItem = ControlListView.SelectedItems[0].Index;
            if (CurrentItem > 0)
            {
                UIElement TargetElement = MasterList[CurrentItem];
                MasterList.RemoveAt(CurrentItem);
                MasterList.Insert(CurrentItem - 1, TargetElement);
                refreshListView(CurrentItem - 1);
            }
        }

        private void MoveDown_Click(object sender, EventArgs e)
        {
            int CurrentItem = (ControlListView.SelectedItems.Count > 0) ? ControlListView.SelectedItems[0].Index : -1;
            if (CurrentItem >= 0 && CurrentItem < MasterList.Count - 1)
            {
                UIElement TargetElement = MasterList[CurrentItem];
                MasterList.RemoveAt(CurrentItem);
                MasterList.Insert(CurrentItem + 1, TargetElement);
                refreshListView(CurrentItem + 1);
            }
        }

        private void ControlListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            dirty = false;
            if (ControlListView.SelectedItems.Count == 0)
            {
                return;
            }

            int CurrentItem = ControlListView.SelectedItems[0].Index;
            if (CurrentItem == -1)
            {
                return;
            }

            UIElement CurrentElement = MasterList[CurrentItem];
            ControlName.Text = CurrentElement.Name;
            if (CurrentElement.EnabledWhen)
            {
                rbEnabled.Checked = false;
                rbEnabledWhen.Checked = true;
                for (int i = 0; i < enabledWhenField.Items.Count; i++)
                {
                    if (enabledWhenField.Items[i].ToString().StartsWith(CurrentElement.EnableIdentifier, StringComparison.Ordinal))
                    {
                        enabledWhenField.SelectedIndex = i;
                        break;
                    }
                }
                enabledWhenCondition.SelectedIndex = (CurrentElement.EnableSwap) ? 1 : 0;
            }
            else
            {
                rbEnabled.Checked = true;
                rbEnabledWhen.Checked = false;
            }

            int BarLoc;

            ControlType.SelectedElementType = CurrentElement.ElementType;

            switch (CurrentElement.ElementType)
            {
                case ElementType.IntSlider:
                    FillStyleDropDown(0);
                    ControlStyle.SelectedIndex = (int)CurrentElement.Style;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.Checkbox:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.ColorWheel:
                    FillStyleDropDown(1);
                    bool alpha = CurrentElement.ColorWheelOptions.HasFlag(ColorWheelOptions.Alpha);
                    bool noReset = CurrentElement.ColorWheelOptions.HasFlag(ColorWheelOptions.NoReset);
                    ControlStyle.SelectedIndex = (noReset && alpha) ? 3 : noReset ? 2 : alpha ? 1 : 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    DefaultColorComboBox.Text = (CurrentElement.StrDefault.Length == 0 ? "None" : CurrentElement.StrDefault);
                    break;
                case ElementType.AngleChooser:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.dMin.ToString();
                    ControlMax.Text = CurrentElement.dMax.ToString();
                    ControlDef.Text = CurrentElement.dDefault.ToString();
                    break;
                case ElementType.PanSlider:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.dMin.ToString();
                    ControlMax.Text = CurrentElement.dMax.ToString();
                    ControlDef.Text = CurrentElement.StrDefault;
                    break;
                case ElementType.Textbox:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.DoubleSlider:
                    FillStyleDropDown(0);
                    ControlStyle.SelectedIndex = (int)CurrentElement.Style;
                    ControlMin.Text = CurrentElement.dMin.ToString();
                    ControlMax.Text = CurrentElement.dMax.ToString();
                    ControlDef.Text = CurrentElement.dDefault.ToString();
                    break;
                case ElementType.DropDown:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    BarLoc = CurrentElement.Name.IndexOf("|", StringComparison.Ordinal);
                    OptionsText.Text = CurrentElement.Name.Substring(BarLoc + 1);
                    ControlName.Text = CurrentElement.ToShortName();
                    break;
                case ElementType.BinaryPixelOp:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.FontFamily:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.StrDefault.ToString();
                    break;
                case ElementType.RadioButtons:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.StrDefault;
                    BarLoc = CurrentElement.Name.IndexOf("|", StringComparison.Ordinal);
                    OptionsText.Text = CurrentElement.Name.Substring(BarLoc + 1);
                    ControlName.Text = CurrentElement.ToShortName();
                    break;
                case ElementType.ReseedButton:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.MultiLineTextbox:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.RollBall:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    break;
                case ElementType.Filename:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    BarLoc = CurrentElement.Name.IndexOf("|", StringComparison.Ordinal);
                    OptionsText.Text = CurrentElement.Name.Substring(BarLoc + 1);
                    ControlName.Text = CurrentElement.ToShortName();
                    break;
                case ElementType.Uri:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    OptionsText.Text = CurrentElement.StrDefault;
                    break;
                case ElementType.Folder:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.Default.ToString();
                    ControlName.Text = CurrentElement.ToShortName();
                    break;
                case ElementType.LabelComment:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    OptionsText.Text = CurrentElement.StrDefault;
                    break;
                case ElementType.LayerChooser:
                    ControlStyle.SelectedIndex = 0;
                    ControlMin.Text = CurrentElement.Min.ToString();
                    ControlMax.Text = CurrentElement.Max.ToString();
                    ControlDef.Text = CurrentElement.StrDefault.ToString();
                    break;
                default:
                    break;
            }

            this.currentID = CurrentElement.Identifier;
            ControlID.Text = CurrentElement.Identifier;
        }

        private void DefaultColorComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            e.DrawBackground();
            string colorName = DefaultColorComboBox.Items[e.Index].ToString();

            using (SolidBrush solidBrush = new SolidBrush(e.ForeColor))
            using (Font font = new Font(e.Font, FontStyle.Regular))
            {
                if (colorName == "None" || colorName == "PrimaryColor" || colorName == "SecondaryColor")
                {
                    e.Graphics.DrawString(colorName.SplitCamel(), font, solidBrush, e.Bounds);
                }
                else
                {
                    e.Graphics.DrawString(colorName.SplitCamel(), font, solidBrush, new Rectangle(e.Bounds.X + e.Bounds.Height, e.Bounds.Y + 1, e.Bounds.Width - e.Bounds.Height, e.Bounds.Height));

                    solidBrush.Color = Color.FromName(colorName);
                    Rectangle colorRect = new Rectangle(e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Height - 3, e.Bounds.Height - 3);

                    e.Graphics.FillRectangle(solidBrush, colorRect);
                    e.Graphics.DrawRectangle(Pens.Black, colorRect);
                }
            }
            e.DrawFocusRectangle();
        }

        private void rbEnabled_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnabledFields();
            dirty = true;
        }

        private void rbEnabledWhen_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnabledFields();
            dirty = true;
        }

        private void UpdateEnabledFields()
        {
            if (rbEnabled.Checked)
            {
                enabledWhenField.Enabled = false;
                enabledWhenCondition.Enabled = false;
            }
            else
            {
                enabledWhenField.Enabled = true;
                enabledWhenCondition.Enabled = true;
            }
        }

        private void enabledWhenField_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            e.DrawBackground();
            using (SolidBrush textBrush = new SolidBrush(e.State.HasFlag(DrawItemState.Disabled) ? Color.Gray : e.ForeColor))
            using (StringFormat textFormat = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                e.Graphics.DrawString(enabledWhenField.Items[e.Index].ToString(), e.Font, textBrush, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), textFormat);
            }
            e.DrawFocusRectangle();
        }

        private void enabledWhenCondition_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            e.DrawBackground();
            using (SolidBrush textBrush = new SolidBrush(e.State.HasFlag(DrawItemState.Disabled) ? Color.Gray : e.ForeColor))
            using (StringFormat textFormat = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                e.Graphics.DrawString(enabledWhenCondition.Items[e.Index].ToString(), e.Font, textBrush, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), textFormat);
            }
            e.DrawFocusRectangle();
        }

        private void ControlStyle_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            e.DrawBackground();
            using (SolidBrush textBrush = new SolidBrush(e.State.HasFlag(DrawItemState.Disabled) ? Color.Gray : e.ForeColor))
            using (StringFormat textFormat = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                e.Graphics.DrawString(ControlStyle.Items[e.Index].ToString(), e.Font, textBrush, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), textFormat);
            }
            e.DrawFocusRectangle();
        }

        private void ControlStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            dirty = true;
            if (ControlType.SelectedElementType == ElementType.ColorWheel)
            {
                if (ControlStyle.SelectedIndex == 0 || ControlStyle.SelectedIndex == 2)
                {
                    ControlMax.Text = "16777215";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                }
                else
                {
                    ControlMax.Text = int.MaxValue.ToString();
                    ControlMin.Text = int.MinValue.ToString();
                    ControlDef.Text = "0";
                }
            }
            else
            {
                if (ControlStyle.SelectedIndex == 1)
                {
                    ControlMax.Text = "360";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                }
                else if (ControlStyle.SelectedIndex == 2)
                {
                    ControlMax.Text = "180";
                    ControlMin.Text = "-180";
                    ControlDef.Text = "0";
                }
                else if (ControlStyle.SelectedIndex >= 6 && ControlStyle.SelectedIndex <= 9)
                {
                    ControlMax.Text = "255";
                    ControlMin.Text = "-255";
                    ControlDef.Text = "0";
                }
                else if (ControlStyle.SelectedIndex != 0)
                {
                    ControlMax.Text = "100";
                    ControlMin.Text = "0";
                    ControlDef.Text = "0";
                }
            }
        }

        private void enabledWhenCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            dirty = true;
        }

        private void enabledWhenField_SelectedIndexChanged(object sender, EventArgs e)
        {
            dirty = true;
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
#if !FASTDEBUG
            if (this.projectType.IsEffect())
            {
                PreviewEffect(this.projectType);
            }
            else if (this.projectType == ProjectType.FileType)
            {
                PreviewFileType();
            }
#endif
            return;
        }

        private void PreviewEffect(ProjectType projectType)
        {
            string uiCode = MasterList.Select(uiE => uiE.ToSourceString(false, projectType)).Join("");
            string previewSourceCode = BitmapEffectWriter.UiPreview(uiCode);

            if (!ScriptBuilder.BuildEffect<BitmapEffect>(previewSourceCode))
            {
                FlexibleMessageBox.Show("Something went wrong, and the Preview can't be displayed.", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!ScriptBuilder.BuiltEffect.Options.IsConfigurable)
            {
                FlexibleMessageBox.Show("There are no UI controls, so the Preview can't be displayed.", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                using Surface emptySurface = new Surface(this.environmentParameters.Document.Size);
                emptySurface.Fill(ColorBgra.White);
                using IBitmap<ColorBgra32> bitmap = emptySurface.CreateSharedBitmap();
                using IEffectEnvironment enviroParams = environmentParameters.CloneWithNewSource(bitmap);
                using IEffect effect = ScriptBuilder.BuiltEffect.EffectInfo.CreateInstance(this.serviceProvider, enviroParams);
                using IEffectConfigForm effectConfigDialog = effect.CreateConfigForm();
                effectConfigDialog.ShowDialog(this);
            }
        }

        private void PreviewFileType()
        {
            string code = "#region UICode\r\n";
            code += MasterList.Select(uiE => uiE.ToSourceString(false, ProjectType.FileType)).Join("");
            code += "\r\n";
            code += "#endregion\r\n";
            code += "void SaveImage(Document input, Stream output, PropertyBasedSaveConfigToken token, Surface scratchSurface, ProgressEventHandler progressCallback)\r\n";
            code += "{}\r\n";
            code += "Document LoadImage(Stream input)\r\n";
            code += "{ return new Document(400, 300); }\r\n";

            string fileTypeSourceCode = FileTypeWriter.Run(code, false);

            if (!ScriptBuilder.BuildFileType(fileTypeSourceCode))
            {
                MessageBox.Show("Compilation Failed!");
                return;
            }

            if (!ScriptBuilder.BuiltFileType.SupportsConfiguration)
            {
                MessageBox.Show("No Config!");
                return;
            }

            using (SaveWidgetDialog widgetDialog = new SaveWidgetDialog(
                ScriptBuilder.BuiltFileType.CreateSaveConfigWidget(),
                ScriptBuilder.BuiltFileType.CreateDefaultSaveConfigToken()))
            {
                widgetDialog.ShowDialog();
            }
        }

        private void ControlMax_Leave(object sender, EventArgs e)
        {
            double dMin = 0;
            double dMax = 0;
            double dDef = 0;

            if (!double.TryParse(ControlMax.Text, out dMax)) dMax = 100;
            if (!double.TryParse(ControlMin.Text, out dMin)) dMin = 0;
            if (!double.TryParse(ControlDef.Text, out dDef)) dDef = 0;

            ElementType elementType = ControlType.SelectedElementType;
            if (elementType != ElementType.AngleChooser &&
                elementType != ElementType.DoubleSlider)
            {
                dMax = Math.Truncate(dMax);
                ControlMax.Text = dMax.ToString();
                dMin = Math.Truncate(dMin);
                ControlMin.Text = dMin.ToString();
                dDef = Math.Truncate(dDef);
                ControlDef.Text = dDef.ToString();
            }

            if (dMax < dMin)
            {
                dMax = dMin;
                ControlMax.Text = dMax.ToString();
            }

            if (dDef > dMax)
            {
                dDef = dMax;
                ControlDef.Text = dDef.ToString();
            }
            dirty = true;
        }

        private void ControlDef_Leave(object sender, EventArgs e)
        {
            double dMin = 0;
            double dMax = 0;
            double dDef = 0;

            if (!double.TryParse(ControlMax.Text, out dMax)) dMax = 100;
            if (!double.TryParse(ControlMin.Text, out dMin)) dMin = 0;
            if (!double.TryParse(ControlDef.Text, out dDef)) dDef = 0;

            ElementType elementType = ControlType.SelectedElementType;
            if (elementType != ElementType.AngleChooser &&
                elementType != ElementType.DoubleSlider)
            {
                dMax = Math.Truncate(dMax);
                ControlMax.Text = dMax.ToString();
                dMin = Math.Truncate(dMin);
                ControlMin.Text = dMin.ToString();
                dDef = Math.Truncate(dDef);
                ControlDef.Text = dDef.ToString();
            }

            if (dDef < dMin)
            {
                dDef = dMin;
                ControlDef.Text = dDef.ToString();
            }

            if (dDef > dMax)
            {
                dDef = dMax;
                ControlDef.Text = dDef.ToString();
            }
            dirty = true;
        }

        private void ControlMin_Leave(object sender, EventArgs e)
        {
            double dMin = 0;
            double dMax = 0;
            double dDef = 0;

            if (!double.TryParse(ControlMax.Text, out dMax)) dMax = 100;
            if (!double.TryParse(ControlMin.Text, out dMin)) dMin = 0;
            if (!double.TryParse(ControlDef.Text, out dDef)) dDef = 0;

            ElementType elementType = ControlType.SelectedElementType;
            if (elementType != ElementType.AngleChooser &&
                elementType != ElementType.DoubleSlider)
            {
                dMax = Math.Truncate(dMax);
                ControlMax.Text = dMax.ToString();
                dMin = Math.Truncate(dMin);
                ControlMin.Text = dMin.ToString();
                dDef = Math.Truncate(dDef);
                ControlDef.Text = dDef.ToString();
            }

            if (dMin > dMax)
            {
                dMin = dMax;
                ControlMin.Text = dMin.ToString();
            }

            if (dDef < dMin)
            {
                dDef = dMin;
                ControlDef.Text = dDef.ToString();
            }
            dirty = true;
        }

        private void ControlID_TextChanged(object sender, EventArgs e)
        {
            dirty = true;
            string newID = ControlID.Text.Trim();
            bool error = (newID.Length == 0 || (newID != this.currentID && IDList.Contains(newID)) || !newID.IsCSharpIdentifier());
            ControlID.ForeColor = error ? Color.Black : Color.Black;
            ControlID.BackColor = error ? Color.FromArgb(246, 97, 81) : Color.White;
        }

        private void OptionsText_TextChanged(object sender, EventArgs e)
        {
            dirty = true;

            if (!OptionsText.Enabled)
            {
                OptionsText.ForeColor = Color.Black;
                OptionsText.BackColor = Color.White;
                return;
            }

            string newOptions = OptionsText.Text.Trim().ToLowerInvariant();
            bool error = false;
            if (ControlType.SelectedElementType == ElementType.Uri)
            {
                // Make sure the URL is valid.
                if (!newOptions.IsWebAddress())
                {
                    error = true;
                }
                else
                {
                    try
                    {
                        Uri uri = new Uri(newOptions);
                    }
                    catch
                    {
                        error = true;
                    }
                }
            }
            else if (ControlType.SelectedElementType == ElementType.LabelComment)
            {
                if (newOptions.Contains('"', StringComparison.Ordinal))
                {
                    error = true;
                }
            }
            else
            {
                // Make sure it looks like options (should contain at least one | character.
                // Although not TECHNICALLY required... let's make it required anyway.
                if (!newOptions.Contains('|', StringComparison.Ordinal))
                {
                    error = true;
                }
            }
            OptionsText.ForeColor = error ? Color.Black : Color.Black;
            OptionsText.BackColor = error ? Color.FromArgb(246, 97, 81) : Color.White;
        }
    }

    public class ControlTypeComboBox : ComboBox
    {
        private ProjectType projectType = ProjectType.Default;

        internal ProjectType ProjectType
        {
            get
            {
                return projectType;
            }
            set
            {
                projectType = value;

                ControlTypeItem[] controlTypes = Enum.GetValues<ElementType>()
                   .Where(et => UIElement.IsControlAllowed(et, projectType))
                   .Select(et => new ControlTypeItem(et))
                   .ToArray();

                this.Items.Clear();
                this.Items.AddRange(controlTypes);
            }
        }

        internal ElementType SelectedElementType
        {
            get
            {
                return this.SelectedItem is ControlTypeItem item
                    ? item.ElementType
                    : ElementType.IntSlider;
            }
            set
            {
                int index = -1;

                for (int i = 0; i < this.Items.Count; i++)
                {
                    if (this.Items[i] is ControlTypeItem item && item.ElementType == value)
                    {
                        index = i;
                        break;
                    }
                }

                this.SelectedIndex = index;
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            e.DrawBackground();
            if (this.Items[e.Index] is ControlTypeItem item)
            {
                int iconSize = UIUtil.Scale(16);
                e.Graphics.DrawImage(UIBuilder.ControlIcons[(int)item.ElementType], e.Bounds.X + 2, e.Bounds.Y + 1, iconSize, iconSize);
                Rectangle textBounds = Rectangle.FromLTRB(e.Bounds.Left + iconSize + 4, e.Bounds.Top + 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(e.Graphics, item.ToString(), e.Font, textBounds, e.ForeColor, TextFormatFlags.VerticalCenter);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, this.Items[e.Index].ToString(), e.Font, e.Bounds.Location, e.ForeColor);
            }
            e.DrawFocusRectangle();

            base.OnDrawItem(e);
        }

        private class ControlTypeItem : IComparable<ControlTypeItem>
        {
            private readonly string text;
            internal ElementType ElementType { get; }

            internal ControlTypeItem(ElementType elementType)
            {
                this.text = elementType.GetDescription() ?? elementType.ToString();
                this.ElementType = elementType;
            }

            public override string ToString()
            {
                return this.text;
            }

            public int CompareTo(ControlTypeItem other)
            {
                return string.Compare(this.text, other.text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    internal class UIElement
    {
        private readonly string Description;
        internal readonly string Name;
        internal readonly ElementType ElementType;
        internal readonly int Min;
        internal readonly int Max;
        internal readonly int Default;
        internal readonly string StrDefault;
        internal readonly ColorWheelOptions ColorWheelOptions;
        internal readonly double dMin;
        internal readonly double dMax;
        internal readonly double dDefault;
        internal readonly SliderStyle Style;
        internal readonly bool EnabledWhen;
        internal readonly bool EnableSwap;
        internal readonly string EnableIdentifier;
        internal readonly string Identifier;
        internal readonly string TEnum;

        private static readonly string[] NewSourceCodeType = {
            "IntSliderControl",         // 0
            "CheckboxControl",          // 1
            "ColorWheelControl",        // 2
            "AngleControl",             // 3
            "PanSliderControl",         // 4
            "TextboxControl",           // 5
            "DoubleSliderControl",      // 6
            "ListBoxControl",           // 7
            "BinaryPixelOp",            // 8
            "FontFamily",               // 9
            "RadioButtonControl",       // 10
            "ReseedButtonControl",      // 11
            "MultiLineTextboxControl",  // 12
            "RollControl",              // 13
            "FilenameControl",          // 14
            "Uri",                      // 15
            "FolderControl",            // 16
            "LabelComment",             // 17
            "LayerControl",             // 18
        };

        internal static UIElement[] ProcessUIControls(string SourceCode, ProjectType projectType)
        {
            string UIControlsText = "";
            Match mcc = Regex.Match(SourceCode, @"\#region UICode(?<sublabel>.*?)\#endregion", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (mcc.Success)
            {
                // We found the standard #region UICode/#endregion block
                UIControlsText = mcc.Groups["sublabel"].Value.Trim();
            }
            else
            {
                // Find standard UI controls from REALLY OLD scripts
                Match ma1 = Regex.Match(SourceCode, @"int\s+Amount1\s*=\s*\-?\d+.*\n", RegexOptions.IgnoreCase);
                if (ma1.Success)
                {
                    UIControlsText = ma1.Value;
                    Match ma2 = Regex.Match(SourceCode, @"int\s+Amount2\s*=\s*\-?\d+.*\n", RegexOptions.IgnoreCase);
                    if (ma2.Success)
                    {
                        UIControlsText += ma2.Value;
                        Match ma3 = Regex.Match(SourceCode, @"int\s+Amount3\s*=\s*\-?\d+.*\n", RegexOptions.IgnoreCase);
                        if (ma3.Success)
                        {
                            UIControlsText += ma3.Value;
                        }
                    }
                }
            }

            if (UIControlsText.Length == 0)
            {
                return Array.Empty<UIElement>();
            }

            return UIControlsText
                .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !x.StartsWith("//", StringComparison.Ordinal))
                .Select(x => FromSourceLine(x))
                .Where(x => x != null && IsControlAllowed(x.ElementType, projectType))
                .ToArray();
        }

        internal static bool IsControlAllowed(ElementType elementType, ProjectType projectType)
        {
            if (projectType.Is5Effect())
            {
                return true;
            }

            if (projectType == ProjectType.ClassicEffect)
            {
                return elementType != ElementType.LayerChooser;
            }

            if (projectType == ProjectType.FileType)
            {
                switch (elementType)
                {
                    case ElementType.IntSlider:
                    case ElementType.Checkbox:
                    case ElementType.Uri:
                    case ElementType.Textbox:
                    case ElementType.MultiLineTextbox:
                    case ElementType.DoubleSlider:
                    case ElementType.DropDown:
                    case ElementType.RadioButtons:
                    case ElementType.LabelComment:
                    case ElementType.LayerChooser:
                        return true;
                }
            }

            return false;
        }

        internal UIElement(ElementType eType, string eName, string eMin, string eMax, string eDefault, string eOptions, int eStyle, bool eEnabled, string targetIdentifier, bool eSwap, string identifier, string typeEnum)
        {
            Name = eName;
            ElementType = eType;
            Identifier = identifier;

            if (!double.TryParse(eMax, out double parsedMax)) parsedMax = 100;
            if (!double.TryParse(eMin, out double parsedMin)) parsedMin = 0;
            if (!double.TryParse(eDefault, out double parsedDefault)) parsedDefault = 0;

            string EnabledDescription = "";
            if (eEnabled)
            {
                EnabledWhen = true;
                EnableIdentifier = targetIdentifier;
                EnableSwap = eSwap;

                EnabledDescription += " {";
                if (EnableSwap)
                {
                    EnabledDescription += "!";
                }
                EnabledDescription += targetIdentifier + "} ";
            }

            switch (eType)
            {
                case ElementType.IntSlider:
                    Min = (int)parsedMin;
                    Max = (int)parsedMax;
                    Default = (int)parsedDefault;
                    Style = Enum.IsDefined(typeof(SliderStyle), eStyle) ? (SliderStyle)eStyle : 0;
                    Description = eName + " (" + Min.ToString() + ".." + Default.ToString() + ".." + Max.ToString() + ")" + EnabledDescription;
                    break;
                case ElementType.Checkbox:
                    Max = 1;
                    Default = (int)parsedDefault;
                    if (Default != 0)
                    {
                        Default = 1;
                    }
                    Description = eName + ((Default == 0) ? " (unchecked)" : " (checked)") + EnabledDescription;
                    break;
                case ElementType.ColorWheel:
                    StrDefault = (eDefault == "None") ? string.Empty : eDefault;
                    ColorWheelOptions = (eStyle == 3) ? ColorWheelOptions.Alpha | ColorWheelOptions.NoReset : (eStyle == 2) ? ColorWheelOptions.NoReset : (eStyle == 1) ? ColorWheelOptions.Alpha : ColorWheelOptions.None;
                    Description = eName;
                    bool alpha = ColorWheelOptions.HasFlag(ColorWheelOptions.Alpha);
                    Min = alpha ? int.MinValue : 0;
                    Max = alpha ? int.MaxValue : 0xffffff;
                    if (StrDefault.Length > 0)
                    {
                        string alphaStyle = alpha ? "?" : "";
                        string resetStyle = ColorWheelOptions.HasFlag(ColorWheelOptions.NoReset) ? "!" : "";
                        Description += " (" + StrDefault + alphaStyle + resetStyle + ")";
                    }
                    Description += EnabledDescription;
                    break;
                case ElementType.AngleChooser:
                    dMin = Math.Clamp(parsedMin, -180.0, 360.0);
                    double upperBound = (dMin < 0.0) ? 180.0 : 360;
                    dMax = Math.Clamp(parsedMax, dMin, upperBound);
                    dDefault = Math.Clamp(parsedDefault, dMin, dMax);
                    Description = eName + " (" + dMin.ToString() + ".." + dDefault.ToString() + ".." + dMax.ToString() + ")" + EnabledDescription;
                    break;
                case ElementType.PanSlider:
                    dMin = -1;
                    dMax = 1;
                    StrDefault = eDefault;
                    Description = eName + EnabledDescription;
                    break;
                case ElementType.Textbox:
                    Max = (int)parsedMax;
                    Description = eName + " (" + Max.ToString() + ")" + EnabledDescription;
                    break;
                case ElementType.DoubleSlider:
                    dMin = parsedMin;
                    dMax = parsedMax;
                    dDefault = parsedDefault;
                    Style = Enum.IsDefined(typeof(SliderStyle), eStyle) ? (SliderStyle)eStyle : 0;
                    Description = eName + " (" + dMin.ToString() + ".." + dDefault.ToString() + ".." + dMax.ToString() + ")" + EnabledDescription;
                    break;
                case ElementType.DropDown:
                case ElementType.RadioButtons:
                    Name += "|" + eOptions;
                    int maxValue = Name.Split('|').Length - 2;
                    Default = (int)Math.Clamp(parsedDefault, 0, maxValue);
                    StrDefault = eDefault;
                    int nameLength1 = Name.IndexOf("|", StringComparison.Ordinal);
                    if (nameLength1 == -1) nameLength1 = Name.Length;
                    Description = Name.Substring(0, nameLength1) + EnabledDescription;
                    TEnum = typeEnum;
                    break;
                case ElementType.BinaryPixelOp:
                    Description = eName + " (Normal)" + EnabledDescription;
                    break;
                case ElementType.FontFamily:
                    Description = eName + EnabledDescription;
                    StrDefault = eDefault;
                    break;
                case ElementType.ReseedButton:
                    Max = 255;
                    Description = eName + " (Button)" + EnabledDescription;
                    break;
                case ElementType.MultiLineTextbox:
                    Min = 1;
                    Max = (int)parsedMax;
                    Default = 1;
                    Description = eName + " (" + Max.ToString() + ")" + EnabledDescription;
                    break;
                case ElementType.RollBall:
                    Max = 1;
                    Description = eName + EnabledDescription;
                    break;
                case ElementType.Filename:
                    Name += "|" + eOptions;
                    int nameLength2 = Name.IndexOf("|", StringComparison.Ordinal);
                    if (nameLength2 == -1) nameLength2 = Name.Length;
                    Description = Name.Substring(0, nameLength2) + EnabledDescription;
                    break;
                case ElementType.Uri:
                    Max = 255;
                    StrDefault = eDefault;
                    Description = eName + " (Web Link)" + EnabledDescription;
                    break;
                case ElementType.Folder:
                    Description = eName + EnabledDescription;
                    break;
                case ElementType.LabelComment:
                    StrDefault = eDefault;
                    Description = eDefault;
                    break;
                case ElementType.LayerChooser:
                    Description = eName + EnabledDescription;
                    StrDefault = eDefault;
                    Min = 0;
                    Max = (int)parsedMax;
                    break;
            }
        }

        private static UIElement FromSourceLine(string RawSourceLine)
        {
            Match m = Regex.Match(RawSourceLine, @"\s*(?<type>.*)\s+(?<identifier>.+\b)\s*=\s*(?<default>.*);\s*\/{2}(?<rawcomment>.*)");
            if (!m.Success)
            {
                // don't understand raw source line
                return null;
            }

            string MinimumStr = "";
            string MaximumStr = "";
            string DefaultColor = "";
            string LabelStr = "";
            string StyleStr = "0";

            string rawComment = m.Groups["rawcomment"].Value;
            Match m0 = Regex.Match(rawComment, @"\s*\[\s*(?<minimum>\-?\d+.*\d*)\s*\,\s*(?<maximum>\-?\d+.*\d*)\s*\,\s*(?<style>\-?\d+.*\d*)\s*\](?<label>.*)");
            if (m0.Success)
            {
                MinimumStr = m0.Groups["minimum"].Value;
                MaximumStr = m0.Groups["maximum"].Value;
                StyleStr = m0.Groups["style"].Value;
                LabelStr = m0.Groups["label"].Value.Trim();
            }
            else
            {
                Match m1 = Regex.Match(rawComment, @"\s*\[\s*(?<minimum>\-?\d+.*\d*)\s*\,\s*(?<maximum>\-?\d+[^]]*\d*)\s*\](?<label>.*)");
                if (m1.Success)
                {
                    MinimumStr = m1.Groups["minimum"].Value;
                    MaximumStr = m1.Groups["maximum"].Value;
                    LabelStr = m1.Groups["label"].Value.Trim();
                }
                else
                {
                    Match m2 = Regex.Match(rawComment, @"\s*\[\s*(?<maximum>\-?\d+.*\d*)\s*\](?<label>.*)");
                    if (m2.Success)
                    {
                        MinimumStr = "0";
                        MaximumStr = m2.Groups["maximum"].Value;
                        LabelStr = m2.Groups["label"].Value.Trim();
                    }
                    else
                    {
                        Match m3 = Regex.Match(rawComment, @"\s*\[\s*(?<defcolor>.*)\s*\](?<label>.*)");
                        if (m3.Success)
                        {
                            DefaultColor = m3.Groups["defcolor"].Value;
                            LabelStr = m3.Groups["label"].Value.Trim();
                        }
                        else
                        {
                            Match m1L = Regex.Match(rawComment, @"\s*(?<label>.*)");
                            if (m1L.Success)
                            {
                                LabelStr = m1L.Groups["label"].Value.Trim();
                            }
                        }
                    }
                }
            }

            bool enabled = false;
            string targetID = string.Empty;
            bool swap = false;

            Match me = Regex.Match(LabelStr, @"\s*{(?<swap>\!?)(?<identifier>.+\b)\s*}\s*(?<label>.*)");
            if (me.Success)
            {
                LabelStr = me.Groups["label"].Value.Trim();

                enabled = true;
                targetID = me.Groups["identifier"].Value;
                swap = me.Groups["swap"].Value.Trim() == "!";
            }

            string DefaultStr = m.Groups["default"].Value.Trim();
            ElementType elementType = ElementType.IntSlider;

            string TypeStr = m.Groups["type"].Value.Trim();
            Match mTEnum = Regex.Match(TypeStr, @"(?<Type>\S+)<(?<TEnum>\S+)>");
            if (mTEnum.Success)
            {
                TypeStr = mTEnum.Groups["Type"].Value;
            }

            if (TypeStr == "IntSliderControl")
            {
                elementType = ElementType.IntSlider;
            }
            else if (TypeStr == "CheckboxControl")
            {
                elementType = ElementType.Checkbox;
            }
            else if (TypeStr == "ColorWheelControl")
            {
                elementType = ElementType.ColorWheel;
            }
            else if (TypeStr == "AngleControl")
            {
                elementType = ElementType.AngleChooser;
            }
            else if (TypeStr == "DoubleSliderControl")
            {
                elementType = ElementType.DoubleSlider;
            }
            else if (TypeStr == "PanSliderControl")
            {
                elementType = ElementType.PanSlider;
            }
            else if (TypeStr == "TextboxControl")
            {
                elementType = ElementType.Textbox;
            }
            else if (TypeStr == "MultiLineTextboxControl")
            {
                elementType = ElementType.MultiLineTextbox;
            }
            else if (TypeStr == "ReseedButtonControl")
            {
                elementType = ElementType.ReseedButton;
            }
            else if (TypeStr == "ListBoxControl")
            {
                elementType = ElementType.DropDown;
            }
            else if (TypeStr == "RadioButtonControl")
            {
                elementType = ElementType.RadioButtons;
            }
            else if (TypeStr == "UserBlendOp" || TypeStr == "BinaryPixelOp")
            {
                elementType = ElementType.BinaryPixelOp;
            }
            else if (TypeStr == "FontFamily")
            {
                elementType = ElementType.FontFamily;
            }
            else if (TypeStr == "RollControl")
            {
                elementType = ElementType.RollBall;
            }
            else if (TypeStr == "FilenameControl")
            {
                elementType = ElementType.Filename;
            }
            else if (TypeStr == "Uri")
            {
                elementType = ElementType.Uri;
            }
            else if (TypeStr == "FolderControl")
            {
                elementType = ElementType.Folder;
            }
            else if (TypeStr == "LabelComment")
            {
                elementType = ElementType.LabelComment;
            }
            else if (TypeStr == "LayerControl")
            {
                elementType = ElementType.LayerChooser;
                MaximumStr = "9999";
            }
            #region Detections for legacy scripts
            else if (TypeStr == "bool")
            {
                elementType = ElementType.Checkbox;
            }
            else if (TypeStr == "int")
            {
                elementType = ElementType.IntSlider;
            }
            else if (TypeStr == "ColorBgra")
            {
                elementType = ElementType.ColorWheel;
            }
            else if (TypeStr == "string")
            {
                elementType = (int.TryParse(MinimumStr, out int min) && min > 0) ? ElementType.MultiLineTextbox : ElementType.Textbox;
            }
            else if (TypeStr == "byte")
            {
                if (!LabelStr.Contains('|', StringComparison.Ordinal) || (MaximumStr == "255"))
                {
                    elementType = ElementType.ReseedButton;
                }
                else if (MaximumStr.Length == 0)
                {
                    elementType = ElementType.DropDown;
                }
                else if (MaximumStr == "1")
                {
                    elementType = ElementType.RadioButtons;
                }
            }
            else if (TypeStr == "Pair<double, double>")
            {
                elementType = ElementType.PanSlider;
            }
            else if (TypeStr == "Tuple<double, double, double>")
            {
                elementType = ElementType.RollBall;
            }
            else if (TypeStr == "double")
            {
                if (int.TryParse(MinimumStr, out int iMin) && (iMin == -180) &&
                    int.TryParse(MaximumStr, out int iMax) && (iMax == 180) &&
                    int.TryParse(DefaultStr, out int iDefault) && (iDefault == 45))
                {
                    elementType = ElementType.AngleChooser;
                }
                else
                {
                    elementType = ElementType.DoubleSlider;
                }
            }
            #endregion
            else
            {
                return null;
            }

            if (!int.TryParse(StyleStr, out int style))
            {
                style = 0;
            }

            if (!double.TryParse(MaximumStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double dMax))
            {
                dMax = 10;
            }

            if (!double.TryParse(MinimumStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double dMin))
            {
                dMin = 0;
            }

            string defaultValue = "";
            if (elementType == ElementType.ColorWheel)
            {
                if (DefaultColor.EndsWith("?!", StringComparison.Ordinal)) // Alpha - No Reset
                {
                    defaultValue = DefaultColor.Substring(0, DefaultColor.Length - 2);
                    style = 3;
                }
                else if (DefaultColor.EndsWith("?", StringComparison.Ordinal)) // Alpha - Reset
                {
                    defaultValue = DefaultColor.Substring(0, DefaultColor.Length - 1);
                    style = 1;
                }
                else if (DefaultColor.EndsWith("!", StringComparison.Ordinal)) // No Alpha - No Reset
                {
                    defaultValue = DefaultColor.Substring(0, DefaultColor.Length - 1);
                    style = 2;
                }
                else // No Alpha - Reset
                {
                    defaultValue = DefaultColor;
                    style = 0;
                }
            }
            else if (elementType == ElementType.FontFamily)
            {
                Match mFont = Regex.Match(DefaultStr, @"\bnew\s+FontFamily\s*\(\s*""(?<fontName>.*?)""\s*\)");
                if (mFont.Success)
                {
                    string foundFontName = mFont.Groups["fontName"].Value.Trim();
                    defaultValue = (foundFontName.Length > 0) ? foundFontName : "Arial";
                }
                else
                {
                    defaultValue = "Arial";
                }
            }
            else if (elementType == ElementType.Uri)
            {
                Match muri = Regex.Match(DefaultStr, @"""(?<uri>.*?[^\\])""");
                if (muri.Success)
                {
                    defaultValue = muri.Groups["uri"].Value;
                }
            }
            else if (elementType == ElementType.Checkbox)
            {
                defaultValue = DefaultStr.Contains("true", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            }
            else if (elementType == ElementType.LabelComment)
            {
                Match mComment = Regex.Match(DefaultStr, @"""(?<comment>.*?[^\\])""");
                if (mComment.Success)
                {
                    defaultValue = mComment.Groups["comment"].Value;
                }
            }
            else if (elementType == ElementType.LayerChooser)
            {
                defaultValue = DefaultStr;
                dMax = 9999;
            }
            else if (elementType == ElementType.PanSlider)
            {
                double x = 0;
                double y = 0;
                Match xyPair = Regex.Match(DefaultStr, @"\bPair.Create\(\s*(?<x>-?\s*\d*.?\d*)\s*,\s*(?<y>-?\s*\d*.?\d*)\s*\)");
                if (xyPair.Success)
                {
                    if (double.TryParse(xyPair.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                    {
                        x = Math.Clamp(x, -1, 1);
                    }
                    if (double.TryParse(xyPair.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    {
                        y = Math.Clamp(y, -1, 1);
                    }
                }

                defaultValue = x.ToString("F3", CultureInfo.InvariantCulture) + ", " + y.ToString("F3", CultureInfo.InvariantCulture);
            }
            else if (mTEnum.Success && (elementType == ElementType.DropDown || elementType == ElementType.RadioButtons))
            {
                defaultValue = DefaultStr;
            }
            else
            {
                if (!double.TryParse(DefaultStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double dDefault))
                {
                    dDefault = 0;
                }
                defaultValue = dDefault.ToString();
            }

            string id = m.Groups["identifier"].Value;

            int pipeIndex = LabelStr.IndexOf('|', StringComparison.Ordinal);
            string name = (pipeIndex > -1) ? LabelStr.Substring(0, pipeIndex) : LabelStr;
            string options = (pipeIndex > -1) ? LabelStr.Substring(pipeIndex + 1) : string.Empty;
            string typeEnum = mTEnum.Success ? mTEnum.Groups["TEnum"].Value : null;

            return new UIElement(elementType, name, dMin.ToString(), dMax.ToString(), defaultValue, options, style, enabled, targetID, swap, id, typeEnum);
        }

        public override string ToString()
        {
            return Identifier + " — " + Description;
        }

        internal string ToSourceString(bool useTEnum, ProjectType projectType)
        {
            bool hasTEnum = useTEnum && !TEnum.IsNullOrEmpty();
            string typeEnum = hasTEnum ? $"<{TEnum}>" : string.Empty;
            string SourceCode = NewSourceCodeType[(int)ElementType] + typeEnum + " " + Identifier;
            switch (ElementType)
            {
                case ElementType.IntSlider:
                    SourceCode += " = " + Default.ToString();
                    SourceCode += "; // [" + Min.ToString() + "," + Max.ToString();
                    if (Style > 0)
                    {
                        SourceCode += "," + (int)Style;
                    }
                    SourceCode += "] ";
                    break;
                case ElementType.AngleChooser:
                case ElementType.DoubleSlider:
                    SourceCode += " = " + dDefault.ToString(CultureInfo.InvariantCulture);
                    SourceCode += "; // [" + dMin.ToString(CultureInfo.InvariantCulture) + "," + dMax.ToString(CultureInfo.InvariantCulture);
                    if (Style > 0)
                    {
                        SourceCode += "," + (int)Style;
                    }
                    SourceCode += "] ";
                    break;
                case ElementType.Checkbox:
                    SourceCode += " = " + ((Default == 0) ? "false" : "true");
                    SourceCode += "; // ";
                    break;
                case ElementType.ColorWheel:
                    Color c;
                    if (StrDefault.Length == 0 || StrDefault == "PrimaryColor")
                    {
                        c = Color.Black;
                    }
                    else if (StrDefault == "SecondaryColor")
                    {
                        c = Color.White;
                    }
                    else
                    {
                        c = Color.FromName(StrDefault);
                    }

                    string rgb = c.B.ToString() + ", " + c.G.ToString() + ", " + c.R.ToString();
                    string resetStyle = ColorWheelOptions.HasFlag(ColorWheelOptions.NoReset) ? "!" : "";
                    string alphaStyle = "";

                    if (ColorWheelOptions.HasFlag(ColorWheelOptions.Alpha))
                    {
                        alphaStyle = "?";
                        SourceCode += " = ColorBgra.FromBgra(" + rgb + ", 255)";
                    }
                    else
                    {
                        SourceCode += " = ColorBgra.FromBgr(" + rgb + ")";
                    }

                    SourceCode += "; // ";

                    string config = StrDefault.Trim() + alphaStyle + resetStyle;
                    if (config.Length > 0)
                    {
                        SourceCode += "[" + config + "] ";
                    }
                    break;
                case ElementType.PanSlider:
                    SourceCode += " = new Vector2Double(" + StrDefault + "); // ";
                    break;
                case ElementType.Textbox:
                case ElementType.MultiLineTextbox:
                    SourceCode += " = \"\"";
                    SourceCode += "; // [" + Max.ToString() + "] ";
                    break;
                case ElementType.DropDown:
                case ElementType.RadioButtons:
                    string listDefault = hasTEnum ? StrDefault : Default.ToString();
                    SourceCode += " = " + listDefault + "; // ";
                    break;
                case ElementType.BinaryPixelOp:
                    SourceCode += " = LayerBlendModeUtil.CreateCompositionOp(LayerBlendMode.Normal); // ";
                    break;
                case ElementType.FontFamily:
                    if (projectType == ProjectType.ClassicEffect)
                    {
                        SourceCode += " = new FontFamily(\"" + StrDefault + "\"); // ";
                    }
                    else
                    {
                        SourceCode += " = \"" + StrDefault + "\"; // ";
                    }
                    break;
                case ElementType.ReseedButton:
                    SourceCode += " = 0; // ";
                    break;
                case ElementType.RollBall:
                    SourceCode += " = new Vector3Double(0.0, 0.0, 0.0)";
                    SourceCode += "; // ";
                    break;
                case ElementType.Filename:
                    SourceCode += " = @\"\"; // ";
                    break;
                case ElementType.Uri:
                    SourceCode += " = new Uri(\"" + StrDefault + "\"); // ";
                    break;
                case ElementType.Folder:
                    SourceCode += " = @\"\"; // ";
                    break;
                case ElementType.LabelComment:
                    SourceCode += " = \"" + StrDefault + "\"; // ";
                    break;
                case ElementType.LayerChooser:
                    SourceCode += " = " + StrDefault;
                    SourceCode += "; // ";
                    break;
            }

            if (EnabledWhen)
            {
                SourceCode += "{";
                if (EnableSwap)
                {
                    SourceCode += "!";
                }
                SourceCode += EnableIdentifier + "} ";
            }
            SourceCode += Name + "\r\n";

            return SourceCode;
        }

        internal string[] ToOptionArray()
        {
            if ((ElementType == ElementType.DropDown) || (ElementType == ElementType.RadioButtons))
            {
                int BarLoc = Name.IndexOf("|", StringComparison.Ordinal);
                if (BarLoc == -1) return Array.Empty<string>();
                string Options = Name.Substring(BarLoc + 1);
                return Options.Split('|');
            }
            return Array.Empty<string>();
        }

        internal string ToShortName()
        {
            if ((ElementType == ElementType.DropDown) || (ElementType == ElementType.RadioButtons) || (ElementType == ElementType.Filename))
            {
                int BarLoc = Name.IndexOf("|", StringComparison.Ordinal);
                if (BarLoc == -1) return Name;
                return Name.Substring(0, BarLoc);
            }
            return Name;
        }

        internal string ToAllowableFileTypes()
        {
            int BarLoc = Name.IndexOf("|", StringComparison.Ordinal);
            if (BarLoc == -1) return null;

            return Name.Substring(BarLoc + 1)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ft => "\"" + ft + "\"")
                .Join(", ");
        }
    }

    internal enum ElementType
    {
        [Description("Integer Slider")]
        IntSlider,
        [Description("Check Box")]
        Checkbox,
        [Description("Color Wheel")]
        ColorWheel,
        [Description("Angle Chooser")]
        AngleChooser,
        [Description("Pan Slider")]
        PanSlider,
        [Description("String")]
        Textbox,
        [Description("Double Slider")]
        DoubleSlider,
        [Description("Drop-Down List Box")]
        DropDown,
        [Description("BlendOp Types")]
        BinaryPixelOp,
        [Description("Font Names")]
        FontFamily,
        [Description("Radio Button List")]
        RadioButtons,
        [Description("Reseed Button")]
        ReseedButton,
        [Description("Multi-Line String")]
        MultiLineTextbox,
        [Description("3D Roll Control")]
        RollBall,
        [Description("Filename Control")]
        Filename,
        [Description("Web Link")]
        Uri,
        [Description("Folder Control")]
        Folder,
        [Description("Label")]
        LabelComment,
        [Description("Layer Names")]
        LayerChooser
    }

    internal enum SliderStyle
    {
        Default,
        Hue,
        HueCentered,
        Saturation,
        WhiteBlack,
        BlackWhite,
        CyanRed,
        MagentaGreen,
        YellowBlue,
        CyanOrange,
        WhiteRed,
        WhiteGreen,
        WhiteBlue,
    }

    [Flags]
    internal enum ColorWheelOptions
    {
        None,
        Alpha,
        NoReset
    }
}
