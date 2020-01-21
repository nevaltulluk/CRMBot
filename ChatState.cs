using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Bot.Connector;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class ChatState
    {
        public Dictionary<string, object> Data = new Dictionary<string, object>();
        private static double chatCacheDurationMinutes = 30.0000;
        private string channelId = string.Empty;
        private string userId = string.Empty;
        private object metadataLock = new object();
        public static string Attachments = "Attachments";
        public static string FilteredEntities = "FilteredEntities";
        public static string SelectedEntity = "SelectedEntity";
        public static string CurrentPageIndex = "CurrentPageIndex";

        protected ChatState(string channelId, string userId)
        {
            this.channelId = channelId;
            this.userId = userId;
        }
        public static bool IsChatStateSet(Activity message)
        {
            if (!MemoryCache.Default.Contains(message.Conversation.Id))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static void ClearChatState(string channelId, string userId)
        {
            if (MemoryCache.Default.Contains(channelId + userId))
            {
                MemoryCache.Default.Remove(channelId + userId);
            }
        }
        public static ChatState RetrieveChatState(string channelId, string userId)
        {
            if (!MemoryCache.Default.Contains(channelId + userId))
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.Priority = CacheItemPriority.Default;
                policy.SlidingExpiration = TimeSpan.FromMinutes(chatCacheDurationMinutes);
                ChatState state = new ChatState(channelId, userId);
                InitializeState(state);
                MemoryCache.Default.Add(channelId + userId, state, policy);
            }
            return MemoryCache.Default[channelId + userId] as ChatState;
        }

        private static string InitializeState(ChatState state)
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["CrmUserName"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CrmPassword"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CrmServiceUrl"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CrmAuthority"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CrmClientId"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CrmClientSecret"]))
            {
                try
                {
                    AuthenticationContext authContext =
                        new AuthenticationContext(ConfigurationManager.AppSettings["CrmAuthority"].ToString(), false);

                    //No prompt for credentials
                    var credentials = new UserPasswordCredential(ConfigurationManager.AppSettings["CrmUserName"].ToString(), ConfigurationManager.AppSettings["CrmPassword"].ToString());
                    var authResult = authContext.AcquireTokenAsync(ConfigurationManager.AppSettings["CrmServiceUrl"].ToString(), ConfigurationManager.AppSettings["CrmClientId"].ToString(), credentials).Result;

                    state.AccessToken = authResult.AccessToken;
                    state.OrganizationUrl = ConfigurationManager.AppSettings["CrmServiceUrl"].ToString();
                }
                catch (Exception ex)
                {
                    string message = ex.ToString();
                }
            }
            return string.Empty;
        }

        public string AccessToken
        {
            get; set;
        }

        public string OrganizationUrl
        {
            get; set;
        }
        public void Set(string key, object data)
        {
            Data[key] = data;
        }
        public object Get(string key)
        {
            if (Data.ContainsKey(key))
            {
                return Data[key];
            }
            return null;
        }
    }
}