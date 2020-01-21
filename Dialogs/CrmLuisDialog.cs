using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    //[LuisModel("cc421661-4803-4359-b19b-35a8bae3b466", "70c9f99320804782866c3eba387d54bf")]
    [LuisModel("64c400cf-b36d-4874-bd01-1c7567e57d8a", "a03f8796d25a493dac9ff9e8ad2b15a6")]
    [Serializable]
    public class CrmLuisDialog : LuisDialog<object>
    {
        public const string Entity_Date = "builtin.datetimeV2.daterange";
        public const string Entity_Entity_Type = "EntityType";
        Random r = new Random();
        public CrmLuisDialog()
        {
        }
        public CrmLuisDialog(ILuisService service)
            : base(service)
        {
        }

        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            int index;
            List<string> selectedEntities;

            if (Int32.TryParse(result.Query, out index) && context.ConversationData.TryGetValue<List<string>>("SelectedEntities", out selectedEntities))
            {
                var message = context.MakeMessage();
                message.Attachments = new List<Attachment>();
                int rInt = 0;
                if(!context.ConversationData.TryGetValue<int>("ImageIndex", out rInt) || rInt >= HttpClientExtensions.Images.Length)
                {
                    rInt = 0;
                }
                message.Attachments.Add(new ThumbnailCard
                {
                    Title = selectedEntities[index - 1],
                    Subtitle = $"Bot likeness...",
                    Text = HttpClientExtensions.Images[rInt][0],
                    Images = new List<CardImage> { new CardImage(HttpClientExtensions.Images[rInt][1]) },
                }.ToAttachment());
                context.ConversationData.SetValue("ImageIndex", rInt + 1);
                //string message = $"Got it. You've selected {selectedEntities[index - 1]}";
                await context.PostAsync(message);
                context.Wait(MessageReceived);
            }
            else
            {
                if (result.Query.ToLower().Contains("thank"))
                {
                    string message = $"You're welcome!";
                    await context.PostAsync(message);
                    context.Wait(MessageReceived);
                }
                else
                {
                    string message = $"Sorry I did not understand: " + string.Join(", ", result.Intents.Select(i => i.Intent));
                    await context.PostAsync(message);
                    context.Wait(MessageReceived);
                }
            }
        }
        [LuisIntent("Locate")]
        public async Task Locate(IDialogContext context, LuisResult result)
        {
            EntityRecommendation entityType;
            EntityRecommendation date;
            string entityTypeString = string.Empty;
            string dateString = string.Empty;
            if (result.TryFindEntity(Entity_Entity_Type, out entityType))
            {
                entityTypeString = entityType.Entity;
            }
            if (result.TryFindEntity(Entity_Date, out date))
            {
                List<DateTime> dates = date.ParseDateTimes();
                if (dates != null && dates.Count > 0)
                {
                    dateString = dates[0].ToString();
                }
            }
            if (!string.IsNullOrEmpty(entityTypeString) && !string.IsNullOrEmpty(dateString))
            {
                //Microsoft.Dynamics.CRM.Between(PropertyName='birthdate',PropertyValues=["1990-01-01","1990-01-01"])
                await CrmFunctions.RetrieveMultiple(context, entityTypeString, $"Microsoft.Dynamics.CRM.Between(PropertyName='createdon',PropertyValues=[\"{dateString}\",\"{DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")}\"])", new string[] { "fullname" }, context.Activity.ChannelId, context.Activity.From.Id);
            }
            context.Wait(MessageReceived);
        }
        [Serializable]
        public sealed class Entity : IEquatable<Entity>
        {
            public string DisplayName { get; set; }
            public string LogicalName { get; set; }
            public Guid Id { get; set; }
            public override string ToString()
            {
                return this.DisplayName;
            }
            public bool Equals(Entity other)
            {
                return other != null
                    && this.Id == other.Id
                    && this.LogicalName == other.LogicalName;
            }
            public override bool Equals(object other)
            {
                return Equals(other as Entity);
            }
            public override int GetHashCode()
            {
                return this.Id.GetHashCode();
            }
        }
    }
}