using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Http.Description;
using SophtronAlexaSkill.Models;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using SophtronEntities;
using System.Web.Script.Serialization;

namespace SophtronAlexaSkill
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AlexaController : ApiController
    {
        private const string helpString = "Sophtron helps you check account balance and payment due date. To get started, make sure you have enabled linking to your Sophtron account. Then ask Sophtron with questions like: What is my bank account balance? Or when is my utitlity account due? Please tell us which account you would to inquire about.";
        private const string noAccountFoundString = "Hmmm we cannot find your Sophtron account. Please go to your Alexa Account and link Alexa with your Sophtron account.";
        private const string launchString = "Please tell us which account you would like to inquire about.";
        private const string invalidRequestString = "Hmm we cannot understand your request. Please tell us which account you would like to inquire about.";

        /// <summary>
        /// https://developer.amazon.com/public/solutions/alexa/alexa-skills-kit/docs/developing-an-alexa-skill-as-a-web-service#verifying-that-the-request-was-sent-by-alexa
        /// </summary>
        /// <returns></returns>
        public static bool VerifySignature(string rawRequest, HttpRequestMessage request)
        {
            bool verified = true;
            string signature = AuthorizationHelper.GetAuthPhrase(request, "Signature");
            string signatureCertChainUrl = AuthorizationHelper.GetAuthPhrase(request, "SignatureCertChainUrl");

            //Check signature cert chain url
            //Step 1. validate against a correctly formatted URL
            Uri certUri = new Uri(signatureCertChainUrl);
            if (!certUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) 
                || !certUri.Host.Equals("s3.amazonaws.com", StringComparison.OrdinalIgnoreCase)
                || !certUri.AbsolutePath.StartsWith("/echo.api/"))
            {
                verified = false;
            }
            if (certUri.Port != null)
            {
                if (certUri.Port != 443)
                {
                    verified = false;
                }
            }
            //Step 2. Verify the public pem file information           
            List<X509Certificate2> additionalCerts = new List<X509Certificate2>();
            X509Certificate2 cert = SigningCertificate.ReadCerts(signatureCertChainUrl, out additionalCerts);
            //Setp 3. Check effective date and expiration date
            if (cert.NotAfter < DateTime.Now || cert.NotBefore > DateTime.Now)
            {
                verified = false;
            }
            //Step 4. Verify the Subject Alternative Name
            string san = cert.GetNameInfo(X509NameType.DnsName, false);
            if (!san.ToLower().Contains("echo-api.amazon.com"))
            {
                verified = false;
            }
            //Step 5. Verify the cert chain
            if (!SigningCertificate.VerifyCertChain(cert, additionalCerts))
            {
                verified = false;
            }
            //Step 6. Use the public key extracted from the signing certificate to decrypt the encrypted signature to produce the asserted hash value.
            //Step 7. Generate a SHA-1 hash value from the full HTTPS request body to produce the derived hash value
            //Step 8. Compare hashed body
            //Note this does not work with RSA built-in .net library, have to use BouncyCastle instead
            var signatureString = request.Headers.GetValues("Signature").First();
            byte[] signatureBytes = Convert.FromBase64String(signatureString);
            var webClient = new WebClient();
            var content = webClient.DownloadString(signatureCertChainUrl);
            var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(new StringReader(content));
            var bouncyCert = (Org.BouncyCastle.X509.X509Certificate)pemReader.ReadObject();
            var publicKey = (Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)bouncyCert.GetPublicKey();
            var signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA1withRSA");
            signer.Init(false, publicKey);
            var input = rawRequest;
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            signer.BlockUpdate(inputBytes, 0, inputBytes.Length);
            verified = signer.VerifySignature(signatureBytes);

            //Step 9. Checking the Timestamp of the Request
            DateTime timeStamp = HttpContext.Current.Timestamp;
            if (timeStamp.AddSeconds(150) < DateTime.Now)
            {
                verified = false;
            }
            return verified;
        }

        [HttpPost, ActionName("HandleAlexa")]
        public Alexa.StandardResponse HandleAlexa()
        {
            Alexa.StandardRequest request = null;
            Alexa.StandardResponse alexa = new Alexa.StandardResponse();
            string rawRequest = string.Empty;
            var se = new JavaScriptSerializer();

            try
            {

                using (var sr = new StreamReader(HttpContext.Current.Request.InputStream))
                {
                    rawRequest = sr.ReadToEnd();
                    request = se.Deserialize<Alexa.StandardRequest>(rawRequest);
                }

                if (!VerifySignature(rawRequest, this.ActionContext.Request))
                {
                    throw new HttpResponseException(HttpStatusCode.Unauthorized);
                }
                //Check timestamp again
                if (DateTime.Parse(request.request.timestamp) < DateTime.Now.AddSeconds(-150))
                {
                    throw new HttpResponseException(HttpStatusCode.Unauthorized);
                }

                //Handle help intent and stop intent
                if (request.request.type == Alexa.intentType)
                {
                    Alexa.StandardIntentRequest strIntentRequest = se.Deserialize<Alexa.StandardIntentRequest>(rawRequest);
                    Alexa.IntentRequest intentRequest = strIntentRequest.request;
                    string intentName = intentRequest.intent.name;
                    if (intentName.Equals("AMAZON.HelpIntent", StringComparison.InvariantCultureIgnoreCase))
                    {
                        alexa.response.outputSpeech.text = helpString;
                        return alexa;
                    }
                    else if (intentName.Equals("AMAZON.StopIntent", StringComparison.InvariantCultureIgnoreCase)
                        || intentName.Equals("AMAZON.CancelIntent", StringComparison.InvariantCultureIgnoreCase))
                    {
                        alexa.response.shouldEndSession = true;
                        return alexa;
                    }
                }

                //Use OAuth for account linking. Identify the logged in user
                User user = null;
                string name = this.User.Identity.Name;
                //Insert with your own code to return logged in user with given name.
                //Return link accounts prompt card if user is not found
                if (user == null)
                {
                    alexa.response.outputSpeech.text = noAccountFoundString;
                    alexa.response.card = new Alexa.Card();
                    alexa.response.card.type = "LinkAccount";
                    alexa.response.shouldEndSession = true;
                    return alexa;
                }

                //If this is a LaunchRequest
                if (request.request.type == Alexa.launchType)
                {
                    alexa.response.outputSpeech.text = launchString;
                    return alexa;
                }
                //If this is a IntentRequest
                else if (request.request.type == Alexa.intentType)
                {
                    //Find institution name
                    Alexa.StandardIntentRequest strIntentRequest = se.Deserialize<Alexa.StandardIntentRequest>(rawRequest);
                    Alexa.IntentRequest intentRequest = strIntentRequest.request;
                    string intentName = intentRequest.intent.name;
                    if (intentName.Equals("GetBalance", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (intentRequest.intent.slots["Institution"] != null)
                        {
                            if (intentRequest.intent.slots["Institution"].value != null)
                            {
                                string institutionName = intentRequest.intent.slots["Institution"].value;
                                //Found user, get institution name
                                alexa.response.outputSpeech.text = GetReply(user, institutionName);
                                return alexa;
                            }
                            else
                            {
                                alexa.response.outputSpeech.text = invalidRequestString;
                                return alexa;
                            }
                        }
                        else
                        {
                            alexa.response.outputSpeech.text = invalidRequestString;
                            return alexa;
                        }
                    }
                    else
                    {
                        alexa.response.outputSpeech.text = invalidRequestString;
                        return alexa;
                    }
                }
            }
            catch (SystemException e)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            alexa.response.outputSpeech.text = invalidRequestString;
            return alexa;
        }

        private string GetReply(User user, string institutionName)
        {
            //Found user, get institution name
            UserInstitutionAccount account = null;
            //account = GetUserInstitutionAccount(user.UserID, institutionName);
            if (account != null)
            {
                return string.Format("Your {0} Account Balance is {1}, your account payment due date is {2}. Which other account can we help you with?", institutionName, account.Balance, account.DueDate);
            }               
            else
            {
                return string.Format("Hmm we cannot find {0} with your Sophtron account. Please go to your Sophtron Account and make sure you link {0} to it. Which other account can we help you with?", institutionName);
            }
        }
    }
}