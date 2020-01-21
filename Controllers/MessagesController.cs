using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Web;
using System.Configuration;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity message)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
            try
            {

                // check if activity is of type message
                if (message != null && message.GetActivityType() == ActivityTypes.Message)
                {
                    ChatState state = ChatState.RetrieveChatState(message.ChannelId, message.From.Id);

                    if (string.IsNullOrEmpty(state.OrganizationUrl) && CrmFunctions.ParseCrmUrl(message) == string.Empty)
                    {
                        await connector.Conversations.ReplyToActivityAsync(message.CreateReply("Hi there, before we can work together you need to tell me your Dynamics 365 URL (e.g. https://contoso.crm.dynamics.com)"));
                    }
                    else if (string.IsNullOrEmpty(state.AccessToken) || CrmFunctions.ParseCrmUrl(message) != string.Empty)
                    {
                        string extraQueryParams = string.Empty;

                        string crmUrl = CrmFunctions.ParseCrmUrl(message);

                        if (crmUrl != string.Empty && state.OrganizationUrl != crmUrl)
                        {
                            if (!string.IsNullOrEmpty(state.OrganizationUrl) && state.OrganizationUrl != crmUrl)
                            {
                                extraQueryParams = "prompt=login";
                            }
                            state.OrganizationUrl = crmUrl;
                        }

                        Activity replyToConversation = message.CreateReply();
                        replyToConversation.Recipient = message.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();

                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction plButton = new CardAction()
                        {
                            // ASP.NET Web Application Hosted in Azure
                            // Pass the user id
                            Value = $"{ConfigurationManager.AppSettings["BotAuthUrl"]}?channelId={HttpUtility.UrlEncode(message.ChannelId)}&userId={HttpUtility.UrlEncode(message.From.Id)}&userName={HttpUtility.UrlEncode(message.From.Name)}&fromId={HttpUtility.UrlEncode(message.Recipient.Id)}&fromName={HttpUtility.UrlEncode(message.Recipient.Name)}&serviceUrl={HttpUtility.UrlEncode(message.ServiceUrl)}&conversationId={HttpUtility.UrlEncode(message.Conversation.Id)}&extraQueryParams={extraQueryParams}",
                            Type = "signin",
                            Title = "Connect"
                        };

                        cardButtons.Add(plButton);

                        SigninCard plCard = new SigninCard("Click connect to signin to Dynamics 365 (" + state.OrganizationUrl + ").", new List<CardAction>() { plButton });
                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                        await connector.Conversations.SendToConversationAsync(replyToConversation);
                    }
                    else
                    {
                        await Conversation.SendAsync(message, () => new CrmLuisDialog());
                    }
                }
                else
                {
                    HandleSystemMessage(message);
                }
            }
            catch (Exception ex)
            {
                await connector.Conversations.ReplyToActivityAsync(message.CreateReply($"Kabloooey! Well played human you just fried my circuits. Thanks for being patient, I'm still learning to do some things while in preview. Hopefully, I'll get this worked out soon. Here's your prize: {ex.Message}"));
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}