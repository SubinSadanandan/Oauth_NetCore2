using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageGallery.Client.Services
{
    public class ImageGalleryHttpClient : IImageGalleryHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpClient _httpClient = new HttpClient();

        public ImageGalleryHttpClient(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        //http://localhost:1601/
        public async Task<HttpClient> GetClient()
        {
            string accessToken = string.Empty;
            var currentContext = _httpContextAccessor.HttpContext;


            var expires_at = await currentContext.GetTokenAsync("expires_at");

            if(string.IsNullOrWhiteSpace(expires_at) ||
                ((DateTime.Parse(expires_at).AddSeconds(-60)).ToUniversalTime() < DateTime.UtcNow))
            {
                accessToken = await RenewTokens();
            }
            else
            {
                accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            }

            if(!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }

            _httpClient.BaseAddress = new Uri("https://localhost:44326/");  
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpClient;
        }        

        private async Task<string> RenewTokens()
        {
            //get the current HTTpContext to access tokens
            var currentContext = _httpContextAccessor.HttpContext;

            var discoveryClient = new DiscoveryClient("https://localhost:44337");
            var metaDataResponse = await discoveryClient.GetAsync();

            var tokenClient = new TokenClient(metaDataResponse.TokenEndpoint, "imagegalleryclient", "secret");

            var currentRefreshToken = await currentContext
                .GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            //Request a toekn using refresh token
            var tokenResult = await tokenClient.RequestRefreshTokenAsync(currentRefreshToken);

            if(!tokenResult.IsError)
            {
                //update the tokens & expiration value
                var updatedTokens = new List<AuthenticationToken>();
                updatedTokens.Add(new AuthenticationToken
                {
                    Name=OpenIdConnectParameterNames.IdToken,
                    Value = tokenResult.IdentityToken
                });

                updatedTokens.Add(new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = tokenResult.AccessToken
                });

                updatedTokens.Add(new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = tokenResult.RefreshToken
                });

                var expiresat = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
                updatedTokens.Add(new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expiresat.ToString("o",CultureInfo.InvariantCulture)
                });

                var currentAuthenticateResult = await currentContext.AuthenticateAsync("Cookies");
                currentAuthenticateResult.Properties.StoreTokens(updatedTokens);

                //sign in
                await currentContext.SignInAsync("Cookies", currentAuthenticateResult.Principal,
                                    currentAuthenticateResult.Properties);

                return tokenResult.AccessToken;

            }
            else
            {
                throw new Exception("Problem encountered while refreshing tokens.", tokenResult.Exception);
            }
        }
    }
}

