using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Firefly.Box;
using Firefly.Box.UI.Advanced;
using ENV;
using ENV.Data;
using DevExpress.XtraScheduler;

namespace DevExpressIntegration.Views
{
    partial class RoomScheduleView : ENV.UI.Form
    {
        ScheduleDemo _controller;
        public RoomScheduleView(ScheduleDemo controller)
        {
            _controller = controller;
            InitializeComponent();
            schedulerControl1.DayView.TopRowTime = new TimeSpan(8, 0, 0);
            schedulerControl1.WorkWeekView.TopRowTime = new TimeSpan(8, 0, 0);
            if (_controller.ShowAsGantt)
                schedulerControl1.ActiveViewType = SchedulerViewType.Gantt;
            else
                schedulerControl1.ActiveViewType = SchedulerViewType.WorkWeek;
            schedulerControl1.WorkDays.Clear();
            schedulerControl1.WorkDays.Add(WeekDays.EveryDay);
            schedulerControl1.WorkDays.AddHoliday(DateTime.Now.AddDays(1), "HAHA");

            schedulerControl1.OptionsView.FirstDayOfWeek = DevExpress.XtraScheduler.FirstDayOfWeek.Sunday;

            //ribbonControl1.Minimized = true;
            ribbonControl1.ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Hidden;
            schedulerControl1.AppointmentViewInfoCustomizing += SchedulerControl1_AppointmentViewInfoCustomizing;
            schedulerStorage1.Appointments.CustomFieldMappings.Add(new DevExpress.XtraScheduler.AppointmentCustomFieldMapping("AptColor", "AptColor"));
            Text = _controller.Text;
            schedulerControl1.Start = _controller.StartDate.ToDateTime();
            RightToLeft = RightToLeft.No;
            schedulerControl1.AppointmentResized += SchedulerControl1_AppointmentResized;
            schedulerControl1.AppointmentDrop += SchedulerControl1_AppointmentDrop;
            schedulerControl1.WeekView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.DayView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.WorkWeekView.ShowFullWeek = true;
            schedulerControl1.MonthView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.TimelineView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.GanttView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.GanttView.AppointmentDisplayOptions.EndTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.GanttView.ResourcesPerPage = 5;




        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(_controller. SavedLayoutFile))
                schedulerControl1.SaveLayoutToXml(_controller.SavedLayoutFile);
        }
        private void SchedulerControl1_AppointmentDrop(object sender, AppointmentDragEventArgs e)
        {
            var app = e.EditedAppointment.Id as MyAppointment;
            if (app != null)
                app.Dropped(e);
        }

        private void SchedulerControl1_AppointmentResized(object sender, AppointmentResizeEventArgs e)
        {
            var app = e.EditedAppointment.Id as MyAppointment;
            if (app != null)
                app.Resized(e);
        }

        private void SchedulerControl1_AppointmentViewInfoCustomizing(object sender, AppointmentViewInfoCustomizingEventArgs e)
        {
            var c = e.ViewInfo.Appointment.Id as MyAppointment;
            if (c != null && c.BackColor != Color.Empty)
                e.ViewInfo.Appearance.BackColor = c.BackColor;
        }

        bool _done = false;
        private object pivotGridControl1;

        

        protected override void OnControllerEnterRow()
        {
            if (_done)
                return;
            _done = true;

            schedulerStorage1.BeginUpdate();
            _controller.AddTo(schedulerStorage1);

            schedulerStorage1.EndUpdate();

            var cms = new ContextMenuStrip();
            resourceCheckListBoxControl1.ContextMenuStrip = cms;
            cms.Items.Add("Check All").Click += delegate { resourceCheckListBoxControl1.CheckAll(); };
            cms.Items.Add("Uncheck All").Click += delegate { resourceCheckListBoxControl1.UnCheckAll(); };
            if (System.IO.File.Exists(_controller.SavedLayoutFile))
            {
                schedulerControl1.RestoreLayoutFromXml(_controller.SavedLayoutFile);
                
            }

        }

        private void schedulerControl1_EditAppointmentFormShowing(object sender, AppointmentFormEventArgs e)
        {
            e.Handled = true;
            var c = e.Appointment.Id as MyAppointment;
            if (c != null)
                c.DoClick();




        }
    }

}