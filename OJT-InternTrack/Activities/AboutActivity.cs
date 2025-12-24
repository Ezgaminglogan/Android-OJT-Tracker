using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "About", Theme = "@style/AppTheme")]
    public class AboutActivity : Activity
    {
        private ImageButton? backButton;
        private TextView? versionText;
        private TextView? developerNameText;
        private TextView? developerEmailText;
        private TextView? supportEmailText;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_about);

            InitializeViews();
            SetupEventHandlers();
            LoadAppInfo();
        }

        private void InitializeViews()
        {
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            versionText = FindViewById<TextView>(Resource.Id.versionText);
            developerNameText = FindViewById<TextView>(Resource.Id.developerNameText);
            developerEmailText = FindViewById<TextView>(Resource.Id.developerEmailText);
            supportEmailText = FindViewById<TextView>(Resource.Id.supportEmailText);
        }

        private void SetupEventHandlers()
        {
            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            // Make email addresses clickable
            if (developerEmailText != null)
            {
                developerEmailText.Click += (s, e) =>
                {
                    OpenEmailClient(developerEmailText.Text ?? "");
                };
            }

            if (supportEmailText != null)
            {
                supportEmailText.Click += (s, e) =>
                {
                    // Extract email from text with emoji
                    string emailText = supportEmailText.Text ?? "";
                    string email = emailText.Replace("📧 ", "").Trim();
                    OpenEmailClient(email);
                };
            }
        }

        private void LoadAppInfo()
        {
            // Get app version from package
            try
            {
                var packageInfo = PackageManager?.GetPackageInfo(PackageName ?? "", 0);
                if (packageInfo != null && versionText != null)
                {
                    versionText.Text = $"Version {packageInfo.VersionName}";
                }
            }
            catch
            {
                if (versionText != null)
                {
                    versionText.Text = "Version 1.0.0";
                }
            }

            // Set developer info (customize these)
            if (developerNameText != null)
            {
                developerNameText.Text = "Logan M. Panucat";
            }

            if (developerEmailText != null)
            {
                developerEmailText.Text = "logan.panucat2@gmail.com";
            }

            if (supportEmailText != null)
            {
                supportEmailText.Text = "📧 logan.panucat2@gmail.com";
            }
        }

        private void OpenEmailClient(string email)
        {
            if (string.IsNullOrEmpty(email)) return;

            try
            {
                var intent = new Intent(Intent.ActionSendto);
                intent.SetData(Android.Net.Uri.Parse($"mailto:{email}"));
                intent.PutExtra(Intent.ExtraSubject, "OJT InternTrack - Inquiry");
                
                if (intent.ResolveActivity(PackageManager!) != null)
                {
                    StartActivity(intent);
                }
                else
                {
                    Toast.MakeText(this, "No email app found", ToastLength.Short)?.Show();
                }
            }
            catch
            {
                Toast.MakeText(this, "Unable to open email client", ToastLength.Short)?.Show();
            }
        }
    }
}
