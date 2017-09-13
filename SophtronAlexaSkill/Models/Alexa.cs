using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SophtronAlexaSkill.Models
{
    public class Alexa
    {
        public string version = "1.0";
        public const string launchType = "LaunchRequest";
        public const string intentType = "IntentRequest";

        #region Response

        public class StandardResponse
        {
            public string version = "1.0";
            public Dictionary<string, object> sessionAttributes = new Dictionary<string, object>();
            public Response response = new Response();
        }

        public class Response
        {
            public Outputspeech outputSpeech = new Outputspeech();
            public Card card { get; set; }
            public Reprompt reprompt { get; set; }
            public Directive[] directives { get; set; }
            public bool shouldEndSession = false;
        }

        public class Outputspeech
        {
            public string type = "PlainText";
            public string text { get; set; }
            public string ssml { get; set; }
        }

        public class Card
        {
            public string type = "Simple";
            public string title { get; set; }
            public string content { get; set; }
            public string text { get; set; }
            public Image image { get; set; }
        }

        public class Image
        {
            public string smallImageUrl { get; set; }
            public string largeImageUrl { get; set; }
        }

        public class Reprompt
        {
            public Outputspeech outputSpeech { get; set; }
        }

        public class Directive
        {
            public string type { get; set; }
            public Template template { get; set; }
            public string playBehavior { get; set; }
            public Audioitem audioItem { get; set; }
            public General general { get; set; }
        }

        public class Template
        {
            public string type { get; set; }
        }

        public class Audioitem
        {
            public Stream stream { get; set; }
        }

        public class Stream
        {
            public string token { get; set; }
            public string url { get; set; }
            public int offsetInMilliseconds { get; set; }
        }

        public class General
        {
            public string type { get; set; }
            public Videoitem videoItem { get; set; }
        }

        public class Videoitem
        {
            public string source { get; set; }
            public Metadata metadata { get; set; }
        }

        public class Metadata
        {
            public string title { get; set; }
            public string subtitle { get; set; }
        }
        #endregion

        #region Request

        public class StandardRequest
        {
            public string version { get; set; }
            public Session session { get; set; }
            public Context context { get; set; }
            public Request request { get; set; }
        }

        public class StandardIntentRequest
        {
            public string version { get; set; }
            public Session session { get; set; }
            public Context context { get; set; }
            public IntentRequest request { get; set; }
        }

        public class Session
        {
            public bool _new { get; set; }
            public string sessionId { get; set; }
            public Application application { get; set; }
            public Dictionary<string, object> attributes { get; set; }
            public User user { get; set; }
        }

        public class Application
        {
            public string applicationId { get; set; }
        }

        public class User
        {
            public string userId { get; set; }
            public Permissions permissions { get; set; }
            public string accessToken { get; set; }
        }

        public class Permissions
        {
            public string consentToken { get; set; }
        }

        public class Context
        {
            public System System { get; set; }
            public Audioplayer AudioPlayer { get; set; }
        }

        public class System
        {
            public Application application { get; set; }
            public User user { get; set; }
            public Device device { get; set; }
            public string apiEndpoint { get; set; }
        }

        public class Device
        {
            public string deviceId { get; set; }
            public Supportedinterfaces supportedInterfaces { get; set; }
        }

        public class Supportedinterfaces
        {
            public Audioplayer AudioPlayer { get; set; }
        }

        public class Audioplayer
        {
            public string token { get; set; }
            public int offsetInMilliseconds { get; set; }
            public string playerActivity { get; set; }
        }

        public class Request
        {
            public string type;
            public string timestamp;
            public string requestId;
            public string locale;
        }


        public class LaunchRequest : Request
        {
        }

        public class IntentRequest : Request
        {
            public string dialogState { get; set; }
            public Intent intent { get; set; }
        }

        public class Intent
        {
            public string name { get; set; }
            public string confirmationStatus { get; set; }
            public Dictionary<string, Slot> slots { get; set; }
        }

        public class Slot
        {
            public string name { get; set; }
            public string value { get; set; }
            public string confirmationStatus { get; set; }
            public Resolutions resolutions { get; set; }
        }

        public class Resolutions
        {
            public Resolutionsperauthority[] resolutionsPerAuthority { get; set; }
        }

        public class Resolutionsperauthority
        {
            public string authority { get; set; }
            public Status status { get; set; }
            public Value[] values { get; set; }
        }

        public class Status
        {
            public string code { get; set; }
        }

        public class Value
        {
            public Value1 value { get; set; }
        }

        public class Value1
        {
            public string name { get; set; }
            public string id { get; set; }
        }
        #endregion
    }
}