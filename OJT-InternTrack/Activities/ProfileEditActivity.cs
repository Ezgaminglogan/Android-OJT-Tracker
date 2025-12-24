using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Widget;
using AndroidX.Core.Content;
using OJT_InternTrack.Database;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "Edit Profile", Theme = "@style/AppTheme")]
    public class ProfileEditActivity : Activity
    {
        private ImageView? profileImageView;
        private ImageButton? changePhotoButton;
        private ImageButton? backButton;
        private Button? saveButton;
        private EditText? fullNameEditText;
        private EditText? studentIdEditText;
        private EditText? emailEditText;
        private EditText? currentPasswordEditText;
        private EditText? newPasswordEditText;
        private EditText? confirmPasswordEditText;
        private EditText? requiredHoursEditText;
        private TextView? startDateText;
        private LinearLayout? startDatePickerContainer;
        private DateTime? selectedStartDate;

        private DatabaseHelper? dbHelper;
        private string? userEmail;
        private int userId;
        private Bitmap? selectedProfileImage;
        private Dialog? loadingDialog;

        private const int PICK_IMAGE_REQUEST = 1;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_profile_edit);

            // Get user info
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            userEmail = prefs?.GetString("user_email", "");
            userId = prefs?.GetInt("user_id", -1) ?? -1;

            // Initialize database helper
            dbHelper = new DatabaseHelper(this);

            // Initialize views
            InitializeViews();

            // Load current user data
            LoadUserData();

            // Setup event handlers
            SetupEventHandlers();
        }

        private void InitializeViews()
        {
            profileImageView = FindViewById<ImageView>(Resource.Id.profileImageView);
            changePhotoButton = FindViewById<ImageButton>(Resource.Id.changePhotoButton);
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            saveButton = FindViewById<Button>(Resource.Id.saveButton);
            fullNameEditText = FindViewById<EditText>(Resource.Id.fullNameEditText);
            studentIdEditText = FindViewById<EditText>(Resource.Id.studentIdEditText);
            emailEditText = FindViewById<EditText>(Resource.Id.emailEditText);
            currentPasswordEditText = FindViewById<EditText>(Resource.Id.currentPasswordEditText);
            newPasswordEditText = FindViewById<EditText>(Resource.Id.newPasswordEditText);
            confirmPasswordEditText = FindViewById<EditText>(Resource.Id.confirmPasswordEditText);
            requiredHoursEditText = FindViewById<EditText>(Resource.Id.requiredHoursEditText);
            startDateText = FindViewById<TextView>(Resource.Id.startDateText);
            startDatePickerContainer = FindViewById<LinearLayout>(Resource.Id.startDatePickerContainer);
            
            var deleteAccountButton = FindViewById<Button>(Resource.Id.deleteAccountButton);
            if (deleteAccountButton != null)
            {
                deleteAccountButton.Click += DeleteAccountButton_Click;
            }
        }

        private void LoadUserData()
        {
            if (dbHelper == null || string.IsNullOrEmpty(userEmail)) return;

            var db = dbHelper.ReadableDatabase;
            if (db == null) return;

            var cursor = db.RawQuery(
                $"SELECT {DatabaseHelper.ColFullName}, {DatabaseHelper.ColStudentId}, {DatabaseHelper.ColRequiredHours}, {DatabaseHelper.ColOJTStartDate} FROM {DatabaseHelper.TableUsers} WHERE {DatabaseHelper.ColEmail} = ?",
                new[] { userEmail }
            );

            if (cursor != null && cursor.MoveToFirst())
            {
                string fullName = cursor.GetString(0) ?? "";
                string studentId = cursor.GetString(1) ?? "";

                if (fullNameEditText != null)
                {
                    fullNameEditText.Text = fullName;
                }

                if (studentIdEditText != null)
                {
                    studentIdEditText.Text = studentId;
                }

                if (emailEditText != null)
                {
                    emailEditText.Text = userEmail;
                }

                // Load required hours
                int requiredHours = cursor.GetInt(2);
                if (requiredHoursEditText != null)
                {
                    requiredHoursEditText.Text = requiredHours.ToString();
                }

                // Load start date
                string? startDateStr = cursor.GetString(3);
                if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, out var sd))
                {
                    selectedStartDate = sd;
                    if (startDateText != null)
                    {
                        startDateText.Text = sd.ToString("MMM dd, yyyy");
                    }
                }
            }
            cursor?.Close();

            // Load profile image from preferences if exists
            LoadProfileImage();
        }

        private void LoadProfileImage()
        {
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            string? imagePath = prefs?.GetString("profile_image_path", null);

            if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
            {
                try
                {
                    // Decode with options to avoid hardware bitmap
                    var options = new BitmapFactory.Options
                    {
                        InPreferredConfig = Bitmap.Config.Argb8888!
                    };
                    var bitmap = BitmapFactory.DecodeFile(imagePath, options);
                    
                    if (bitmap != null && profileImageView != null)
                    {
                        // Ensure bitmap is mutable and software-backed
                        var mutableBitmap = bitmap.Copy(Bitmap.Config.Argb8888!, true);
                        
                        // Create circular bitmap
                        var circularBitmap = GetCircularBitmap(mutableBitmap);
                        profileImageView.SetImageBitmap(circularBitmap);
                        
                        // Clean up
                        bitmap.Recycle();
                        mutableBitmap.Recycle();
                    }
                }
                catch
                {
                    // If loading fails, keep default image
                }
            }
        }

        private void SetupEventHandlers()
        {
            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            if (changePhotoButton != null)
            {
                changePhotoButton.Click += ChangePhotoButton_Click;
            }

            if (profileImageView != null)
            {
                profileImageView.Click += ChangePhotoButton_Click;
            }

            if (saveButton != null)
            {
                saveButton.Click += SaveButton_Click;
            }

            if (startDatePickerContainer != null)
            {
                startDatePickerContainer.Click += StartDatePickerContainer_Click;
            }
        }

        private void StartDatePickerContainer_Click(object? sender, EventArgs e)
        {
            DateTime date = selectedStartDate ?? DateTime.Today;
            var picker = new DatePickerDialog(this, (s, args) =>
            {
                selectedStartDate = args.Date;
                if (startDateText != null)
                {
                    startDateText.Text = selectedStartDate.Value.ToString("MMM dd, yyyy");
                }
            }, date.Year, date.Month - 1, date.Day);
            picker.Show();
        }

        private void ChangePhotoButton_Click(object? sender, EventArgs e)
        {
            // Create intent to pick image from gallery
            var intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.ExternalContentUri);
            intent.SetType("image/*");
            StartActivityForResult(Intent.CreateChooser(intent, "Select Profile Picture"), PICK_IMAGE_REQUEST);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == PICK_IMAGE_REQUEST && resultCode == Result.Ok && data != null)
            {
                var selectedImageUri = data.Data;
                if (selectedImageUri != null)
                {
                    try
                    {
                        // Load the selected image
                        Bitmap? bitmap;
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                        {
                            var source = ImageDecoder.CreateSource(ContentResolver!, selectedImageUri);
                            var tempBitmap = ImageDecoder.DecodeBitmap(source);
                            // Immediately copy to software-backed bitmap to avoid hardware bitmap issues
                            bitmap = tempBitmap?.Copy(Bitmap.Config.Argb8888!, true);
                            tempBitmap?.Recycle();
                        }
                        else
                        {
#pragma warning disable CA1422
                            bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, selectedImageUri);
#pragma warning restore CA1422
                        }
                        
                        if (bitmap != null)
                        {
                            // Ensure bitmap is mutable and software-backed
                            var mutableBitmap = bitmap.Copy(Bitmap.Config.Argb8888!, true);
                            bitmap.Recycle();
                            
                            // Fix orientation based on EXIF data
                            mutableBitmap = FixImageOrientation(selectedImageUri, mutableBitmap);
                            
                            // Resize and compress the image
                            selectedProfileImage = ResizeImage(mutableBitmap, 500, 500);
                            mutableBitmap.Recycle();
                            
                            // Display in ImageView as circular
                            if (profileImageView != null)
                            {
                                var circularBitmap = GetCircularBitmap(selectedProfileImage);
                                profileImageView.SetImageBitmap(circularBitmap);
                            }

                            Toast.MakeText(this, "Photo selected! Click Save to apply changes.", ToastLength.Short)?.Show();
                        }
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, $"Error loading image: {ex.Message}", ToastLength.Short)?.Show();
                    }
                }
            }
        }

        private Bitmap FixImageOrientation(Android.Net.Uri imageUri, Bitmap bitmap)
        {
            try
            {
                // Get the image orientation from EXIF data
                var inputStream = ContentResolver?.OpenInputStream(imageUri);
                if (inputStream != null)
                {
                    var exif = new Android.Media.ExifInterface(inputStream);
                    int orientation = exif.GetAttributeInt(
                        Android.Media.ExifInterface.TagOrientation,
                        (int)Android.Media.Orientation.Normal);

                    inputStream.Close();

                    // Rotate bitmap based on orientation
                    Matrix matrix = new Matrix();
                    switch (orientation)
                    {
                        case (int)Android.Media.Orientation.Rotate90:
                            matrix.PostRotate(90);
                            break;
                        case (int)Android.Media.Orientation.Rotate180:
                            matrix.PostRotate(180);
                            break;
                        case (int)Android.Media.Orientation.Rotate270:
                            matrix.PostRotate(270);
                            break;
                        case (int)Android.Media.Orientation.FlipHorizontal:
                            matrix.PreScale(-1, 1);
                            break;
                        case (int)Android.Media.Orientation.FlipVertical:
                            matrix.PreScale(1, -1);
                            break;
                        default:
                            return bitmap; // No rotation needed
                    }

                    // Create rotated bitmap
                    var rotatedBitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
                    if (rotatedBitmap != bitmap)
                    {
                        bitmap.Recycle();
                    }
                    return rotatedBitmap;
                }
            }
            catch
            {
                // If we can't read EXIF, return original bitmap
            }

            return bitmap;
        }

        private Bitmap ResizeImage(Bitmap originalImage, int maxWidth, int maxHeight)
        {
            int width = originalImage.Width;
            int height = originalImage.Height;

            float ratioBitmap = (float)width / (float)height;
            float ratioMax = (float)maxWidth / (float)maxHeight;

            int finalWidth = maxWidth;
            int finalHeight = maxHeight;

            if (ratioMax > ratioBitmap)
            {
                finalWidth = (int)((float)maxHeight * ratioBitmap);
            }
            else
            {
                finalHeight = (int)((float)maxWidth / ratioBitmap);
            }

            return Bitmap.CreateScaledBitmap(originalImage, finalWidth, finalHeight, true);
        }

        private Bitmap GetCircularBitmap(Bitmap bitmap)
        {
            int size = Math.Min(bitmap.Width, bitmap.Height);

            var output = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888!);
            var canvas = new Canvas(output);

            var paint = new Paint();
            var rect = new Rect(0, 0, size, size);

            paint.AntiAlias = true;
            canvas.DrawARGB(0, 0, 0, 0);
            paint.Color = Color.White;

            // Draw circle
            canvas.DrawCircle(size / 2f, size / 2f, size / 2f, paint);

            // Cut out the middle
            paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.SrcIn!));

            // Center the source bitmap
            int xOffset = (bitmap.Width - size) / 2;
            int yOffset = (bitmap.Height - size) / 2;
            var srcRect = new Rect(xOffset, yOffset, xOffset + size, yOffset + size);

            canvas.DrawBitmap(bitmap, srcRect, rect, paint);

            return output;
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            if (dbHelper == null || string.IsNullOrEmpty(userEmail))
            {
                Toast.MakeText(this, "Error: No user session found", ToastLength.Short)?.Show();
                return;
            }

            // Validate inputs
            string fullName = fullNameEditText?.Text?.Trim() ?? "";
            string studentId = studentIdEditText?.Text?.Trim() ?? "";
            string currentPassword = currentPasswordEditText?.Text?.Trim() ?? "";
            string newPassword = newPasswordEditText?.Text?.Trim() ?? "";
            string confirmPassword = confirmPasswordEditText?.Text?.Trim() ?? "";
            
            string reqHoursStr = requiredHoursEditText?.Text?.Trim() ?? "600";
            int.TryParse(reqHoursStr, out int reqHours);
            if (reqHours <= 0) reqHours = 600;

            string? startDateStr = selectedStartDate?.ToString("yyyy-MM-dd");

            if (string.IsNullOrEmpty(fullName))
            {
                Toast.MakeText(this, "Please enter your full name", ToastLength.Short)?.Show();
                return;
            }

            if (string.IsNullOrEmpty(studentId))
            {
                Toast.MakeText(this, "Please enter your student ID", ToastLength.Short)?.Show();
                return;
            }

            // Check if user wants to change password
            bool changingPassword = !string.IsNullOrEmpty(currentPassword) || 
                                   !string.IsNullOrEmpty(newPassword) || 
                                   !string.IsNullOrEmpty(confirmPassword);

            if (changingPassword)
            {
                // Validate password change
                if (string.IsNullOrEmpty(currentPassword))
                {
                    Toast.MakeText(this, "Please enter your current password", ToastLength.Short)?.Show();
                    return;
                }

                if (string.IsNullOrEmpty(newPassword))
                {
                    Toast.MakeText(this, "Please enter a new password", ToastLength.Short)?.Show();
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    Toast.MakeText(this, "New passwords do not match", ToastLength.Short)?.Show();
                    return;
                }

                if (newPassword.Length < 6)
                {
                    Toast.MakeText(this, "New password must be at least 6 characters", ToastLength.Short)?.Show();
                    return;
                }

                // Verify current password
                if (!dbHelper.ValidateUser(userEmail, currentPassword))
                {
                    Toast.MakeText(this, "Current password is incorrect", ToastLength.Short)?.Show();
                    return;
                }
            }

            // Show loading dialog
            ShowLoadingDialog();

            // Simulate processing delay
            await Task.Delay(1500);

            try
            {
                // Check if user exists first
                int checkUserId = dbHelper.GetUserId(userEmail);
                if (checkUserId == -1)
                {
                    HideLoadingDialog();
                    Toast.MakeText(this, "User account not found. Please logout and register again.", ToastLength.Long)?.Show();
                    return;
                }

                // Update user profile
                bool success = dbHelper.UpdateUserProfile(userEmail, fullName, studentId, reqHours, startDateStr);

                if (!success)
                {
                    HideLoadingDialog();
                    Toast.MakeText(this, $"Database error. Please try again or contact support.", ToastLength.Long)?.Show();
                    return;
                }

                // Auto-update schedule based on new start date or hours
                if (selectedStartDate.HasValue)
                {
                    dbHelper.RegenerateSchedule(checkUserId, selectedStartDate.Value);
                }

                if (changingPassword)
                {
                    // Update password if requested
                    success = dbHelper.UpdateUserPassword(userEmail, newPassword);
                    if (!success)
                    {
                        HideLoadingDialog();
                        Toast.MakeText(this, "Profile updated but failed to change password", ToastLength.Long)?.Show();
                        return;
                    }
                }

                // Save profile image if selected
                if (selectedProfileImage != null)
                {
                    SaveProfileImage();
                }

                HideLoadingDialog();
                Toast.MakeText(this, "Profile updated successfully!", ToastLength.Long)?.Show();
                
                // Clear password fields
                if (currentPasswordEditText != null) currentPasswordEditText.Text = "";
                if (newPasswordEditText != null) newPasswordEditText.Text = "";
                if (confirmPasswordEditText != null) confirmPasswordEditText.Text = "";

                // Go back to dashboard
                Finish();
            }
            catch (Exception ex)
            {
                HideLoadingDialog();
                Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long)?.Show();
            }
        }

        private void SaveProfileImage()
        {
            if (selectedProfileImage == null) return;

            try
            {
                // Create app-specific directory for images
                var directory = System.IO.Path.Combine(
                    Android.OS.Environment.GetExternalStoragePublicDirectory(
                        Android.OS.Environment.DirectoryPictures)?.AbsolutePath ?? "",
                    "OJT_InternTrack"
                );

                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // Create file path
                string fileName = $"profile_{userId}_{DateTime.Now.Ticks}.jpg";
                string filePath = System.IO.Path.Combine(directory, fileName);

                // Save bitmap to file
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    selectedProfileImage.Compress(Bitmap.CompressFormat.Jpeg!, 90, stream);
                }

                // Save path to preferences
                var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
                var editor = prefs?.Edit();
                editor?.PutString("profile_image_path", filePath);
                editor?.Apply();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Error saving image: {ex.Message}", ToastLength.Short)?.Show();
            }
        }

        private void DeleteAccountButton_Click(object? sender, EventArgs e)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Delete Account");
            builder.SetMessage("Are you absolutely sure you want to delete your account? This will permanently remove all your data, including schedules, tasks, and time entries. This action cannot be undone.");
            builder.SetPositiveButton("Delete", (s, ev) =>
            {
                if (dbHelper != null && !string.IsNullOrEmpty(userEmail))
                {
                    bool deleted = dbHelper.DeleteUser(userEmail);
                    if (deleted)
                    {
                        // Clear login state
                        var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
                        var editor = prefs?.Edit();
                        editor?.Clear();
                        editor?.Apply();

                        Toast.MakeText(this, "Account deleted successfully", ToastLength.Long)?.Show();

                        // Redirect to Login
                        var intent = new Intent(this, typeof(LoginActivity));
                        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.ClearTask);
                        StartActivity(intent);
                        Finish();
                    }
                    else
                    {
                        Toast.MakeText(this, "Failed to delete account", ToastLength.Short)?.Show();
                    }
                }
            });
            builder.SetNegativeButton("Cancel", (s, ev) => { });
            builder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
            builder.Show();
        }

        private void ShowLoadingDialog()
        {
            if (loadingDialog != null && loadingDialog.IsShowing)
                return;

            loadingDialog = new Dialog(this);
            loadingDialog.RequestWindowFeature((int)Android.Views.WindowFeatures.NoTitle);
            loadingDialog.SetContentView(Resource.Layout.dialog_loading);
            loadingDialog.Window?.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
            loadingDialog.SetCancelable(false);
            loadingDialog.Show();

            // Update loading text
            var loadingText = loadingDialog.FindViewById<TextView>(Resource.Id.loadingText);
            if (loadingText != null)
            {
                loadingText.Text = "Updating profile...";
            }
        }

        private void HideLoadingDialog()
        {
            loadingDialog?.Dismiss();
            loadingDialog = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            loadingDialog?.Dismiss();
            dbHelper?.Close();
            selectedProfileImage?.Dispose();
        }
    }
}
