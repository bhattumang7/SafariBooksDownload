#if ANDROID
using Android.Content;
using Android.Provider;
using Android.OS;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
#endif
using Microsoft.Maui.ApplicationModel;

namespace SafariBooksDownload
{
    public static class PermissionHelper
    {
#if ANDROID
        private static ActivityResultLauncher? _launcher;
        private static TaskCompletionSource<bool>? _pending;

        public static void RegisterLauncher(AndroidX.Activity.ComponentActivity activity)
        {
            _launcher = activity.RegisterForActivityResult(
                new ActivityResultContracts.StartActivityForResult(),
                new ResultCallback(_ => _pending?.TrySetResult(Android.OS.Environment.IsExternalStorageManager)));
        }

        private sealed class ResultCallback : Java.Lang.Object, IActivityResultCallback
        {
            private readonly Action<Java.Lang.Object?> _onResult;
            public ResultCallback(Action<Java.Lang.Object?> onResult) => _onResult = onResult;
            public void OnActivityResult(Java.Lang.Object? result) => _onResult(result);
        }
#endif

        public static Task<bool> RequestStoragePermissions()
        {
#if ANDROID
            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
                return Task.FromResult(true);

            if (Android.OS.Environment.IsExternalStorageManager)
                return Task.FromResult(true);

            if (_launcher is null)
                return Task.FromResult(false);

            _pending = new TaskCompletionSource<bool>();
            var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission)
                .SetData(Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName));
            _launcher.Launch(intent);
            return _pending.Task;
#else
            return Task.FromResult(true);
#endif
        }
    }
}
