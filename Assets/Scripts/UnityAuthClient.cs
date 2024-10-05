using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Text;
using System.Linq;

using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using IdentityModel.OidcClient.Infrastructure;
using IdentityModel.OidcClient.Results;

using UnityEngine;
using UnityEngine.Networking;

namespace Assets
{
    public class UnityAuthClient
    {
        private OidcClient _client;
        private LoginResult _result;
        private readonly string authUrl = "https://demo.duendesoftware.com/";
        private const string EditorRedirectUri = "http://localhost:8181/";
        private TaskCompletionSource<LoginResult> _loginTaskCompletionSource;
        private AuthorizeState _authorizeState;

#if UNITY_EDITOR
        private HttpListener httpListener;
        private Thread listenerThread;
#endif

        public UnityAuthClient(string url)
        {
#if UNITY_IOS
            LogSerializer.Enabled = false;
#endif
            authUrl = url;

#if UNITY_EDITOR || UNITY_STANDALONE
            Browser = new StandaloneBrowser();
#elif UNITY_ANDROID
            Browser = new AndroidChromeCustomTabBrowser();
#elif UNITY_IOS
            Browser = new SFSafariViewBrowser();
#endif
            CertificateHandler.Initialize();
        }

        private OidcClient CreateAuthClient()
        {
            var options = new OidcClientOptions()
            {
                Authority = authUrl,
                ClientId = "interactive.public",
                ClientSecret = "secret",
                Scope = "openid profile email",
#if UNITY_EDITOR
                RedirectUri = EditorRedirectUri,
                PostLogoutRedirectUri = EditorRedirectUri,
#else
                RedirectUri = "io.identitymodel.native://callback",
                PostLogoutRedirectUri = "io.identitymodel.native://callback",
#endif
                Browser = Browser,
            };

            options.LoggerFactory.AddProvider(new UnityAuthLoggerProvider());
            return new OidcClient(options);
        }

        public async Task<bool> LoginAsync()
        {
            _client = CreateAuthClient();
            try
            {
#if UNITY_EDITOR
                _loginTaskCompletionSource = new TaskCompletionSource<LoginResult>();
                StartLocalServer();
                var loginRequest = new LoginRequest();
                _authorizeState = await _client.PrepareLoginAsync(loginRequest.FrontChannelExtraParameters);
                Application.OpenURL(_authorizeState.StartUrl);

                // Add timeout handling
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(_loginTaskCompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                    throw new TimeoutException("Login process timed out");

                _result = await _loginTaskCompletionSource.Task;
#else
                _result = await _client.LoginAsync(new LoginRequest());
#endif
            }
            catch (Exception e)
            {
                Debug.Log("UnityAuthClient::Exception during login: " + e.Message);
                return false;
            }
            finally
            {
                Debug.Log("UnityAuthClient::Dismissing sign-in browser.");
                Browser.Dismiss();
#if UNITY_EDITOR
                StopLocalServer();
#endif
            }

            if (_result.IsError)
            {
                Debug.Log("UnityAuthClient::Error authenticating: " + _result.Error);
            }
            else
            {
                Debug.Log("UnityAuthClient::AccessToken: " + _result.AccessToken);
                Debug.Log("UnityAuthClient::RefreshToken: " + _result.RefreshToken);
                Debug.Log("UnityAuthClient::IdentityToken: " + _result.IdentityToken);
                Debug.Log("UnityAuthClient::Signed in.");
                return true;
            }

            return false;
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
#if UNITY_EDITOR
                StartLocalServer();
#endif
                await _client.LogoutAsync(new LogoutRequest()
                {
                    BrowserDisplayMode = DisplayMode.Hidden,
                    IdTokenHint = _result.IdentityToken
                });
                Debug.Log("UnityAuthClient::Signed out successfully.");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("UnityAuthClient::Failed to sign out: " + e.Message);
            }
            finally
            {
                Debug.Log("UnityAuthClient::Dismissing sign-out browser.");
                Browser.Dismiss();
#if UNITY_EDITOR
                StopLocalServer();
#endif
                _client = null;
            }

            return false;
        }

        public string GetUserName()
        {
            return _result == null ? "" : _result.User.Identity.Name;
        }

        public IdentityBrowser Browser { get; }

#if UNITY_EDITOR
        private void StartLocalServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(EditorRedirectUri);
            httpListener.Start();

            listenerThread = new Thread(ListenForCallback);
            listenerThread.Start();
        }

        private void ListenForCallback()
        {
            while (httpListener.IsListening)
            {
                var context = httpListener.GetContext();
                var request = context.Request;
                var response = context.Response;

                if (request.Url.AbsolutePath != "/")
                    continue;

                string responseString = "<html><body>Authentication successful. You can close this window now.</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                var queryParams = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
                var code = queryParams["code"];
                var state = queryParams["state"];

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    Debug.LogError("Missing code or state in the callback URL");
                    _loginTaskCompletionSource.SetException(new InvalidOperationException("Missing code or state in the callback URL"));
                    break;
                }

                if (state != _authorizeState.State)
                {
                    Debug.LogError("State mismatch in the callback URL");
                    _loginTaskCompletionSource.SetException(new InvalidOperationException("State mismatch in the callback URL"));
                    break;
                }

                ExchangeCodeForToken(code);
                break;
            }
        }

        private async void ExchangeCodeForToken(string code)
        {
            try
            {
                if (_authorizeState == null)
                    throw new InvalidOperationException("Authorize state is null. The login process may not have been initiated properly.");

                Debug.Log($"Exchanging code for token. Code: {code.Substring(0, 5)}..., State: {_authorizeState.State.Substring(0, 5)}...");

                // Construct the full redirect URI with the code
                var redirectUri = $"{EditorRedirectUri}?code={code}&state={_authorizeState.State}";
                Debug.Log($"Full redirect URI: {redirectUri}");

                // Process the response
                var result = await _client.ProcessResponseAsync(redirectUri, _authorizeState);

                if (result == null)
                    throw new InvalidOperationException("ProcessResponseAsync returned null result.");

                if (result.IsError)
                {
                    Debug.LogError($"Error processing response: {result.Error}");
                    _loginTaskCompletionSource.SetException(new Exception($"Error processing response: {result.Error}"));
                    return;
                }

                Debug.Log("Token exchange successful.");
                Debug.Log($"Access Token: {result.AccessToken.Substring(0, 10)}...");
                Debug.Log($"Identity Token: {result.IdentityToken.Substring(0, 10)}...");
                Debug.Log($"Refresh Token: {(string.IsNullOrEmpty(result.RefreshToken) ? "Not provided" : "Provided")}");

                _loginTaskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in ExchangeCodeForToken: {ex.Message}");
                Debug.LogException(ex);
                _loginTaskCompletionSource.SetException(ex);
            }
        }

        private void StopLocalServer()
        {
            if (httpListener != null && httpListener.IsListening)
            {
                httpListener.Stop();
            }
            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join();
            }
        }
#endif
    }
}