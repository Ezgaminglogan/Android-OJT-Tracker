using Android.Content;
using Android.Views;
using Android.Widget;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "Login", Theme = "@style/AppTheme")]
    public class LoginActivity : Activity
    {
        private EditText? emailInput;
        private EditText? passwordInput;
        private CheckBox? rememberMeCheckbox;
        private Button? loginButton;
        private TextView? signUpText;
        private TextView? errorMessage;
        private ProgressBar? loginProgress;
        private Dialog? loadingDialog;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_login);

            // Initialize views
            InitializeViews();

            // Set up event handlers
            SetupEventHandlers();
        }

        private void InitializeViews()
        {
            emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            rememberMeCheckbox = FindViewById<CheckBox>(Resource.Id.rememberMeCheckbox);
            loginButton = FindViewById<Button>(Resource.Id.loginButton);
            signUpText = FindViewById<TextView>(Resource.Id.signUpText);
            errorMessage = FindViewById<TextView>(Resource.Id.errorMessage);
            loginProgress = FindViewById<ProgressBar>(Resource.Id.loginProgress);
        }

        private void SetupEventHandlers()
        {
            if (loginButton != null)
            {
                loginButton.Click += LoginButton_Click;
            }

            if (signUpText != null)
            {
                signUpText.Click += SignUpText_Click;
            }
        }

        private async void LoginButton_Click(object? sender, EventArgs e)
        {
            // Hide error message
            if (errorMessage != null)
            {
                errorMessage.Visibility = ViewStates.Gone;
            }

            // Get input values
            string email = emailInput?.Text?.Trim() ?? string.Empty;
            string password = passwordInput?.Text ?? string.Empty;

            // Validate inputs
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email or student ID");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password");
                return;
            }

            // Show loading dialog
            ShowLoadingDialog();

            // Simulate network delay
            await Task.Delay(1500);

            // Validate credentials against database
            bool isAuthenticated = ValidateCredentials(email, password);

            // Hide loading dialog
            HideLoadingDialog();

            if (isAuthenticated)
            {
                // Always save login state for persistent session
                SaveLoginState(email);

                // Navigate to main activity
                NavigateToMainActivity();
            }
            else
            {
                ShowError("Invalid email or password. Please try again.");
            }
        }

        private bool ValidateCredentials(string email, string password)
        {
            // Use SQLite database for authentication
            var dbHelper = new Database.DatabaseHelper(this);
            bool isValid = dbHelper.ValidateUser(email, password);
            dbHelper.Close();
            
            return isValid;
        }

        private void SaveLoginState(string identifier)
        {
            // Get user info from database
            var dbHelper = new Database.DatabaseHelper(this);
            int userId = dbHelper.GetUserId(identifier);
            
            // Get the actual email (in case they logged in with Student ID)
            string actualEmail = identifier;
            var db = dbHelper.ReadableDatabase;
            if (db != null)
            {
                var cursor = db.RawQuery(
                    $"SELECT {Database.DatabaseHelper.ColEmail} FROM {Database.DatabaseHelper.TableUsers} WHERE {Database.DatabaseHelper.ColUserId} = ?",
                    new[] { userId.ToString() }
                );
                if (cursor != null && cursor.MoveToFirst())
                {
                    actualEmail = cursor.GetString(0) ?? identifier;
                }
                cursor?.Close();
            }
            dbHelper.Close();

            // Save to SharedPreferences
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            var editor = prefs?.Edit();
            editor?.PutString("user_email", actualEmail);
            editor?.PutInt("user_id", userId);
            editor?.PutBoolean("is_logged_in", true);
            editor?.Apply();
        }

        private void ShowLoadingDialog()
        {
            if (loadingDialog != null && loadingDialog.IsShowing)
                return;

            loadingDialog = new Dialog(this);
            loadingDialog.RequestWindowFeature((int)WindowFeatures.NoTitle);
            loadingDialog.SetContentView(Resource.Layout.dialog_loading);
            loadingDialog.Window?.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
            loadingDialog.SetCancelable(false);
            loadingDialog.Show();

            // Update loading text
            var loadingText = loadingDialog.FindViewById<TextView>(Resource.Id.loadingText);
            if (loadingText != null)
            {
                loadingText.Text = "Signing you in...";
            }
        }

        private void HideLoadingDialog()
        {
            loadingDialog?.Dismiss();
            loadingDialog = null;
        }

        private void ShowLoading(bool show)
        {
            if (loginProgress != null)
            {
                loginProgress.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
            }

            if (loginButton != null)
            {
                loginButton.Enabled = !show;
                loginButton.Alpha = show ? 0.5f : 1.0f;
            }
        }

        private void ShowError(string message)
        {
            if (errorMessage != null)
            {
                errorMessage.Text = message;
                errorMessage.Visibility = ViewStates.Visible;
            }

            Toast.MakeText(this, message, ToastLength.Short)?.Show();
        }

        private void NavigateToMainActivity()
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
            Finish(); // Close login activity
        }

        private void ForgotPasswordText_Click(object? sender, EventArgs e)
        {
            // TODO: Implement forgot password functionality
            Toast.MakeText(this, "Forgot password feature coming soon!", ToastLength.Short)?.Show();
        }

        private void SignUpText_Click(object? sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(Activities.SignUpActivity));
            StartActivity(intent);
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Check if already logged in (persistent session)
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            bool isLoggedIn = prefs?.GetBoolean("is_logged_in", false) ?? false;

            if (isLoggedIn)
            {
                // User has an active session, navigate to main activity
                NavigateToMainActivity();
            }
        }
    }
}
