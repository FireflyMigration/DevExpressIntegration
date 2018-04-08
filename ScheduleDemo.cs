using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Firefly.Box;
using ENV;
using ENV.Data;
using DevExpress.XtraScheduler;
using Firefly.Box.Data.Advanced;


namespace DevExpressIntegration
{
    public class ScheduleDemo : UIControllerBase
    {
        Views.RoomScheduleView _form;

        public ScheduleDemo()
        {
            Handlers.Add(Command.Exit, HandlerScope.CurrentTaskOnly).Invokes += e =>
            {

                e.Handled = !ENV.Common.ShowYesNoMessageBox("Close", "Exit?", false);
            };
            Text = "Scheduler";
            StartDate = Date.Now;
        }
        public string SavedLayoutFile { get; set; }
        public void Run()
        {
            Execute();
        }

        protected override void OnStart()
        {
            //Cached<Data>().Run();
        }

        protected override void OnLoad()
        {
            _form = new Views.RoomScheduleView(this);
            View = () => _form;
        }

        List<Appointment> _appointments = new List<Appointment>();
        List<Resource> _resources = new List<Resource>();
        HashSet<object> _foundResources = new HashSet<object>();

        public string Text { get; set; }
        public Date StartDate { get; set; }
        public bool ShowAsGantt { get; set; }

        public MyAppointment AddAppointment(Date startDate, Time startTime, int duration, string subject, string description="", object resourceId=null, string resourceCaption=null)
        {
            return AddAppointment(startDate, startTime, startDate, startTime.AddHours(duration),subject, description, resourceId, resourceCaption);
        }
        public MyAppointment AddAppointment(Date startDate,Time startTime,Date endDate,Time endTime,string subject,string description="",object resourceId=null,string resourceCaption=null)
        {
            {
                var col = resourceId as ColumnBase;
                if (col != null)
                    resourceId = col.Value;
            }
            
            var my = new DevExpressIntegration.MyAppointment();
            var app = _form.schedulerControl1.Storage.CreateAppointment(AppointmentType.Normal,
                        startTime.AddToDateTime(startDate.ToDateTime()),
                        endTime.AddToDateTime(endDate.ToDateTime())- startTime.AddToDateTime(startDate.ToDateTime()),
                        subject.TrimEnd()); //todo: send my
            my.SetApp(app);
            app.Description = description.TrimEnd();
            _appointments.Add(app);
            if (resourceId != null)
            {
                app.ResourceId = resourceId;


                
                if (!_foundResources.Contains(resourceId))
                {
                    if (resourceCaption == null)
                        resourceCaption = resourceId.ToString().Trim();
                    _resources.Add(_form.schedulerControl1.Storage.CreateResource(resourceId, resourceCaption.TrimEnd()));
                    _foundResources.Add(resourceId);
                }
            }
            return my;
        }
        
     
        

        internal void ShowInfo(Appointment appointment)
        {
            Action a = appointment.Id as Action;
            if (a != null)
                a();



        }

        internal void AddTo(SchedulerStorage s)
        {
            
            {
                _resources.Sort((a, b) => a.Caption.CompareTo(b.Caption));
                s.Resources.AddRange(_resources.ToArray());
                s.Appointments.AddRange(_appointments.ToArray());
            }


        }

    }

    

    public class MyAppointment
    {
        public event Action ShowAppointmentDetail;
        public Color BackColor;

        public int CompletePrecent
        {
            get { return _app.PercentComplete; }
            set { _app.PercentComplete = value; }
        }

        internal void DoClick()
        {
            if (ShowAppointmentDetail != null)
                ShowAppointmentDetail();
            
        }
        Appointment _app;
        internal void SetApp(Appointment app)
        {

            _app = app;
            
        }
        public event Action<AppTimeInformation> Changed;
        public void SetStart(Date startDate, Time time)
        {
            _app.Start = time.AddToDateTime(startDate);
        }

        public void SetEnd(Date endDate, Time time)
        {
            _app.End = time.AddToDateTime(endDate);
        }


        internal void Resized(AppointmentResizeEventArgs e)
        {
            if (Changed != null)
            {
                var app = new AppTimeInformation(e.EditedAppointment);
                app.Allow = e.Allow;
                Changed(app);
                e.Allow = app.Allow;
            }
        }

        internal void Dropped(AppointmentDragEventArgs e)
        {
            if (Changed != null)
            {
                var app = new AppTimeInformation(e.EditedAppointment);
                app.Allow = e.Allow;
                Changed(app);
                e.Allow = app.Allow;
            }
        }
        Dictionary<string, object> _values = new Dictionary<string, object>();
        public void Set(params ColumnBase[] cols)
        {
            foreach (var item in cols)
            {
                _values.Add(item.Name, item.Value);
            }
        }

        public T Get<T>(TypedColumnBase<T> column)
        {
            return (T)_values[column.Name];
        }
    }
    public class AppTimeInformation
    {
        Appointment _app;
        public bool Allow { get; set; }
        internal AppTimeInformation(Appointment app)
        {
            _app = app;
        }
        public Date StartDate { get { return _app.Start; } }
        public Date EndDate { get { return _app.End; } }
        public Time StartTime { get { return Time.FromDateTime(_app.Start); } }
        public Time EndTime { get { return Time.FromDateTime(_app.End); } }
        public DateTime Start { get { return _app.Start; } }
        public DateTime End { get { return _app.End; } }
    }

    public class ColorById
    {
        Dictionary<object, Color> _knownIds = new Dictionary<object, Color>();
        public static Color[] colors = new Color[] {
            Color.PaleVioletRed,
             Color.LightGreen,
            Color.LightBlue,
            Color.OrangeRed,
            Color.LightCoral,
            Color.LightPink };
        public Color GetFor(object id)
        {
            Color result;
            if (_knownIds.TryGetValue(id, out result))
                return result;
            else
            {
                result = colors[_knownIds.Count % colors.Length];
                _knownIds.Add(id, result);
                return result;
            }
            
            
        }
    }
    


}