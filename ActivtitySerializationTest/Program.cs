using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xaml;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;

namespace ActivtitySerializationTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            var vbActivity = GetSerializedActivity(visualBasic: true);
            InvokeActivity(vbActivity);

            var csActivity = GetSerializedActivity(visualBasic: false);
            InvokeActivity(csActivity);

            Console.ReadLine();
        }

        static string GetSerializedActivity(bool visualBasic)
        {
            var activityBuilder = new ActivityBuilder
                {
                    Name = "Demo"
                };

            activityBuilder.Properties.Add(new DynamicActivityProperty { Name = "HttpClient", Type = typeof(InArgument<HttpClient>) });

            activityBuilder.Implementation = new Sequence
            {
                Activities =
                        {
                            new WriteLine
                                {
                                    Text = (visualBasic) 
                                        ?
                                        (InArgument<string>) new VisualBasicValue<string>("New HttpClient().GetAsync(\"http://google.com\").Result.Content.ReadAsStringAsync.Result")
                                        :
                                        (InArgument<string>) new CSharpValue<string>("new HttpClient().GetAsync(\"http://google.com\").Result.Content.ReadAsStringAsync.Result")
                                }
                        }
            };

            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var xamlWriter = ActivityXamlServices.CreateBuilderWriter(new XamlXmlWriter(stringWriter, new XamlSchemaContext()));
            XamlServices.Save(xamlWriter, activityBuilder);
            var serializedActivityBuilder = stringBuilder.ToString();

            Console.WriteLine(serializedActivityBuilder);

            return serializedActivityBuilder;
        }

        static void InvokeActivity(string serializedActivity)
        {
            var stringReader = new StringReader(serializedActivity);
            var xamlXmlReader = new XamlXmlReader(stringReader, new XamlXmlReaderSettings
                {
                    LocalAssembly = Assembly.GetExecutingAssembly()
                });
            var xamlReader = ActivityXamlServices.CreateReader(xamlXmlReader);
            var activity = ActivityXamlServices.Load(xamlReader, new ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                });

            var settings = new VisualBasicSettings();
            settings.ImportReferences.Add(new VisualBasicImportReference
                {
                    Assembly = typeof (HttpClient).Assembly.GetName().Name,
                    Import = typeof (HttpClient).Namespace
                });
            VisualBasic.SetSettings(activity, settings);

            WorkflowInvoker.Invoke(activity, new Dictionary<string, object>
                {
                    {"HttpClient", new HttpClient()}
                });
        }
    }
}
