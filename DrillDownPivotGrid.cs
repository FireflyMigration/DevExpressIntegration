using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ENV.Data;
using ENV.UI;
using ENV.Utilities;
using Firefly.Box;
using Firefly.Box.Data.Advanced;

namespace DevExpressIntegration
{
    public class DrillDownPivotGrid<EntityType> where EntityType : Entity
    {
        PivotGrid _pivot = new PivotGrid();
        public DataColumn ColumnColumn
        {
            get { return _pivot.ColumnColumn; }
            set { _pivot.ColumnColumn = value; }
        }

        public DataColumn DataColumn
        {
            get { return _pivot.DataColumn; }
            set { _pivot.DataColumn = value; }
        }

        public DataColumn RowColumn
        {
            get { return _pivot.RowColumn; }
            set { _pivot.RowColumn = value; }
        }

        public void SetAsCount(DataColumn dataColumn)
        {
            _pivot.SetAsCount(dataColumn);
        }

        DataTableBuilder _builder = new DataTableBuilder();
        EntityType _instance;

        public DrillDownPivotGrid()
            : this(System.Activator.CreateInstance<EntityType>())
        {
        }

        public DrillDownPivotGrid(EntityType instance)
        {
            _instance = instance;
            _pivot.UserSelectedValue += useFilter =>
                                        {
                                            if (_onUserSelect != null)
                                                _onUserSelect((type, collection) =>
                                                                 {
                                                                     useFilter((column, objects) =>
                                                                                       {
                                                                                           var y = filters[column];
                                                                                           FilterBase f = null;
                                                                                           foreach (var value in objects)
                                                                                           {
                                                                                               var myF = y(type, filterValues[column][value]);
                                                                                               if (f == null)
                                                                                                   f = myF;
                                                                                               else f = f.Or(myF);
                                                                                           }
                                                                                           if (f != null)
                                                                                               collection.Add(f);
                                                                                       });
                                                                 });
                                        };
        }

        public delegate void ApplyFilter(EntityType entity, FilterCollection where);
        public delegate void UserSelectedDataDelegate(ApplyFilter apply);
        UserSelectedDataDelegate _onUserSelect;

        DataTable Result
        {
            get
            {
                return _builder.Result;
            }
        }

        

        Dictionary<DataColumn, Dictionary<object, object>> filterValues = new Dictionary<DataColumn, Dictionary<object, object>>();
        Dictionary<DataColumn, Func<EntityType, object, FilterBase>> filters = new Dictionary<DataColumn, Func<EntityType, object, FilterBase>>();


        public DataColumn AddColumn(Func<EntityType, TextColumn> column)
        {
            return AddColumn<string>(column(_instance).Caption, (y) => column(y).Value.ToString(), y => column(y), (y, v) => column(y).IsEqualTo((Text)v), y => Text.IsNullOrEmpty(column(y).Value));
        }
        public DataColumn AddColumn(Func<EntityType, NumberColumn> column)
        {
            return AddColumn<decimal>(column(_instance).Caption, (y) => column(y).Value.ToDecimal(), y => column(y), (y, v) => column(y).IsEqualTo((Number)v), y => column(y) == null);
        }


        public DataColumn AddDateColumnYear(Func<EntityType, DateColumn> column)
        {
            return AddDateColumnPart("Year", column, y => column(y).Year);
        }
        public DataColumn AddDateColumnMonth(Func<EntityType, DateColumn> column)
        {
            return AddDateColumnPart("Month", column, y => column(y).Month);
        }
        public DataColumn AddDateColumnWeekDay(Func<EntityType, DateColumn> column)
        {
            return AddDateColumnPart("Week Day", column, y => ((int)column(y).DayOfWeek) + 1);
        }
        public DataColumn AddColumn(string caption, Func<EntityType, decimal> value)
        {
            return AddColumn<decimal>(caption, value,y=> value(y), (y,v) => {
                var fc = new FilterCollection();
                fc.Add(() => value(y) == (decimal)v);
                return fc;
            }, y => false);
        }

        DataColumn AddDateColumnPart(string name, Func<EntityType, DateColumn> column, Func<EntityType, int> getValue)
        {
            return AddColumn<int>(column(_instance).Caption + " " + name,
                getValue,
                y => getValue(y),
                (y, v) =>
                {
                    var fc = new FilterCollection();
                    if (v == null)
                        fc.Add(column(y).IsEqualTo(Date.Empty));
                    else
                    {
                        fc.Add(() => getValue(y) == (int)v);
                    }
                    return fc;
                }, y => Date.IsNullOrEmpty(column(y)));
        }

        public DataColumn AddColumn(Func<EntityType, DateColumn> column)
        {
            return AddColumn<DateTime>(column(_instance).Caption, (y) => column(y).Value.ToDateTime(), y => column(y), (y, v) => column(y).IsEqualTo((Date)v), y => Date.IsNullOrEmpty(column(y)));



        }

        public DataColumn AddColumn(string name, Func<EntityType, string> getDataTableValue,
            Func<EntityType, object, FilterBase> getFilter, Func<EntityType, bool> isEmpty)
        {
            return AddColumn<string>(name, getDataTableValue, getDataTableValue, getFilter, isEmpty);

        }
        public DataColumn AddColumn<Type>(string name, Func<EntityType, Type> getDataTableValue, Func<EntityType, object> getFitlerValue, Func<EntityType, object, FilterBase> getFilter, Func<EntityType, bool> isEmpty)
        {
            var f = new Dictionary<object, object>();
            var dc = _builder.AddColumn(name, typeof(Type), () =>
            {
                var r = getDataTableValue(_instance);
                if (!f.ContainsKey(r))
                {
                    var x = getFitlerValue(_instance);
                    var c = x as ColumnBase;
                    if (c != null)
                        x = c.Value;
                    f.Add(r, x);
                }
                return r;
            }, () => isEmpty(_instance), delegate { });
            filterValues.Add(dc, f);
            filters.Add(dc, getFilter);
            return dc;
        }

        public void AddRow()
        {
            _builder.AddRow();
        }

        public void Show(UserSelectedDataDelegate onUserSelect)
        {
            _onUserSelect = onUserSelect;
            _pivot.Run(Result);
        }

        
        public void Run(UserSelectedDataDelegate onUserSelect, FilterBase where=null)
        {
            _onUserSelect = onUserSelect;
            var bp = new BusinessProcess { From = _instance };
            if (where != null)
                bp.Where.Add(where);

            ShowProgressInNewThread.ReadAllRowsWithProgress(bp, "Loading", AddRow);
            _pivot.Run(Result);
        }
    }
}
