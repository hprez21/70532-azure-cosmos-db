using AzureCosmosDBDemo.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCosmosDBDemo
{
    class Program
    {
        private const string EndpointUrl = "https://democosmosdbhp.documents.azure.com:443/";
        private const string PrimaryKey = "jFGvUq6ZlLk4JTFZfbiY2RoW2CCxb3yhgOABMxvCnFwnq2vhIkizc5KAaICLs62Mba9eR4wNXLTJyYpqI3UFdQ==";
        private static DocumentClient client;

        static void Main(string[] args)
        {
            client =
                new DocumentClient(new Uri(EndpointUrl),
                PrimaryKey);

            Database db = new Database();

            Task.Run(async () => db = await GetDatabase("school")).Wait();

            DocumentCollection collection =
                new DocumentCollection();
            Task.Run(async () => collection = await CreateCollection(db,
                "students")).Wait();

            //CreateStudents(collection).Wait();

            //GetStudents(collection);

            //TestGraphs(collection).Wait();

            CreateStoredProcedure(collection).Wait();

            Console.WriteLine("Operación exitosa");
            Console.ReadLine();
        }

        private static async Task<Database> GetDatabase(string databaseName)
        {
            var database =
                await client
                .CreateDatabaseIfNotExistsAsync(new Database
                {
                    Id = databaseName
                });
            return database;
        }

        private static async Task<DocumentCollection> CreateCollection(
            Database database, string collectionName)
        {
            var collection =
                await client
                .CreateDocumentCollectionIfNotExistsAsync(database
                            .CollectionsLink,
                new DocumentCollection
                {
                    Id = collectionName
                },
                new RequestOptions
                {
                    OfferThroughput = 400
                });
            return collection;
        }

        private static async Task CreateStudents(DocumentCollection collection)
        {
            var s1 = new Student
            {
                Name = "Héctor Pérez",
                Subjects = new List<Subject>()
                {
                    new Subject
                    {
                        Name = "Maths",
                        Score = 9
                    },
                    new Subject
                    {
                        Name = "Spanish",
                        Score = 9
                    }
                }
            };
            var s2 = new Student
            {
                Name = "Emma Watson",
                Subjects = new List<Subject>()
                {
                    new Subject
                    {
                        Name = "Maths",
                        Score = 10
                    },
                    new Subject
                    {
                        Name = "Geography",
                        Score = 10
                    }
                }
            };

            await client.CreateDocumentAsync(collection.DocumentsLink,
                s1);
            await client.CreateDocumentAsync(collection.DocumentsLink,
                s2);
        }

        private static void GetStudents(DocumentCollection collection)
        {
            //var students =
            //    client
            //    .CreateDocumentQuery<Student>(collection.DocumentsLink)
            //    .Where(s => s.Name == "Emma Watson").ToList();

            //var students =
            //    from s in client
            //    .CreateDocumentQuery<Student>(collection.DocumentsLink)
            //    .Where(s => s.Name == "Emma Watson")
            //    select s;

            var students =
                client.CreateDocumentQuery<Student>(collection.DocumentsLink,
                "SELECT * FROM students s " +
                "WHERE s.Name = 'Emma Watson'");

            foreach (var s in students)
            {
                Console.WriteLine($"{s.Name}");
                foreach (var subject in s.Subjects)
                {
                    Console.WriteLine($"{subject.Name}: {subject.Score}");
                }
            }
        }

        private static async Task TestGraphs(DocumentCollection graph)
        {
            Dictionary<string, string> gremlinQueries = new Dictionary<string, string>
            {
                { "Cleanup",        "g.V().drop()" },
                { "AddVertex 1",    "g.addV('person').property('id', 'thomas').property('firstName', 'Thomas').property('age', 44)" },
                { "AddVertex 2",    "g.addV('person').property('id', 'mary').property('firstName', 'Mary').property('lastName', 'Andersen').property('age', 39)" },
                { "AddVertex 3",    "g.addV('person').property('id', 'ben').property('firstName', 'Ben').property('lastName', 'Miller')" },
                { "AddVertex 4",    "g.addV('person').property('id', 'robin').property('firstName', 'Robin').property('lastName', 'Wakefield')" },
                { "AddEdge 1",      "g.V('thomas').addE('knows').to(g.V('mary'))" },
                { "AddEdge 2",      "g.V('thomas').addE('knows').to(g.V('ben'))" },
                { "AddEdge 3",      "g.V('ben').addE('knows').to(g.V('robin'))" },
                { "UpdateVertex",   "g.V('thomas').property('age', 44)" },
                { "CountVertices",  "g.V().count()" },
                { "Filter Range",   "g.V().hasLabel('person').has('age', gt(40))" },
                { "Project",        "g.V().hasLabel('person').values('firstName')" },
                { "Sort",           "g.V().hasLabel('person').order().by('firstName', decr)" },
                { "Traverse",       "g.V('thomas').outE('knows').inV().hasLabel('person')" },
                { "Traverse 2x",    "g.V('thomas').outE('knows').inV().hasLabel('person').outE('knows').inV().hasLabel('person')" },
                { "Loop",           "g.V('thomas').repeat(out()).until(has('id', 'robin')).path()" },
                { "DropEdge",       "g.V('thomas').outE('knows').where(inV().has('id', 'mary')).drop()" },
                { "CountEdges",     "g.E().count()" },
                { "DropVertex",     "g.V('thomas').drop()" },
            };

            foreach (var gremlinQuery in gremlinQueries)
            {
                Console.WriteLine($"{gremlinQuery.Key}:" +
                    $"{gremlinQuery.Value}");

                IDocumentQuery<dynamic> query =
                    client.CreateGremlinQuery<dynamic>(graph,
                    gremlinQuery.Value);

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        Console.WriteLine($"\t{JsonConvert.SerializeObject(result)}");
                    }
                }
                Console.WriteLine();
            }
        }

        static async Task CreateStoredProcedure(DocumentCollection collection)
        {
            var mySProc = new StoredProcedure
            {
                Id = "HelloFromCSharp",
                // BuildMyString.com generated code. Please enjoy your string responsibly.

                Body = "function Hello(){" +
                    "    var context = getContext();" +
                    "    var response = context.getResponse();" +
                    "    response.setBody(\"Hello, World\");" +
                    "}"
            };
            var response = await client
                .CreateStoredProcedureAsync(collection.SelfLink, 
                mySProc);
        }
    }
}
