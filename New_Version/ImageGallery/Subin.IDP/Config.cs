using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Subin.IDP
{
    public static class Config
    {
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>()
            {
                new TestUser
                {
                    SubjectId = "b7539694-97e7-4dfe-84da-b4256e1ff5c7",
                    Username="Frank",
                    Password="password",
                    Claims = new List<Claim>
                    {
                        new Claim("given_name","Frank"),
                        new Claim("family_name","Underwood"),
                        new Claim("address","TP Para"),
                        new Claim("role","FreeUser"),
                        new Claim("country","nl"),
                        new Claim("subscriptionlevel","FreeUser")
                    }
                },
                new TestUser
                {
                    SubjectId = "d860efca-22d9-47fd-8249-791ba61b07c7",
                    Username="Claire",
                    Password="password",
                    Claims = new List<Claim>
                    {
                        new Claim("given_name","Claire"),
                        new Claim("family_name","Underwood"),
                        new Claim("address","TP Para"),
                        new Claim("role","PayingUser"),
                        new Claim("country","be"),
                        new Claim("subscriptionlevel","PayingUser")
                    }
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address(),
                new IdentityResource(
                    "roles",
                    "Your Roles",
                    new List<string>() { "role" }),
                new IdentityResource(
                    "country",
                    "The country you are living in",
                    new List<string>() { "country" }),
                new IdentityResource(
                    "subscriptionlevel",
                    "Your subscription level",
                    new List<string>() { "subscriptionlevel" })
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("imagegalleryapi","Image Gallery API",new List<string>() { "role" })
                {
                     ApiSecrets = { new Secret("apisecret".Sha256()) }
                }
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>()
            {
                new Client
                {
                    ClientName="Image Gallery One more time",
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Hybrid,
                    AllowOfflineAccess = true,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AccessTokenLifetime = 120,
                    AccessTokenType = AccessTokenType.Reference,
                    RedirectUris = new List<string>()
                    {
                        "https://localhost:44381/signin-oidc"
                    },
                    PostLogoutRedirectUris = new List<string>()
                    {
                        "https://localhost:44381/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Address,
                        "roles",
                        "imagegalleryapi",
                        "country",
                        "subscriptionlevel"
                    },
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    }
                }
            };
        }
    }
}
