using Android;
using Android.Content;                    // For Intent and other context-related classes
using Android.Provider;                   // For Settings.ActionManageAppAllFilesAccessPermission
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SafariBooksDownload
{
    public class PermissionHelper
    {
        private const int RequestCode = 1000;
        private static TaskCompletionSource<bool> permissionTcs;

        public static async Task<bool> RequestStoragePermissions()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                #if ANDROID
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    var result = Android.OS.Environment.IsExternalStorageManager;
                    if (!result)
                    {
                        var manage = Settings.ActionManageAppAllFilesAccessPermission;
                        Intent intent = new Intent(manage);
                        Android.Net.Uri uri = Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName);
                        intent.SetData(uri);
                        Platform.CurrentActivity.StartActivity(intent);
                    }
                    return result;
                } else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    // Show alert before requesting permissions
                    await Application.Current.MainPage.DisplayAlert("Permission Request", "Requesting storage permissions...", "OK");
                    return await RequestRuntimePermissions();
                }
                else
                {
                    // Inform user that permissions are granted by default on older Android versions
                    await Application.Current.MainPage.DisplayAlert("Permissions Granted", "Permissions are granted by default on Android versions older than 6.0.", "OK");
                    return true; // Permissions assumed to be granted for versions below Android 6.0
                }
                #endif
            }

            // If not Android, no need to request permissions
            await Application.Current.MainPage.DisplayAlert("Unsupported Platform", "Permission request only needed on Android.", "OK");
            return false;
        }

        private static async Task<bool> RequestRuntimePermissions()
        {
            var readPermissionStatus = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, Manifest.Permission.ReadExternalStorage);
            var writePermissionStatus = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, Manifest.Permission.WriteExternalStorage);

            if (readPermissionStatus != (int)Permission.Granted || writePermissionStatus != (int)Permission.Granted)
            {
                // Inform the user that permissions are needed
                await Application.Current.MainPage.DisplayAlert("Permission Needed", "Storage permissions are required for the app to access files.", "OK");

                permissionTcs = new TaskCompletionSource<bool>();

                // Request both Read and Write permissions
                ActivityCompat.RequestPermissions(
                    Platform.CurrentActivity,
                    new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage },
                    RequestCode
                );

                // Await the result of the user's decision
                return await permissionTcs.Task;
            }

            // Inform user that permissions are already granted
            await Application.Current.MainPage.DisplayAlert("Permissions Granted", "Storage permissions are already granted.", "OK");
            return true;
        }

        public static async void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode)
            {
                bool allGranted = true;

                foreach (var result in grantResults)
                {
                    if (result != Permission.Granted)
                    {
                        allGranted = false;
                        break;
                    }
                }

                if (allGranted)
                {
                    // Notify that permissions were granted
                    await Application.Current.MainPage.DisplayAlert("Permissions Granted", "Storage permissions have been granted.", "OK");
                }
                else
                {
                    // Notify that permissions were denied
                    await Application.Current.MainPage.DisplayAlert("Permissions Denied", "Storage permissions are required for the app to function.", "OK");
                }

                // Set the result to the TaskCompletionSource
                permissionTcs?.SetResult(allGranted);
            }
        }
    }
}
