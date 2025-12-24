using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using System.Threading.Tasks;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "OJT Track", Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_splash);

            // Navigate to MainActivity after a short delay
            StartNextActivity();
        }

        private async void StartNextActivity()
        {
            await Task.Delay(2500); // 2.5 seconds delay
            
            // Check if user is logged in (you might want to check shared preferences here)
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            bool isLoggedIn = prefs != null && prefs.GetBoolean("is_logged_in", false);

            if (isLoggedIn)
            {
                StartActivity(new Intent(this, typeof(MainActivity)));
            }
            else
            {
                StartActivity(new Intent(this, typeof(LoginActivity)));
            }
            Finish();
        }
    }
}
