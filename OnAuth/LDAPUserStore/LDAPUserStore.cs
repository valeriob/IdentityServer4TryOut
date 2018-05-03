using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OnAuth.LDAP
{
    public partial class LdapUserStore
    {
        string _queryUsername;
        string _queryPassword;
        string _serverAddress;

        public LdapUserStore()
        {
            Init();
        }

        partial void Init();




        public bool ValidateCredentials(string username, string password)
        {
            using (var connection = new LdapConnection(_serverAddress))
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
            if (!string.IsNullOrWhiteSpace(_queryUsername) && !string.IsNullOrWhiteSpace(_queryPassword))
                entry = new DirectoryEntry(serverAddress, _queryUsername, _queryPassword);
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
            var entry = CreateEntry(_serverAddress);

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
        LdapUserStore _ldapUserStore;

        public ProfileService(LdapUserStore ldapUserStore)
        {
            _ldapUserStore = ldapUserStore;
        }


        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            throw new NotImplementedException();
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            throw new NotImplementedException();
        }
    }

    class CustomResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        LdapUserStore _ldapUserStore;

        public CustomResourceOwnerPasswordValidator(LdapUserStore ldapUserStore)
        {
            _ldapUserStore = ldapUserStore;
        }

        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            throw new NotImplementedException();
        }
    }
}
