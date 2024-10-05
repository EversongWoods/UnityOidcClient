using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using IdentityModel.OidcClient.Results;

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Assets
{
    public class TokenResult : Result
    {
        public string IdentityToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTimeOffset AccessTokenExpiration { get; set; }
    }

    public class UnityOidcClient
    {
        private readonly string AuthUrl = "https://demo.duendesoftware.com/";
        private const string EditorRedirectUri = "http://localhost:8181/";

        public string UserName => LoginResult == null ? "" : LoginResult.User.Identity.Name;
        public LoginResult LoginResult { get; private set; }
        public TokenResult TokenResult { get; private set; }
        public IdentityBrowser Browser { get; }

        private OidcClient _client;
        private DiscoveryCache _discoveryCache;
        private AuthorizeState _authorizeState;
        private TaskCompletionSource<LoginResult> _loginTaskCompletionSource;

        private HttpListener httpListener;
        private Thread listenerThread;

        public UnityOidcClient(string url)
        {
#if UNITY_IOS
            LogSerializer.Enabled = false;
#endif
            AuthUrl = string.IsNullOrWhiteSpace(url) ? AuthUrl : url;
#if UNITY_EDITOR || UNITY_STANDALONE
            Browser = new StandaloneBrowser();
#elif UNITY_ANDROID
            Browser = new AndroidChromeCustomTabBrowser();
#elif UNITY_IOS
            Browser = new SFSafariViewBrowser();
#endif
            CertificateHandler.Initialize();
        }

        private async Task<DiscoveryDocumentResponse> GetDiscoveryDocumentAsync()
        {
            if (_discoveryCache == null)
                _discoveryCache = new DiscoveryCache(AuthUrl);

            var discoveryDocument = await _discoveryCache.GetAsync();
            if (discoveryDocument.IsError)
            {
                Debug.LogError($"Discovery error: {discoveryDocument.Error}");
                if (discoveryDocument.Exception != null)
                    Debug.LogException(discoveryDocument.Exception);
            }
            else
            {
                Debug.Log("Discovery document retrieved successfully");
                Debug.Log($"Authorization Endpoint: {discoveryDocument.AuthorizeEndpoint}");
                Debug.Log($"Token Endpoint: {discoveryDocument.TokenEndpoint}");
            }

            return discoveryDocument;
        }

        private OidcClient CreateAuthClient(DiscoveryDocumentResponse discoveryDocument)
        {
            var options = new OidcClientOptions()
            {
                Authority = AuthUrl,
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

            options.ProviderInformation = new ProviderInformation
            {
                IssuerName = discoveryDocument.Issuer,
                AuthorizeEndpoint = discoveryDocument.AuthorizeEndpoint,
                TokenEndpoint = discoveryDocument.TokenEndpoint,
                EndSessionEndpoint = discoveryDocument.EndSessionEndpoint,
                UserInfoEndpoint = discoveryDocument.UserInfoEndpoint,
                KeySet = discoveryDocument.KeySet
            };

            options.LoggerFactory.AddProvider(new UnityAuthLoggerProvider());
            return new OidcClient(options);
        }

        public async Task<bool> LoginAsync()
        {
            var discoveryDocument = await GetDiscoveryDocumentAsync();
            if (discoveryDocument.IsError)
            {
                Debug.LogError($"Error retrieving discovery document: {discoveryDocument.Error}");
                return false;
            }

            _client = CreateAuthClient(discoveryDocument);
            try
            {
                var loginRequest = new LoginRequest();
#if UNITY_EDITOR
                _loginTaskCompletionSource = new TaskCompletionSource<LoginResult>();
                StartLocalServer();

                _authorizeState = await _client.PrepareLoginAsync(loginRequest.FrontChannelExtraParameters);
                Application.OpenURL(_authorizeState.StartUrl);

                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(_loginTaskCompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                    throw new TimeoutException("Login process timed out");

                LoginResult = await _loginTaskCompletionSource.Task;
#else
                LoginResult = await _client.LoginAsync(loginRequest);
#endif
                TokenResult = new TokenResult
                {
                    AccessToken = LoginResult.AccessToken,
                    RefreshToken = LoginResult.RefreshToken,
                    IdentityToken = LoginResult.IdentityToken,
                    AccessTokenExpiration = LoginResult.AccessTokenExpiration,
                };
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

            if (LoginResult.IsError)
            {
                Debug.Log("UnityAuthClient::Error authenticating: " + LoginResult.Error);
            }
            else
            {
                Debug.Log($"UnityAuthClient::AccessToken: {LoginResult.AccessToken}, \nRefreshToken: {LoginResult.RefreshToken} \nIdentityToken: {LoginResult.IdentityToken}");
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
                    IdTokenHint = LoginResult.IdentityToken
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

        public async Task<bool> RefreshTokenAsync()
        {
            if (_client == null
                || LoginResult == null
                || string.IsNullOrEmpty(LoginResult.RefreshToken))
            {
                Debug.LogError("Cannot refresh token. Client not initialized or refresh token is missing.");
                return false;
            }

            try
            {
                var refreshResult = await _client.RefreshTokenAsync(LoginResult.RefreshToken);
                if (refreshResult.IsError)
                {
                    Debug.LogError($"Error refreshing token: {refreshResult.Error}");
                    return false;
                }

                TokenResult = new TokenResult
                {
                    IdentityToken = refreshResult.IdentityToken,
                    RefreshToken = refreshResult.RefreshToken,
                    AccessToken = refreshResult.AccessToken,
                    AccessTokenExpiration = refreshResult.AccessTokenExpiration,
                    ExpiresIn = refreshResult.ExpiresIn
                };

                // Update the current result with the new tokens
                Debug.Log("Token refreshed successfully.");
                Debug.Log($"New Access Token: {refreshResult.AccessToken.Substring(0, 10)}...," +
                    $"\nToken: {refreshResult.IdentityToken.Substring(0, 10)}..., " +
                    $"\nRefresh Token: {(refreshResult.RefreshToken != null ? "Provided" : "Not updated")}, " +
                    $"\nExpires in: {refreshResult.ExpiresIn} seconds");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception during token refresh: {e.Message}");
                return false;
            }
        }

        public bool IsTokenExpired()
        {
            if (TokenResult == null
                || TokenResult.AccessTokenExpiration == null)
                return true;

            // Add a buffer of 5 minutes to ensure we refresh before the token actually expires
            return TokenResult.AccessTokenExpiration < DateTime.UtcNow.AddMinutes(5);
        }

        public async Task<string> GetValidAccessTokenAsync()
        {
            if (IsTokenExpired())
            {
                bool refreshed = await RefreshTokenAsync();
                if (!refreshed)
                {
                    Debug.LogError("Failed to refresh token. User may need to log in again.");
                    return null;
                }
            }

            return TokenResult.AccessToken;
        }

        private void StartLocalServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(EditorRedirectUri);
            httpListener.Start();

            listenerThread = new Thread(ListenForCallback);
            listenerThread.Start();
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

                var redirectUri = $"{EditorRedirectUri}?code={code}&state={_authorizeState.State}";
                var result = await _client.ProcessResponseAsync(redirectUri, _authorizeState);

                if (result == null)
                    throw new InvalidOperationException($"ProcessResponseAsync returned null result. url={redirectUri}");

                if (result.IsError)
                {
                    Debug.LogError($"Error processing response: {result.Error}");
                    _loginTaskCompletionSource.SetException(new Exception($"Error processing response: {result.Error}"));
                    return;
                }

                Debug.Log("Token exchange successful.");
                Debug.Log($"Access Token: {result.AccessToken.Substring(0, 10)}...," +
                    $"\nIdentity Token: {result.IdentityToken.Substring(0, 10)}...," +
                    $"\nRefresh Token: {(string.IsNullOrEmpty(result.RefreshToken) ? "Not provided" : "Provided")}");

                _loginTaskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in ExchangeCodeForToken: {ex.Message}");
                Debug.LogException(ex);
                _loginTaskCompletionSource.SetException(ex);
            }
        }
    }
}