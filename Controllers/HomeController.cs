using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login(string channelId, string userId, string userName, string fromId, string fromName, string serviceUrl, string conversationId, string extraQueryParams)
        {
            // CRM Url
            Session["fromId"] = fromId;
            Session["fromName"] = fromName;
            Session["serviceUrl"] = serviceUrl;
            Session["conversationId"] = conversationId;
            Session["channelId"] = channelId;
            Session["userId"] = userId;
            Session["userName"] = userName;
            ChatState state = ChatState.RetrieveChatState(Session["channelId"].ToString(), Session["userId"].ToString());

            AuthenticationContext authContext = new AuthenticationContext(ConfigurationManager.AppSettings["CrmAuthority"]);
            var authUri = authContext.GetAuthorizationRequestUrlAsync(state.OrganizationUrl, ConfigurationManager.AppSettings["CrmClientId"],
            new Uri(ConfigurationManager.AppSettings["CrmRedirectUrl"]), UserIdentifier.AnyUser, extraQueryParams);
            return Redirect(authUri.Result.ToString());
        }

        public ActionResult Authorize(string code)
        {
            AuthenticationContext authContext = new AuthenticationContext(ConfigurationManager.AppSettings["CrmAuthority"]);
            var authResult = authContext.AcquireTokenByAuthorizationCodeAsync(
            code, new Uri(ConfigurationManager.AppSettings["CrmRedirectUrl"]),
            new ClientCredential(ConfigurationManager.AppSettings["CrmClientId"],
            ConfigurationManager.AppSettings["CrmClientSecret"]));

            // Saving token in Bot State
            var botCredentials = new MicrosoftAppCredentials(ConfigurationManager.AppSettings["MicrosoftAppId"],
            ConfigurationManager.AppSettings["MicrosoftAppPassword"]);
            ChatState state = ChatState.RetrieveChatState(Session["channelId"].ToString(), Session["userId"].ToString());
            state.AccessToken = authResult.Result.AccessToken;

            ViewBag.Message = $"Your Token - {authResult.Result.AccessToken} Channel Id - {Session["channelId"].ToString()} User Id - {Session["userId"].ToString()}";

            // Use the data stored previously to create the required objects.
            var userAccount = new ChannelAccount(Session["userId"].ToString(), Session["userName"].ToString());
            var botAccount = new ChannelAccount(Session["fromId"].ToString(), Session["fromName"].ToString());
            var connector = new ConnectorClient(new Uri(Session["serviceUrl"].ToString()));

            string conversationId;
            // Create a new message.
            IMessageActivity message = Activity.CreateMessageActivity();
            if (!string.IsNullOrEmpty(Session["conversationId"].ToString()) && !string.IsNullOrEmpty(Session["channelId"].ToString()))
            {
                // If conversation ID and channel ID was stored previously, use it.
                message.ChannelId = Session["channelId"].ToString();
                conversationId = Session["conversationId"].ToString();
            }
            else
            {
                // Conversation ID was not stored previously, so create a conversation. 
                // Note: If the user has an existing conversation in a channel, this will likely create a new conversation window.
                conversationId = connector.Conversations.CreateDirectConversation(botAccount, userAccount).Id;
            }

            // Set the address-related properties in the message and send the message.
            message.From = botAccount;
            message.Recipient = userAccount;
            message.Conversation = new ConversationAccount(id: conversationId);
            message.Text = $"Thanks! You're logged in now. Try saying 'How many contacts were created this week?'";
            message.Locale = "en-us";
            connector.Conversations.SendToConversation((Activity)message);

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}