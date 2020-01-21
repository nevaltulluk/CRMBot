using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public static class CrmFunctions
    {
        private static string _clientId = ConfigurationManager.AppSettings["CrmClientId"];
        // Azure Application REPLY URL - can be anything here but it must be registered ahead of time
        private static string _redirectUrl = ConfigurationManager.AppSettings["CrmRedirectUrl"];
        //Azure Directory OAUTH 2.0 AUTHORIZATION ENDPOINT
        private static string _authority = ConfigurationManager.AppSettings["CrmAuthority"];

        public static HttpClient CreateClient(string channelId, string userId)
        {
            ChatState state = ChatState.RetrieveChatState(channelId, userId);
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(state.OrganizationUrl);
            httpClient.Timeout = new TimeSpan(0, 2, 0);
            httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", state.AccessToken);
            return httpClient;
        }

        public static async Task Create(string entityCollectionName, JObject entity, string channelId, string userId)
        {
            using (HttpClient httpClient = CreateClient(channelId, userId))
            {
                //Unbound Function
                //The URL will change in 2016 to include the API version - api/data/v8.0/v8.0/accounts
                HttpResponseMessage createResponse =
                    await httpClient.PostAsJsonAsync($"api/data/v8.1/{entityCollectionName}", entity);
                Guid accountId = new Guid();
                if (createResponse.IsSuccessStatusCode)
                {
                    string accountUri = createResponse.Headers.GetValues("OData-EntityId").FirstOrDefault();
                    if (accountUri != null)
                        accountId = Guid.Parse(accountUri.Split('(', ')')[1]);
                }
                else
                    return;
            }
        }


        public static string ParseCrmUrl(Activity message)
        {
            if (message.From.Properties.ContainsKey("crmUrl"))
            {
                return message.From.Properties["crmUrl"].ToString();
            }
            else
            {
                var regex = new Regex("<a [^>]*href=(?:'(?<href>.*?)')|(?:\"(?<href>.*?)\")", RegexOptions.IgnoreCase);
                var urls = regex.Matches(message.Text).OfType<Match>().Select(m => m.Groups["href"].Value).ToList();
                if (urls.Count > 0)
                {
                    return urls[0];
                }
                else if (message.Text.ToLower().StartsWith("http") && message.Text.ToLower().Contains(".dynamics.com"))
                {
                    return message.Text;
                }
            }
            return string.Empty;

        }

        public static async Task RetrieveMultiple(IDialogContext context, string entityCollectionName, string query, string[] properties, string channelId, string userId)
        {
            StringBuilder sb = new StringBuilder();
            using (HttpClient httpClient = CreateClient(channelId, userId))
            {
                HttpResponseMessage retrieveResponse =
                    await httpClient.GetAsync($"api/data/v8.0/{entityCollectionName}?$filter={query}");
                if (retrieveResponse.IsSuccessStatusCode)
                {
                    JObject collection = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(retrieveResponse.Content.ReadAsStringAsync().Result);
                    DisplayFormattedEntities(context, sb, entityCollectionName,
                        collection, properties);
                }
            }
            await context.PostAsync(sb.ToString());
        }

        /// <summary> Displays formatted entity collections to the console. </summary>
        /// <param name="label">Descriptive text output before collection contents </param>
        /// <param name="collection"> JObject containing array of entities to output by property </param>
        /// <param name="properties"> Array of properties within each entity to output. </param>
        private static void DisplayFormattedEntities(IDialogContext context, StringBuilder sb, string entityName, JArray entities, string[] properties)
        {
            sb.Append($"I found {entities.Count} {entityName} that match");
            int lineNum = 0;
            List<string> selectedEntities = new List<string>();
            foreach (JObject entity in entities)
            {
                lineNum++;
                List<string> propsOutput = new List<string>();
                //Iterate through each requested property and output either formatted value if one 
                //exists, otherwise output plain value.
                foreach (string prop in properties)
                {
                    string propValue;
                    string formattedProp = prop + "@OData.Community.Display.V1.FormattedValue";
                    if (null != entity[formattedProp])
                    { propValue = entity[formattedProp].ToString(); }
                    else
                    { propValue = entity[prop].ToString(); }
                    propsOutput.Add(propValue);
                }
                string text = string.Format("\n{0}. {1}", lineNum, String.Join(", ", propsOutput));
                selectedEntities.Add(String.Join(", ", propsOutput));
                sb.Append(text);
            }
            context.ConversationData.SetValue("SelectedEntities", selectedEntities);
            sb.Append("\n");            
        }
        ///<summary>Overloaded helper version of method that unpacks 'collection' parameter.</summary>
        private static void DisplayFormattedEntities(IDialogContext context, StringBuilder sb, string entityName, JObject collection, string[] properties)
        {
            JToken valArray;
            //Parameter collection contains an array of entities in 'value' member.
            if (collection.TryGetValue("value", out valArray))
            {
                DisplayFormattedEntities(context, sb, entityName, (JArray)valArray, properties);
            }
            //Otherwise it just represents a single entity.
            else
            {
                JArray singleton = new JArray(collection);
                DisplayFormattedEntities(context, sb, entityName, singleton, properties);
            }
        }

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public static List<DateTime> ParseDateTimes(this EntityRecommendation dateEntity)
        {
            List<DateTime> ret = new List<DateTime>();
            foreach (var vals in dateEntity.Resolution.Values)
            {
                Dictionary<string, object> values = (Dictionary<string, object>)((List<object>)vals)[0];
                if (values["type"].ToString() == "daterange")
                {
                    DateTime start;
                    DateTime end;

                    if (values.ContainsKey("start") && DateTime.TryParse(values["start"].ToString(), out start))
                    {
                        ret.Add(start);
                    }
                    if (values.ContainsKey("end") && DateTime.TryParse(values["end"].ToString(), out end))
                    {
                        ret.Add(end);
                    }
                }
            }
            return ret;
        }

        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear, int daysToAdd)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3).AddDays(daysToAdd);
        }

    }
}