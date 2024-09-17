using Android;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class PermissionHelper
    {
        public static async Task RequestStoragePermissions()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                #if ANDROID
                // Check if the Android version requires runtime permissions
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    // Android 6.0 (API 23) and above requires requesting runtime permissions
                    await RequestRuntimePermissions();
                }
                else
                {
                    // For Android versions before 6.0 (API 23), permissions are granted at install time
                    Console.WriteLine("Permissions granted by default on older Android versions.");
                }
                #endif
            }
        }

        private static async Task RequestRuntimePermissions()
        {
            var readPermissionStatus = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, Manifest.Permission.ReadExternalStorage);
            var writePermissionStatus = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, Manifest.Permission.WriteExternalStorage);

            // Check if permissions are not already granted
            if (readPermissionStatus != (int)Android.Content.PM.Permission.Granted || writePermissionStatus != (int)Android.Content.PM.Permission.Granted)
            {
                // Request both Read and Write permissions
                ActivityCompat.RequestPermissions(
                    Platform.CurrentActivity,
                    new string[]
                    {
                        Manifest.Permission.ReadExternalStorage,
                        Manifest.Permission.WriteExternalStorage
                    },
                    0
                );
            }
            else
            {
                Console.WriteLine("Permissions already granted.");
            }
        }
    }
}
