﻿using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Widget;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BatteryScreamer
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        const string TAG = "BATTERY_SCREAMER";
        const int JOB_ID = 41383;

        TextView Info, Status;
        Button Start, Stop;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.main);

            Info = FindViewById<TextView>(Resource.Id.info);
            Status = FindViewById<TextView>(Resource.Id.status);
            Start = FindViewById<Button>(Resource.Id.scheduleJob);
            Stop = FindViewById<Button>(Resource.Id.cancelJob);

            Init();
        }

        void Init()
        {
            Start.Click += delegate { ScheduleJob(); };
            Stop.Click += delegate { CancelJob(); };

            UpdateInfo();
            Battery.BatteryInfoChanged += delegate { UpdateInfo(); };

            UpdateJobStatus();
            Task.Run(() =>
            {
                do
                {
                    UpdateJobStatus();
                    Task.Delay(30000).Wait();
                } while (true);
            });
        }

        void UpdateInfo() => Info.Text = $"Level: {Battery.ChargeLevel * 100}%\nSource: {Battery.PowerSource.ToString()}\nStatus: {Battery.State.ToString()}";

        void UpdateJobStatus(bool? isActive = null)
        {
            if (!isActive.HasValue)
                isActive = ((JobScheduler)GetSystemService(JobSchedulerService)).AllPendingJobs.Any(j => j.Id == JOB_ID);

            var ColorResource = isActive.Value ? Resource.Color.colorHappy : Resource.Color.colorDanger;
            var Color = Build.VERSION.SdkInt >= BuildVersionCodes.M ? Resources.GetColor(ColorResource, null) : Resources.GetColor(ColorResource);

            Status.Text = $"Alerts are " + (isActive.Value ? "Active" : "Inactive");
            Status.SetTextColor(Color);
        }

        void ScheduleJob()
        {
            var JavaClass = Java.Lang.Class.FromType(typeof(BatteryScreamerJob));
            var Component = new ComponentName(this, JavaClass);
            var JobInfo = new JobInfo.Builder(JOB_ID, Component)
                                .SetRequiredNetworkType(NetworkType.Unmetered)
                                .SetPersisted(true)
                                .SetPeriodic(30 * 60 * 1000)
                                .Build();

            var Scheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            var IsScheduled = Scheduler.Schedule(JobInfo) == JobScheduler.ResultSuccess;
            UpdateJobStatus(IsScheduled);
        }

        void CancelJob()
        {
            var Scheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            Scheduler.Cancel(JOB_ID);
            UpdateJobStatus(false);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}