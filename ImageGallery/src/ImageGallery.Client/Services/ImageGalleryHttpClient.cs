using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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
        
        public async Task<HttpClient> GetClient()
        {
            string accessToken = string.Empty;
            
            //Get the current HttpContext to access the token
            var currentContext = _httpContextAccessor.HttpContext;

            //Should we renew access & refresh tokens?
            //get expired_at value
            var expires_at = await currentContext.GetTokenAsync("expires_at");

            //compare in UTC
            if(string.IsNullOrWhiteSpace(expires_at)
                || ((DateTime.Parse(expires_at).AddSeconds(-60)).ToUniversalTime() < DateTime.UtcNow))
            {
                accessToken = await RenewTokens();
            }
            else
            {
                //get access tokens
                accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            }

            //Get Access Tokens
            //accessToken = await currentContext.GetTokenAsync(
                //OpenIdConnectParameterNames.AccessToken);

            if(!string.IsNullOrWhiteSpace(accessToken))
            {
                //set as bearer token
                _httpClient.SetBearerToken(accessToken);
            }

            _httpClient.BaseAddress = new Uri("https://localhost:44387/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpClient;
        }
        

        private async Task<string> RenewTokens()
        {
            //get the current httpcontext to access tokens
            var currentContext = _httpContextAccessor.HttpContext;

            //get the metadata
            var discoveryClient = new DiscoveryClient("https://localhost:44313/");
            var metaDataResponse = await discoveryClient.GetAsync();

            //create a new token client to get new tokens
            var tokenClient = new TokenClient(metaDataResponse.TokenEndpoint, "imagegalleryclient", "secret");

            //get the saved refresh token
            var currentRefreshToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            //refresh the tokens
            var tokenResult = await tokenClient.RequestRefreshTokenAsync(currentRefreshToken);

            if(!tokenResult.IsError)
            {
                //update the tokens & expiration value
                var updatedTokens = new List<AuthenticationToken>();
                updatedTokens.Add(new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.IdToken,
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

                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
                updatedTokens.Add(new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expiresAt.ToString("o",CultureInfo.InvariantCulture)
                });

                //get authenticate result, containing the current principal & properties
                var currentAuthenticateResult = await currentContext.AuthenticateAsync("Cookies");

                //store the updated tokens
                currentAuthenticateResult.Properties.StoreTokens(updatedTokens);

                //sign in
                await currentContext.SignInAsync("Cookies", currentAuthenticateResult.Principal,
                    currentAuthenticateResult.Properties);

                //return the new access tokens
                return tokenResult.AccessToken;
            }
            else
            {
                throw new Exception("Problem Encountered while refreshing tokens.", tokenResult.Exception);
            }
        }
    }
}

