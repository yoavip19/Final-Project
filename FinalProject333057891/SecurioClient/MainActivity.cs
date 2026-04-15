using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using SecurioModels;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using SecurioClient.Helpers.ServerHelpers;
using SecurioClient.Helpers;
using System;
using SecurioModels.DataTransferObjects;

namespace SecurioClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            try
            {
                _ = RunFullSecurioTest();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QUAKE! [TEST SUITE ERROR] An exception occurred: {ex.Message}");
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public async Task RunFullSecurioTest()
        {
            // --- START OF TEST SUITE ---
            System.Diagnostics.Debug.WriteLine("QUAKE! ### STARTING SECURIO INTEGRATION TESTS ###");

            var authService = new AuthService();
            var profileService = new ProfileService();

            // 1. TEST: Registration (Happy Path)
            var testUser = new User
            {
                Username = "TestUser",
                Email = $"test_{Guid.NewGuid()}@example.com", // Unique email every time
                MasterPasswordKey = EncryptionHelper.DeriveKey("HashedKey123", "dmVyeS1zZWNyZXQtc2FsdA=="),
                AuthSalt = "dmVyeS1zZWNyZXQtc2FsdA==",
                EncryptionSalt = "YW5vdGhlci1zZWNyZXQtc2FsdA=="
            };

            var regResult = await authService.RegisterAsync(testUser);
            System.Diagnostics.Debug.WriteLine($"QUAKE! [REGISTRATION TEST] Success: {regResult.Success}, Message: {regResult.Message}");

            if (regResult.Success)
            {
                // 2. TEST: Auto-Login Verification
                var cachedUser = await StorageHelper.GetCachedProfileAsync(); // Should exist if registration worked
                bool isSessionActive = SessionHelper.IsAuthenticated;
                System.Diagnostics.Debug.WriteLine($"QUAKE! [AUTO-LOGIN TEST] Session Active: {isSessionActive}, Cached User: {cachedUser?.Email}");

                // 3. TEST: Profile Fetch (Success)
                var profile = await profileService.GetProfileAsync();
                if (profile.Success)
                {
                    // Save to cache
                    await StorageHelper.SaveProfileAsync(profile.Data);

                    // Load from cache to verify DateTimes
                    var cached = await StorageHelper.GetCachedProfileAsync();

                    System.Diagnostics.Debug.WriteLine($"QUAKE! [DATE TEST] Created At: {cached.CreatedAt}");
                    System.Diagnostics.Debug.WriteLine($"QUAKE! [DATE TEST] Last Login: {cached.LastLogin}");

                    if (cached.CreatedAt != DateTime.MinValue)
                        System.Diagnostics.Debug.WriteLine($"QUAKE! ✅ Timestamps successfully recovered from cache.");
                    else
                        System.Diagnostics.Debug.WriteLine($"QUAKE! ❌ Timestamp recovery failed.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"QUAKE! [PROFILE TEST] Failed to fetch profile: {profile.Message}");
                }
            }

            // 4. TEST: Login with Wrong Password (Error Path)
            var wrongLogin = await authService.LoginAsync(testUser.Email, "WrongPassword_123");
            System.Diagnostics.Debug.WriteLine($"QUAKE! [WRONG PASSWORD TEST] Success: {wrongLogin.Success} (Should be False), Message: {wrongLogin.Message}");

            // 5. TEST: Duplicate Registration (Error Path)
            var dupReg = await authService.RegisterAsync(testUser);
            System.Diagnostics.Debug.WriteLine($"QUAKE! [DUPLICATE REG TEST] Success: {dupReg.Success} (Should be False), Message: {dupReg.Message}");

            // 6. TEST: Non-existent User Salts (Error Path)
            // Assuming you implemented GetSalts in AuthService
            var fakeSalts = await authService.LoginAsync("fake_user_999@none.com", "any");
            System.Diagnostics.Debug.WriteLine($"QUAKE! [FAKE USER TEST] Success: {fakeSalts.Success} (Should be False), Message: {fakeSalts.Message}");

            // TEST: Login with Wrong Password
            var wrongPasswordResult = await authService.LoginAsync("test_ada45678-52d1-48b0-abce-0a5511b11267@example.com", "AbsolutelyWrongPassword123!");
            System.Diagnostics.Debug.WriteLine($"QUAKE! [AUTH TEST] Wrong Password - Success: {wrongPasswordResult.Success} (Should be False), Message: {wrongPasswordResult.Message}");

            // TEST: Login with Non-existent Email
            var fakeUserResult = await authService.LoginAsync("ghost_user_999@none.com", "any_password");
            System.Diagnostics.Debug.WriteLine($"QUAKE! [AUTH TEST] Fake User - Success: {fakeUserResult.Success} (Should be False), Message: {fakeUserResult.Message}");

            // TEST: Get Profile for ID that doesn't exist (e.g., 99999)
            // You might need to temporarily modify ProfileService to accept an ID for this test
            await StorageHelper.SaveUserId(99999); // Force a non-existent ID into storage for testing
            var ghostProfile = await profileService.GetProfileAsync();
            System.Diagnostics.Debug.WriteLine($"QUAKE! [PROFILE TEST] Non-existent ID - Success: {ghostProfile.Success} (Should be False), Message: {ghostProfile.Message}");


            System.Diagnostics.Debug.WriteLine("QUAKE! ### TESTS COMPLETED ###");
            // --- END OF TEST SUITE ---

        }
    }
}