using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace AddnPrintHD.Models
{
    public static class TokenService
    {
        private static IPublicClientApplication _app;
        private static string[] Scopes = new[] { "https://fristadev.crm4.dynamics.com/.default" };
        private static string _accessToken = string.Empty;

        private const string ClientId = "705e5379-a911-42a2-80ec-8d273f85dbf6";
        private const string TenantId = "9912b00d-f0bf-48cc-9ddb-36f00f044cef";

        private static bool _isFetching = false;

        public static async Task<string> GetAccessToken()
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return _accessToken;

            if (_isFetching)
            {
                while (_isFetching)
                    await Task.Delay(100);
                return _accessToken;
            }

            _isFetching = true;

            if (_app == null)
            {
                _app = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
                    .WithRedirectUri("http://localhost")
                    .Build();
            }

            try
            {
                var accounts = await _app.GetAccountsAsync();
                AuthenticationResult result = null;
                var firstAccount = accounts.FirstOrDefault();

                try
                {
                    if (firstAccount != null)
                    {
                        Console.WriteLine($"Trying silent login for: {firstAccount.Username}");
                        result = await _app.AcquireTokenSilent(Scopes, firstAccount)
                                           .ExecuteAsync();
                    }
                    else
                    {
                        Console.WriteLine("No cached account found, using interactive login...");
                        result = await _app.AcquireTokenInteractive(Scopes)
                                           .WithPrompt(Prompt.SelectAccount)
                                           .ExecuteAsync();
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    Console.WriteLine($"Silent login failed: {ex.Message}");
                    result = await _app.AcquireTokenInteractive(Scopes)
                                       .WithPrompt(Prompt.SelectAccount)
                                       .ExecuteAsync();
                }

                if (result != null && !string.IsNullOrEmpty(result.AccessToken))
                {
                    _accessToken = result.AccessToken;
                    Console.WriteLine($"Token acquired: {_accessToken.Substring(0, 20)}...");
                }
                else
                {
                    Console.WriteLine("Token acquisition failed or empty.");
                }

                return _accessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token retrieval failed: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                _isFetching = false;
            }
        }
        public static string GetCurrentToken()
        {
            return _accessToken;
        }
    }
}
