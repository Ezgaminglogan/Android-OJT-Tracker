using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Google.Android.Material.FloatingActionButton;
using OJT_InternTrack.Database;
using OJT_InternTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "My Tasks", Theme = "@style/Theme.AppCompat.Light.NoActionBar")]
    public class TasksActivity : Activity
    {
        private LinearLayout? tasksContainer;
        private TextView? pendingTasksCount;
        private TextView? completedTasksCount;
        private FloatingActionButton? addTaskFab;
        private SwipeRefreshLayout? swipeRefreshLayout;
        private ImageButton? backButton;
        private DatabaseHelper? dbHelper;
        private int userId;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_tasks);

            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            userId = prefs?.GetInt("user_id", -1) ?? -1;

            if (userId == -1)
            {
                Finish();
                return;
            }

            dbHelper = new DatabaseHelper(this);
            InitializeViews();
            LoadTasks();
        }

        private void InitializeViews()
        {
            tasksContainer = FindViewById<LinearLayout>(Resource.Id.tasksContainer);
            pendingTasksCount = FindViewById<TextView>(Resource.Id.pendingTasksCount);
            completedTasksCount = FindViewById<TextView>(Resource.Id.completedTasksCount);
            addTaskFab = FindViewById<FloatingActionButton>(Resource.Id.addTaskFab);
            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);

            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            if (addTaskFab != null)
            {
                addTaskFab.Click += (s, e) => ShowAddTaskDialog();
            }

            if (swipeRefreshLayout != null)
            {
                swipeRefreshLayout.SetColorSchemeColors(Android.Graphics.Color.ParseColor("#10B981"));
                swipeRefreshLayout.Refresh += (s, e) => {
                    LoadTasks();
                    swipeRefreshLayout.Refreshing = false;
                };
            }
        }

        private void LoadTasks()
        {
            if (dbHelper == null || tasksContainer == null) return;

            var tasks = dbHelper.GetTasks(userId);
            tasksContainer.RemoveAllViews();

            int pendingCount = tasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
            int completedCount = tasks.Count(t => t.Status == "Completed");

            if (pendingTasksCount != null) pendingTasksCount.Text = pendingCount.ToString();
            if (completedTasksCount != null) completedTasksCount.Text = completedCount.ToString();

            if (tasks.Count == 0)
            {
                var empty = new TextView(this)
                {
                    Text = "No tasks yet. Tap + to add one!",
                    Gravity = Android.Views.GravityFlags.Center,
                    LayoutParameters = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, 300)
                };
                tasksContainer.AddView(empty);
                return;
            }

            foreach (var task in tasks)
            {
                var view = CreateTaskItemView(task);
                tasksContainer.AddView(view);
            }
        }

        private Android.Views.View CreateTaskItemView(InternTask task)
        {
            var card = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.WrapContent)
                {
                    BottomMargin = 16
                }
            };
            card.SetPadding(20, 20, 20, 20);
            card.SetBackgroundResource(Resource.Drawable.quick_action_card);
            card.SetGravity(Android.Views.GravityFlags.CenterVertical);
            card.Elevation = 2;

            // Status Checkbox (Emoji based)
            var statusIcon = new TextView(this)
            {
                Text = task.Status == "Completed" ? "✅" : "⭕",
                TextSize = 24,
                LayoutParameters = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.WrapContent, Android.Views.ViewGroup.LayoutParams.WrapContent)
                {
                    RightMargin = 16
                }
            };

            // Info
            var infoLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(0, Android.Views.ViewGroup.LayoutParams.WrapContent, 1)
            };

            var titleText = new TextView(this)
            {
                Text = task.Title,
                TextSize = 16,
                Typeface = Android.Graphics.Typeface.DefaultBold
            };
            titleText.SetTextColor(task.Status == "Completed" ? Android.Graphics.Color.Gray : Android.Graphics.Color.ParseColor("#1A202C"));
            if (task.Status == "Completed")
            {
                titleText.PaintFlags |= Android.Graphics.PaintFlags.StrikeThruText;
            }

            var subInfo = new TextView(this)
            {
                Text = task.GetFormattedDueDate(),
                TextSize = 12
            };
            subInfo.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));

            infoLayout.AddView(titleText);
            infoLayout.AddView(subInfo);

            card.AddView(statusIcon);
            card.AddView(infoLayout);

            card.Click += (s, e) => ShowTaskOptions(task);
            statusIcon.Click += (s, e) => ToggleTaskStatus(task);

            return card;
        }

        private void ToggleTaskStatus(InternTask task)
        {
            string newStatus = task.Status == "Completed" ? "Pending" : "Completed";
            if (dbHelper != null && dbHelper.UpdateTaskStatus(task.Id, newStatus))
            {
                LoadTasks();
            }
        }

        private void ShowTaskOptions(InternTask task)
        {
            var options = new string[] { "Change Status", "Delete Task" };
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(task.Title);
            builder.SetItems(options, (s, e) =>
            {
                if (e.Which == 0) ShowStatusPicker(task);
                else if (e.Which == 1) DeleteTask(task);
            });
            builder.Show();
        }

        private void ShowStatusPicker(InternTask task)
        {
            var statuses = new string[] { "Pending", "In Progress", "Completed" };
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Update Status");
            builder.SetItems(statuses, (s, e) =>
            {
                if (dbHelper != null && dbHelper.UpdateTaskStatus(task.Id, statuses[e.Which]))
                {
                    LoadTasks();
                }
            });
            builder.Show();
        }

        private void DeleteTask(InternTask task)
        {
            if (dbHelper != null && dbHelper.DeleteTask(task.Id))
            {
                LoadTasks();
            }
        }

        private void ShowAddTaskDialog()
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("New Task");

            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            layout.SetPadding(40, 20, 40, 0);

            var inputTitle = new EditText(this) { Hint = "Task Title" };
            var inputDesc = new EditText(this) { Hint = "Description (optional)" };
            
            layout.AddView(inputTitle);
            layout.AddView(inputDesc);
            builder.SetView(layout);

            builder.SetPositiveButton("Add", (s, e) =>
            {
                string title = inputTitle.Text;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    var task = new InternTask
                    {
                        UserId = userId,
                        Title = title,
                        Description = inputDesc.Text,
                        Status = "Pending"
                    };
                    dbHelper?.SaveTask(userId, task);
                    LoadTasks();
                }
            });
            builder.SetNegativeButton("Cancel", (s, e) => { });
            builder.Show();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dbHelper?.Close();
        }
    }
}
