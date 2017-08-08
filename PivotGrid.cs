using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ENV;
using ENV.Labs;
using ENV.Utilities;
using Firefly.Box;
using ENV.Data;
using Firefly.Box.Data.Advanced;

namespace DevExpressIntegration
{
    public class PivotGrid : UIControllerBase
    {
        public static bool IsNotINMdi(System.Windows.Forms.Form f)
        {
            if (f.MdiParent != null)
                return false;
            var x = f.Parent as System.Windows.Forms.Form;
            if (x != null)
                return IsNotINMdi(x);
            return true;
        }
        public static void Enable()
        {
            ENV.UI.Grid.AddToGridContext +=
                action =>
                {
                    DevExpressIntegration.PivotGrid pg = null;
                    FilterCollection fc = null;
                    bool hide = false;
                    var t = Firefly.Box.Context.Current.ActiveTasks;
                    var uic = t[t.Count - 1] as UIController;
                    if (uic != null)
                    {
                        action("Pivot - By Dev Express",
                            () =>
                            {
                                t = Firefly.Box.Context.Current.ActiveTasks;
                                if (t[t.Count - 1] == uic)
                                    uic.SaveRowAndDo(
                                        o =>
                                        {
                                            hide = false;
                                            if (pg == null)
                                            {
                                                fc = new FilterCollection();
                                                uic.Where.Add(fc);
                                                var dt = new DataTableBuilder()
                                                         {
                                                             SplitDateToYearMonthDay = true,
                                                             SplitHourOutOfTime = true
                                                         };
                                                GridExports.ExportToDataTableBuilder(uic,
                                                    () =>
                                                    {
                                                        pg = new DevExpressIntegration.PivotGrid();
                                                        if (IsNotINMdi(uic.View))
                                                            pg.DisableFitToMDI();
                                                        pg.KeepViewVisible(false);

                                                        pg.UserSelectedValue +=
                                                            apply =>
                                                            {
                                                                fc.Clear();
                                                                dt.ApplyFilter(apply, fc);
                                                                o.ReloadData();
                                                        //        o.GoToFirstRow();
                                                                pg.KeepViewVisible(true);

                                                                pg.Hide();
                                                                hide = true;
                                                            };
                                                        pg.Run(dt.Result);

                                                    }, dt);
                                            }
                                            else
                                            {
                                                pg.KeepViewVisible(false);
                                                pg.ShowAfterHide();
                                            }
                                            if (!hide)
                                            {
                                                pg.Dispose();
                                                pg = null;
                                                fc.Clear();
                                      //          o.GoToPreviouslyParkedRow();
                                                o.ReloadData();
                                            }
                                        });
                            });
                    }
                };
        }

        private void DisableFitToMDI()
        {
            _form.FitToMDI = false;
        }

        // TODO: Add members Here

        UI.PivotGridUI _form;

        public void KeepViewVisible(bool val)
        {
            KeepViewVisibleAfterExit = val;
        }

        public PivotGrid()
            : this(true)
        {
        }

        public PivotGrid(bool showChart)
        {
            //TODO: Add task definitions here

            Handlers.Add(Command.WindowResize).Invokes += e => e.Handled = true;
            View = () => _form = new UI.PivotGridUI(this, showChart); 
        }
        public string SavedLayoutFile { get { return _form.SavedLayoutFile; } set { _form.SavedLayoutFile = value; } }
        public void Run(System.Data.DataTable dt)
        {
            _form.SetDataTable(dt);
            Execute();
        }

        public DataColumn RowColumn { get; set; }

        public DataColumn ColumnColumn { get; set; }
        public DataColumn DataColumn { get; set; }


        public void CellSelected(Action<Action<DataColumn, object[]>> setFilter)
        {
            if (UserSelectedValue != null)
                UserSelectedValue(setFilter);
        }

        public event Action<Action<Action<DataColumn, object[]>>> UserSelectedValue;

        internal Dictionary<string, Func<object, object, int>> _sorters = new Dictionary<string, Func<object, object, int>>();
        public void AddCustomSort(string pipeline, Func<object, object, int> compare)
        {
            _sorters.Add(pipeline, compare);
        }

        public void SetAsCount(DataColumn dataColumn)
        {
            _form.SetAsCount(DataColumn);
        }

        public void Hide()
        {
            Exit();
            Firefly.Box.Context.Current.InvokeUICommand(_form.Hide);
        }

        public void ShowAfterHide()
        {
            //_form.Visible = false;
            Execute();
        }
        protected override void OnLoad()
        {
            Firefly.Box.Context.Current.InvokeUICommand(() =>
            {
                _form.TopMost = true;
                if (_form.IsHandleCreated)
                {
                    _form.Enabled = true;
                    _form.Visible = true;
                }
                _form.DisableMDIChildZOrdering = false;
            });
        }

        public void Dispose()
        {
            Firefly.Box.Context.Current.InvokeUICommand(() =>
            {
                _form.Close();
                _form.Dispose();
            });
        }
    }
}
