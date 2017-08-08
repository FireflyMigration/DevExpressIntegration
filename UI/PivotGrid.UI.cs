using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraPivotGrid;
using Firefly.Box;
using DevExpress.XtraCharts;

namespace DevExpressIntegration.UI
{
    partial class PivotGridUI : Firefly.Box.UI.Form
    {
        PivotGrid _task;
        public PivotGridUI(PivotGrid task, bool showChart)
        {
            _task = task;
            InitializeComponent();
            var save = contextMenuStrip1.Items.Add("Save Report");
            save.Click += delegate
                          {
                              var x = new SaveFileDialog();
                              MyDialog(x,f=>pivotGridControl1.SaveLayoutToXml(f));
                          };
            var load = contextMenuStrip1.Items.Add("Load Report");
            load.Click += delegate
                          {
                              var x = new OpenFileDialog();
                              MyDialog(x, y => pivotGridControl1.RestoreLayoutFromXml(y));
                          };


            this.splitContainerControl1.Panel2.Visible = showChart;
            if (!showChart)
                this.splitContainerControl1.Panel1.Dock = DockStyle.Fill;

            foreach (var item in System.Enum.GetValues(typeof(DevExpress.XtraCharts.ViewType)))
            {
                comboBox1.Items.Add(item);
            }
            comboBox1.SelectedItem = ViewType.Bar;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedValueChanged += (s, e) =>
            {
                if (comboBox1.SelectedItem is ViewType)
                {
                    try
                    {
                        chartControl1.SeriesTemplate.ChangeView((ViewType)comboBox1.SelectedItem);
                    }
                    catch { }
                }

            };




        }

        void MyDialog(FileDialog x,Action<string> what)
        {
            x.DefaultExt = "PivotRep";
            x.Filter = "Pivot Report|*.pivotRep";
            x.AddExtension = true;
            x.RestoreDirectory = true;
            if (x.ShowDialog(this.Owner) == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    what(x.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(SavedLayoutFile))
                pivotGridControl1.SaveLayoutToXml(SavedLayoutFile);
        }

        DataSet GetDs()
        {
            var ds = new DataSet();
            var dt = new DataTable("dt");
            ds.Tables.Add(dt);
            dt.Columns.Add(new DataColumn { ColumnName = "a", Caption = "נועם", DataType = typeof(int) });
            dt.Columns.Add(new DataColumn { ColumnName = "b", Caption = "ספי", DataType = typeof(int) });
            dt.Columns.Add(new DataColumn { ColumnName = "c", Caption = "יוני", DataType = typeof(string) });

            dt.Rows.Add(1, 1, "noam");
            dt.Rows.Add(2, 1, "yael");
            dt.Rows.Add(3, 2, "maayan");
            return ds;
        }

        public String SavedLayoutFile { get; set; }
        public void SetDataTable(DataTable dt)
        {
            Firefly.Box.Context.Current.InvokeUICommand(() =>
            {

                var ds = new DataSet();
                ds.Tables.Add(dt);




                var l = new List<DevExpress.XtraPivotGrid.PivotGridField>();
                Dictionary<PivotGridField, DataColumn> map = new Dictionary<PivotGridField, DataColumn>();
                foreach (DataColumn column in dt.Columns)
                {
                    var fielda = new DevExpress.XtraPivotGrid.PivotGridField();
                    map.Add(fielda, column);
                    l.Add(fielda);

                    if (_task._sorters.ContainsKey(column.ColumnName))
                    {
                        fielda.SortMode = PivotSortMode.Custom;
                        var n = column.ColumnName;
                        var f = _task._sorters[n];
                        pivotGridControl1.CustomFieldSort +=
                        (sender, args) =>
                        {

                            if (args.Field.Name == "XX_" + n)
                            {
                                args.Result = f(args.Value1, args.Value2);
                                args.Handled = true;
                            }
                        };
                    }


                    fielda.AreaIndex = 0;
                    fielda.Caption = column.Caption;
                    fielda.Name = "XX_" + column.ColumnName;
                    fielda.FieldName = column.ColumnName;
                    if (column == _task.ColumnColumn)
                        fielda.Area = DevExpress.XtraPivotGrid.PivotArea.ColumnArea;
                    else if (column == _task.RowColumn)
                        fielda.Area = DevExpress.XtraPivotGrid.PivotArea.RowArea;
                    else if (column == _task.DataColumn)
                        fielda.Area = DevExpress.XtraPivotGrid.PivotArea.DataArea;
                    if (column.DataType == typeof(decimal) && !_countColumns.Contains(column))
                        fielda.SummaryType = DevExpress.Data.PivotGrid.PivotSummaryType.Sum;
                    else
                        fielda.SummaryType = DevExpress.Data.PivotGrid.PivotSummaryType.Count;


                    //    fielda.Name = column.ColumnName;
                }
                l.Reverse();
                pivotGridControl1.Fields.AddRange(l.ToArray());

                this.dataTable1BindingSource.DataMember = "dt";
                this.dataTable1BindingSource.DataSource = ds;
                chartControl1.DataSource = pivotGridControl1;
                chartControl1.SeriesDataMember = "Series";
                chartControl1.SeriesTemplate.ArgumentDataMember = "Arguments";
                chartControl1.SeriesTemplate.ValueDataMembers.AddRange(new string[] { "Values" });

                pivotGridControl1.CellDoubleClick += (sender, args) =>
                                                         {
                                                             RunInLogicContext(() =>
                                                             {
                                                                 _task.CellSelected(
                                                                     y =>
                                                                     {
                                                                         foreach (var f in args.GetRowFields())
                                                                         {
                                                                             y(map[f], new[] { args.GetFieldValue(f) });
                                                                         }
                                                                         foreach (var f in args.GetColumnFields())
                                                                         {
                                                                             y(map[f], new[] { args.GetFieldValue(f) });
                                                                         }
                                                                         foreach (PivotGridField f in pivotGridControl1.Fields)
                                                                         {
                                                                             if (f.FilterValues.HasFilter)
                                                                             {
                                                                                 y(map[f], f.FilterValues.ValuesIncluded);
                                                                             }
                                                                         }
                                                                     });
                                                             });

                                                         };

                if (File.Exists(SavedLayoutFile))
                {
                    pivotGridControl1.RestoreLayoutFromXml(SavedLayoutFile);
                    foreach (PivotGridField f in pivotGridControl1.Fields)
                    {
                        if (_task._sorters.ContainsKey(f.Name.Substring(3)))
                            f.SortMode = PivotSortMode.Custom;
                    }
                }
            });
        }



        private void exportToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var x = ENV.Labs.GridExports.GetAvailableFileName("Pivot", ".xls");
            pivotGridControl1.ExportToXls(x);
            ENV.Windows.OSCommand("\"" + x + "\"");
        }

        private void editorToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void pieToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        HashSet<DataColumn> _countColumns = new HashSet<DataColumn>();

        internal void SetAsCount(DataColumn DataColumn)
        {
            _countColumns.Add(DataColumn);
        }
    }
}