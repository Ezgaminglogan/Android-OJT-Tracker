using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "Sign Up", Theme = "@style/AppTheme")]
    public class SignUpActivity : Activity
    {
        private EditText? fullNameInput;
        private EditText? studentIdInput;
        private EditText? emailInput;
        private EditText? passwordInput;
        private EditText? confirmPasswordInput;
        private Button? signUpButton;
        private TextView? signInText;
        private TextView? errorMessage;
        private ProgressBar? signUpProgress;
        private Dialog? loadingDialog;
        private Dialog? successDialog;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_signup);

            InitializeViews();
            SetupEventHandlers();
        }

        private void InitializeViews()
        {
            fullNameInput = FindViewById<EditText>(Resource.Id.fullNameInput);
            studentIdInput = FindViewById<EditText>(Resource.Id.studentIdInput);
            emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            confirmPasswordInput = FindViewById<EditText>(Resource.Id.confirmPasswordInput);
            signUpButton = FindViewById<Button>(Resource.Id.signUpButton);
            signInText = FindViewById<TextView>(Resource.Id.signInText);
            errorMessage = FindViewById<TextView>(Resource.Id.errorMessage);
            signUpProgress = FindViewById<ProgressBar>(Resource.Id.signUpProgress);

            var backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }
        }

        private void SetupEventHandlers()
        {
            if (signUpButton != null)
            {
                signUpButton.Click += SignUpButton_Click;
            }

            if (signInText != null)
            {
                signInText.Click += (s, e) => Finish();
            }
        }

        private async void SignUpButton_Click(object? sender, EventArgs e)
        {
            // Hide error message
            if (errorMessage != null)
            {
                errorMessage.Visibility = ViewStates.Gone;
            }

            // Get input values
            string fullName = fullNameInput?.Text?.Trim() ?? string.Empty;
            string studentId = studentIdInput?.Text?.Trim() ?? string.Empty;
            string email = emailInput?.Text?.Trim() ?? string.Empty;
            string password = passwordInput?.Text ?? string.Empty;
            string confirmPassword = confirmPasswordInput?.Text ?? string.Empty;

            // Validate inputs
            if (string.IsNullOrEmpty(fullName))
            {
                ShowError("Please enter your full name");
                return;
            }

            if (string.IsNullOrEmpty(studentId))
            {
                ShowError("Please enter your student ID");
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email");
                return;
            }

            var matcher = Android.Util.Patterns.EmailAddress?.Matcher(email);
            if (matcher == null || !matcher.Matches())
            {
                ShowError("Please enter a valid email address");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter a password");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match");
                return;
            }

            // Show loading dialog
            ShowLoadingDialog();

            // Simulate registration process
            await Task.Delay(2000);

            // Register user in database
            var dbHelper = new Database.DatabaseHelper(this);
            bool success = dbHelper.RegisterUser(email, password, fullName, studentId);
            dbHelper.Close();

            // Hide loading dialog
            HideLoadingDialog();

            if (success)
            {
                // Show success dialog
                ShowSuccessDialog();
            }
            else
            {
                ShowError("Registration failed. Email might already be registered.");
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
        }

        private void HideLoadingDialog()
        {
            loadingDialog?.Dismiss();
            loadingDialog = null;
        }

        private void ShowSuccessDialog()
        {
            successDialog = new Dialog(this);
            successDialog.RequestWindowFeature((int)WindowFeatures.NoTitle);
            successDialog.SetContentView(Resource.Layout.dialog_success);
            successDialog.Window?.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
            successDialog.SetCancelable(false);
            successDialog.Show();

            // Auto-dismiss and navigate after 2.5 seconds
            var handler = new Android.OS.Handler(Android.OS.Looper.MainLooper!);
            handler.PostDelayed(() =>
            {
                successDialog?.Dismiss();
                Finish(); // Return to login
            }, 2500);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            loadingDialog?.Dismiss();
            successDialog?.Dismiss();
        }
    }
}
