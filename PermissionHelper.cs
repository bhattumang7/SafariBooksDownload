using Android;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using Android.App;
using Microsoft.Maui.Controls;

namespace SafariBooksDownload
{
    public class PermissionHelper
    {
        private const int RequestCode = 1000;

        public static async Task<bool> RequestStoragePermissions()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                #if ANDROID
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    // Android 6.0 (API 23) and above requires requesting runtime permissions
                    return await RequestRuntimePermissions();
                }
                else
                {
                    // Assume permissions are granted by default for older Android versions
                    return true;
                }
                #endif
            }
            return false; // Platform is not Android
        }

        private static TaskCompletionSource<bool> permissionTcs;

        private static async Task<bool> RequestRuntimePermissions()
        {
            var readPermissionStatus = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, Manifest.Permission.ReadExternalStorage);
            var writePermissionStatus = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, Manifest.Permission.WriteExternalStorage);

            if (readPermissionStatus != (int)Permission.Granted || writePermissionStatus != (int)Permission.Granted)
            {
                // Request permissions if not already granted
                permissionTcs = new TaskCompletionSource<bool>();

                ActivityCompat.RequestPermissions(
                    Platform.CurrentActivity,
                    new string[]
                    {
                        Manifest.Permission.ReadExternalStorage,
                        Manifest.Permission.WriteExternalStorage
                    },
                    RequestCode
                );

                // Wait for user's response
                return await permissionTcs.Task;
            }
            else
            {
                // Permissions already granted
                return true;
            }
        }

        // This method should be called from the MainActivity.cs to handle the permission request result
        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode)
            {
                // Check if the permissions were granted
                bool allGranted = true;
                for (int i = 0; i < grantResults.Length; i++)
                {
                    if (grantResults[i] != Permission.Granted)
                    {
                        allGranted = false;
                        break;
                    }
                }

                // Set the result to the TaskCompletionSource
                permissionTcs?.SetResult(allGranted);
            }
        }
    }
}
