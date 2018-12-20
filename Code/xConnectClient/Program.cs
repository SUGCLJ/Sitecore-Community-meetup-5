using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Client.WebApi;
using Sitecore.XConnect.Collection.Model;
using Sitecore.XConnect.Schema;
using Sitecore.Xdb.Common.Web;

namespace xConnectClient
{
    class Program
    {
        public const string xConnectURL = "https://eef.xconnect";
        private const string Thumbprint = "8bc9de990ae25c39a3f23dea2251ebfa2d0a90cb";

        //{9B8C8D7E-E9B6-4BA1-8B8C-A74BB9897406}
        //sitecore/system/Marketing Control Panel/Goals/Enter Competition
        private static Guid CompEnteredGoalID = Guid.Parse("{EA92A726-4695-4E64-A1B0-3E13108DB64C}");
        private static Guid CompCompletedGoalID = Guid.Parse("{6DA3D475-31E4-4EC3-A229-8950468C6F57}");
        private static Guid ShowId = Guid.Parse("{6DA3D475-31E4-4EC3-A229-8950468C6F57}");   
        private static Guid CompWinGoalID = Guid.Parse("{611BE608-6D1E-45CA-9CF1-7C2A7D59BE88}");
        private static Guid pageViewID = Guid.Parse("{71CC6BA3-0C90-4881-9F45-A6B070335EC7}");
        //sitecore/system/Marketing Control Panel/Taxonomies/Channel/Offline/Event/Other event
        private static Guid enterStoreChannelId = Guid.Parse("{670BB98B-B352-40C1-99C8-880BF2AA4C54}");
        private static string userAgent = "xConnectDemo Console App";

             
        private static Random rand = new Random(Environment.TickCount);

        static void Main(string[] args)
        {
            try
            {
                //1 - 1
                //  Console.WriteLine("Adding Contact: Start");
                 AddRandomContactAddCompetion(false);
                //  Console.WriteLine("Contact added successfully!");

                //1 .2 3 Add few contacts - 
                //Console.WriteLine("Adding Contact: Start");
                //AddRandomContactAddCompetion(true);
                //Console.WriteLine("Contact added successfully!");



                //Console.WriteLine("Add to all user Completed Goal");
                //AddAllContactToCompetionCompletedGoal();


                //3 Extract WINNER  
                //Console.WriteLine("Choose Winner");
                //SelectWinnerCompetionGoal();



                //4:
                //Console.WriteLine("Search Contacts: Start");
                //SearchContacts();
                //Console.WriteLine("Search Contacts: End");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }         
      
        private static XConnectClient GetClient()
        {
            // Valid certificate thumbprint must be passed in
            CertificateWebRequestHandlerModifierOptions options =
                CertificateWebRequestHandlerModifierOptions.Parse("StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=99BCB5C8EC438A165CE3BDE23A53A15E4E3F2FCB");

            // Optional timeout modifier
            var certificateModifier = new CertificateWebRequestHandlerModifier(options);

            List<IHttpClientModifier> clientModifiers = new List<IHttpClientModifier>();
            var timeoutClientModifier = new TimeoutHttpClientModifier(new TimeSpan(0, 0, 20));
            clientModifiers.Add(timeoutClientModifier);

            // This overload takes three client end points - collection, search, and configuration
            var collectionClient = new CollectionWebApiClient(new Uri(xConnectURL+"/odata"), clientModifiers, new[] { certificateModifier });
            var searchClient = new SearchWebApiClient(new Uri(xConnectURL+"/odata"), clientModifiers, new[] { certificateModifier });
            var configurationClient = new ConfigurationWebApiClient(new Uri(xConnectURL+"/configuration"), clientModifiers, new[] { certificateModifier });

            var config = new XConnectClientConfiguration(
                               new XdbRuntimeModel(CollectionModel.Model),
                               collectionClient,
                               searchClient,
                               configurationClient);

            try
            {
                config.Initialize();
            }
            catch (XdbModelConflictException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return new XConnectClient(config);
        }

        private static void AddRandomContactAddCompetion(bool competitionEntry)
        {
            using (var client = GetClient())
            {
                string firstName = RandName();
                string lastName = RandLastName();
                string email = firstName + lastName + "@gmail.com";

                var contactReference = new IdentifiedContactReference("email", email);
                var contact = client.Get(contactReference,
                    new ExpandOptions
                    {
                        FacetKeys =
                        {PersonalInformation.DefaultFacetKey,
                    EmailAddressList.DefaultFacetKey
                        }
                    });

                bool createContact = false;
                if (contact == null)
                {
                    contact = new Contact();
                    Console.WriteLine("Can't find contact , we will create one: "+ email);
                    client.AddContactIdentifier(contact, new ContactIdentifier("email", email, ContactIdentifierType.Known));
                    client.AddContact(contact);
                    createContact = true;
                }
                else
                {
                    Console.WriteLine($"Contact Found: {contact.Id.Value}:{ email }");
                }
                SetPersonalInformation(client, contact, firstName, lastName);
                SetEmail(client, contact, email);

                if (competitionEntry) AddEventInteraction(client, contact, CompEnteredGoalID, true);
                else AddEventInteraction(client, contact, pageViewID, false);
                client.Submit();

                if (createContact)
                    Console.WriteLine($"Contact Add with ID: {contact.Id.Value}");
            }
        }

        private static async void AddAllContactToCompetionCompletedGoal()
        {
            using (var client = GetClient())
            {
                var queryable = client.Contacts
                    .Where(c => c.Interactions.Any(x => x.StartDateTime > DateTime.UtcNow.AddDays(-30)))
                    .WithExpandOptions(new ContactExpandOptions("Personal"));

                var results = await queryable.ToSearchResults();
                var contacts = await results.Results.Select(x => x.Item).Where(x => x.Personal() != null).ToList();

                foreach (var cont in contacts)
                {
                    Console.WriteLine($"{cont.Personal()?.FirstName} {cont.Personal()?.LastName}");

                    AddEventInteraction(client, cont, CompCompletedGoalID, true);
                    
                }
                client.Submit();
            }
        }
        private static async void SelectWinnerCompetionGoal()
        {
            using (var client = GetClient())
            {
                var queryable = client.Contacts
                .Where(c => c.Interactions.Any(x => x.StartDateTime > DateTime.UtcNow.AddDays(-30)))
                .WithExpandOptions(new ContactExpandOptions("Personal"));

                var results = await queryable.ToSearchResults();
                var contacts = await results.Results.Select(x => x.Item).Where(x => x.Personal() != null).ToList();

                int ran = rand.Next(1, contacts.Count() - 1);
                var cont = contacts[ran];
                if (cont != null)
                {
                    Console.WriteLine($"Winner: {cont.Personal()?.FirstName} {cont.Personal()?.LastName}");
                    AddEventInteraction(client, cont, CompWinGoalID, true);
                    client.Submit();
                }                                
                
            }
        }
      
        private static void AddEventInteraction(XConnectClient client, Contact contact, Guid compEnteredGoalID, bool asGoal= false)
        {
            var interaction = new Interaction(contact, InteractionInitiator.Contact, enterStoreChannelId, userAgent);          
            interaction.Events.Add(asGoal?  new Goal(compEnteredGoalID, DateTime.UtcNow) : new Sitecore.XConnect.Event(compEnteredGoalID, DateTime.UtcNow));
            client.AddInteraction(interaction);
        }             

        private static void SetEmail(XConnectClient client, Contact contact, string email)
        {          
            var emailFacet = new EmailAddressList(new EmailAddress(email, true), "email");
            client.SetFacet<EmailAddressList>(contact, EmailAddressList.DefaultFacetKey, emailFacet);
        }

        private static void SetPersonalInformation(XConnectClient client, Contact contact, string firstName, string lastName)
        {
            var personalInfoFacet = new PersonalInformation
            {
                FirstName = firstName,
                LastName = lastName
            };
            client.SetFacet<PersonalInformation>(contact, PersonalInformation.DefaultFacetKey, personalInfoFacet);                      
        }
            
     
        private static string  RandName()
        {
            string[] maleNames = new string[12] {"Laur", "Marian", "Doru", "Robert", "Andy", "Adam", "Radu", "Oana", "George", "Vlad","Octavian","Horea" };
            string[] femaleNames = new string[5] { "Dana", "Daniela", "Aura", "Anca" ,"Maria"};
           
            if (rand.Next(1, 2) == 1)
            {
               return  maleNames[rand.Next(0, maleNames.Length - 1)];
            }
            else
            {
                return femaleNames[rand.Next(0, femaleNames.Length - 1)];
            }
        }
        private static string RandLastName()
        {
            string[] lastNames = new string[8] { "Ionesco", "Popesco", "Badio", "Popa", "Petre" ,"Scoutt","Gigi","Petresco"};

            return lastNames[rand.Next(0, lastNames.Length - 1)];
           
        }

       
    }
}
