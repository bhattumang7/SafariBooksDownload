using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace SafariBooksDownload
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            // Pass the result to the PermissionHelper
            PermissionHelper.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool hasPermission = await PermissionHelper.RequestStoragePermissions();

        if (!hasPermission)
        {
            await DisplayAlert("Permission Required", "Storage access is required to proceed.", "OK");
            // Handle the case where permission is not granted (e.g., disable file access features)
        }
        else
        {
            // Proceed with file access since permission is granted
        }
    }
    
}
