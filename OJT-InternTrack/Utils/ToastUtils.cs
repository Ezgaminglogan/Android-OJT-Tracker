using Android.App;
using Android.Views;
using Android.Widget;
using System;

namespace OJT_InternTrack.Utils
{
    public static class ToastUtils
    {
        public static void ShowCustomToast(Activity activity, string message)
        {
            if (activity == null) return;

            activity.RunOnUiThread(() =>
            {
                try
                {
                    var inflater = activity.LayoutInflater;
                    if (inflater == null) return;

                    var layout = inflater.Inflate(Resource.Layout.layout_toast_wavy, null);
                    if (layout == null) return;

                    var text = layout.FindViewById<TextView>(Resource.Id.toast_text);
                    if (text != null) text.Text = message;

                    var toast = new Toast(activity);
                    toast.Duration = ToastLength.Short;
                    toast.View = layout;
                    
                    // Center horizontal, top with offset
                    toast.SetGravity(GravityFlags.Top | GravityFlags.CenterHorizontal, 0, 200);
                    toast.Show();
                }
                catch (Exception ex)
                {
                    // Fallback to standard toast
                    Toast.MakeText(activity, message, ToastLength.Short)?.Show();
                    System.Diagnostics.Debug.WriteLine($"Error showing custom toast: {ex.Message}");
                }
            });
        }
    }
}
