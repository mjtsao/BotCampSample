﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using BotCampDemo.Model;
using Microsoft.Bot.Connector;
using Microsoft.ProjectOxford.Vision;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotCampDemo
{
	[BotAuthentication]
	public class MessagesController : ApiController
	{
		/// <summary>
		/// POST: api/Messages
		/// Receive a message from a user and reply to it
		/// </summary>
		public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
		{
			if (activity.Type == ActivityTypes.Message)
			{
				//Trace.TraceInformation(JsonConvert.SerializeObject(activity, Formatting.Indented));
				ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
				Activity reply = activity.CreateReply();

				if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image/"))
				{
					ImageTemplate(reply, activity.Attachments.First().ContentUrl);
				}
				else
				{
					var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
					if (fbData.postback != null)
					{
						if (fbData.postback.payload.StartsWith("Face>"))
						{
							//faceAPI
						}
						else if (fbData.postback.payload.StartsWith("Analyze>"))
						{
							//辨識圖片
							string url = fbData.postback.payload.Split('>')[1];
							VisionServiceClient client = new VisionServiceClient("");
							var result = await client.AnalyzeImageAsync(url, new VisualFeature[] { VisualFeature.Description });
							reply.Text = result.Description.Captions.First().Text;
						}
					}
					else
						reply.Text = activity.From.Id;
				}
				
				await connector.Conversations.ReplyToActivityAsync(reply);
			}
			else
			{
				HandleSystemMessage(activity);
			}
			var response = Request.CreateResponse(HttpStatusCode.OK);
			return response;
		}

		private void TemplateByChannelData(Activity reply)
		{
			reply.ChannelData = JObject.FromObject(new
			{
				attachment = new
				{
					type = "template",
					payload = new
					{
						template_type = "generic",
						elements = new List<object>()
						{
							new
							{
								title = "iPad Pro",
								image_url = "https://s.yimg.com/wb/images/936392DB6B69D9C6D1B897B8DAB20AE595E96FA4",
								buttons = new List<object>()
								{
									new
									{
										type = "web_url",
										title = "YAHOO購物中心url",
										url = "https://tw.buy.yahoo.com/gdsale/MM172-6798747.html",
										webview_height_ratio = "tall"
									}
								}
							},
							new
							{
								title = "Surface Pro",
								image_url = "https://s.yimg.com/wb/images/268917ABD27238C9A20428002A8143AEEF40A048",
								buttons = new List<object>()
								{
									new
									{
										type = "web_url",
										title = "YAHOO購物中心url",
										url = "https://tw.buy.yahoo.com/gdsale/gdsale.asp?act=gdsearch&gdid=6561885",
										webview_height_ratio = "tall"
									}
								}
							}
						}
					}
				}
			});
		}

		private void TemplateBySDK(Activity reply)
		{
			List<Attachment> att = new List<Attachment>();
			att.Add(new HeroCard()
			{
				Title = "iPad Pro",
				Images = new List<CardImage>() { new CardImage("https://s.yimg.com/wb/images/936392DB6B69D9C6D1B897B8DAB20AE595E96FA4") },
				Buttons = new List<CardAction>()
						{
							new CardAction(ActionTypes.OpenUrl, "Yahoo購物中心", value: $"https://tw.buy.yahoo.com/gdsale/MM172-6798747.html")
						}
			}.ToAttachment());
			att.Add(new HeroCard()
			{
				Title = "Surface Pro",
				Images = new List<CardImage>() { new CardImage("https://s.yimg.com/wb/images/268917ABD27238C9A20428002A8143AEEF40A048") },
				Buttons = new List<CardAction>()
						{
							new CardAction(ActionTypes.OpenUrl, "Yahoo購物中心", value: $"https://tw.buy.yahoo.com/gdsale/gdsale.asp?act=gdsearch&gdid=6561885")
						}
			}.ToAttachment());
			reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
			reply.Attachments = att;
		}

		private void ImageTemplate(Activity reply, string url)
		{
			var element = new List<object>()
			{
				new
				{
					title = "Cognitive services?",
					subtitle = "Select from below",
					image_url = url,
					buttons = new List<object>()
					{
						new
						{
							type = "postback",
							title = "男女生",
							payload = $"Face>{url}"
						},
						new
						{
							type = "postback",
							title = "辨識圖片",
							payload= $"Analyze>{url}"
						}
					}
				}
			};

			reply.ChannelData = JObject.FromObject(new
			{
				attachment = new
				{
					type = "template",
					payload = new
					{
						template_type = "generic",
						elements = element
					}
				}
			});
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