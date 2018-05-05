using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OnAuth.LDAPUserStore
{
    public partial class LDAPUserStore
    {
        IOptions<LDAPUserStoreOptions> _configuration;

        public LDAPUserStore(IOptions<LDAPUserStoreOptions> configuration)
        {
            _configuration = configuration;
        }


        public bool ValidateCredentials(string username, string password)
        {
            using (var connection = new LdapConnection(_configuration.Value.ServerAddress))
            {
                var credential = new NetworkCredential(username, password);
                connection.Credential = credential;
                try
                {
                    connection.Bind();
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }

        DirectoryEntry CreateEntry(string serverAddress)
        {
            serverAddress = "LDAP://" + serverAddress;
            DirectoryEntry entry;
            if (!string.IsNullOrWhiteSpace(_configuration.Value.Username) && !string.IsNullOrWhiteSpace(_configuration.Value.Password))
                entry = new DirectoryEntry(serverAddress, _configuration.Value.Username, _configuration.Value.Password);
            else
                entry = new DirectoryEntry(serverAddress);
            return entry;
        }

        string DisplayName(DirectoryEntry directoryObject, string username)
        {
            var displayName = string.Empty;
            if (directoryObject.Properties.Contains(PropertyNames.name))
            {
                displayName = directoryObject.Properties[PropertyNames.name].Value.ToString();
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                if (!directoryObject.Properties.Contains(PropertyNames.cn))
                {
                    throw new Exception($"GetUser, username {username} !directoryObject.Properties.Contains(PropertyNames.cn)");
                }
                displayName = directoryObject.Properties[PropertyNames.cn].Value.ToString();
            }
            return displayName;
        }

        string LdapUsername(DirectoryEntry directoryObject, string username)
        {
            var ldapUsername = string.Empty;
            //if (directoryObject.Properties.Contains(PropertyNames.userPrincipalName))
            //{
            //    userName = directoryObject.Properties[PropertyNames.userPrincipalName].Value.ToString();
            //}
            if (string.IsNullOrWhiteSpace(ldapUsername))
            {
                if (!directoryObject.Properties.Contains(PropertyNames.sAMAccountName))
                {
                    throw new Exception($"GetUser, username {username} !directoryObject.Properties.Contains(PropertyNames.sAMAccountName)");
                }
                ldapUsername = directoryObject.Properties[PropertyNames.sAMAccountName].Value.ToString();
            }
            return ldapUsername;
        }


        public LdapUser FindByUsername(string username)
        {
            var entry = CreateEntry(_configuration.Value.ServerAddress);

            using (var mySearcher = new DirectorySearcher(entry))
            {
                mySearcher.SearchScope = System.DirectoryServices.SearchScope.Subtree;
                mySearcher.Filter = "(&(objectClass=user)(|(cn=" + username + ")(sAMAccountName=" + username + ")))";

                var result = mySearcher.FindOne();
                var directoryObject = result.GetDirectoryEntry();

                var displayName = DisplayName(directoryObject, username);
                var ldapUsername = LdapUsername(directoryObject, username);

                return new LdapUser
                {
                    SubjectId = directoryObject.Guid.ToString(),
                    Username = username,
                };
            }

        }

        public LdapUser FindBySubject(string subject)
        {
            var entry = CreateEntry(_configuration.Value.ServerAddress);

            using (var mySearcher = new DirectorySearcher(entry))
            {
                mySearcher.SearchScope = System.DirectoryServices.SearchScope.Subtree;
                //mySearcher.Filter = "(&(objectClass=user)(|(cn=" + username + ")(sAMAccountName=" + username + ")))";
                string queryGuid = Guid2OctetString(subject);
                mySearcher.Filter = "(&(objectClass=user)(objectGUID=" + queryGuid + "))";

                var result = mySearcher.FindOne();
                var directoryObject = result.GetDirectoryEntry();

                var ldapUsername = LdapUsername(directoryObject, subject);

                return new LdapUser
                {
                    SubjectId = directoryObject.Guid.ToString(),
                    Username = ldapUsername,
                };
            }

        }



        static string Guid2OctetString(string objectGuid)
        {
            var guid = new Guid(objectGuid);
            byte[] byteGuid = guid.ToByteArray();
            string queryGuid = "";
            foreach (byte b in byteGuid)
            {
                queryGuid += @"\" + b.ToString("x2");
            }
            return queryGuid;
        }


        public LdapUser FindByExternalProvider(string provider, string providerUserId)
        {
            throw new NotImplementedException();
        }

        public LdapUser AutoProvisionUser(string provider, string providerUserId, List<Claim> list)
        {
            throw new NotImplementedException();
        }
    }

    public static class PropertyNames
    {
        public static string distinguishedName = "distinguishedName";
        //public static string ObjectGUID = "ObjectGUID";
        public static string sAMAccountName = "sAMAccountName";
        public static string cn = "cn";
        public static string userPrincipalName = "userPrincipalName";
        public static string mail = "mail";
        public static string name = "name";

    }

    public class LdapUser
    {
        public string Username { get; set; }
        public string SubjectId { get; set; }
    }

    class ProfileService : IProfileService
    {
        LDAPUserStore _ldapUserStore;

        public ProfileService(LDAPUserStore ldapUserStore)
        {
            _ldapUserStore = ldapUserStore;
        }


        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subject = context.Subject.Claims.Where(c => c.Type == "sub").Select(r => r.Value).FirstOrDefault();
            var user = _ldapUserStore.FindBySubject(subject);

            context.AddRequestedClaims(new[] { new Claim("preferred_username", user.Username), new Claim("role", "Administrator") });
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.CompletedTask;
            // throw new NotImplementedException();
        }
    }

    class CustomResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        LDAPUserStore _ldapUserStore;

        public CustomResourceOwnerPasswordValidator(LDAPUserStore ldapUserStore)
        {
            _ldapUserStore = ldapUserStore;
        }

        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var valid = _ldapUserStore.ValidateCredentials(context.UserName, context.Password);

            if (valid)
            {
                var user = _ldapUserStore.FindByUsername(context.UserName);
                context.Result = new GrantValidationResult(user.SubjectId, "Password");
            }
            else
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest);
            }

            return Task.CompletedTask;
        }
    }
}
