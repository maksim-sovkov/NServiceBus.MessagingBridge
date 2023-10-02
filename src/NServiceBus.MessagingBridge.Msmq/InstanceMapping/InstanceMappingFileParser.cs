namespace NServiceBus.MessagingBridge.Msmq
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Routing;

    class InstanceMappingFileParser
    {
        public List<EndpointInstance> Parse(XDocument document)
        {
            var root = document.Root;
            var endpointElements = root.Descendants("endpoint");

            var instances = new List<EndpointInstance>();

            foreach (var e in endpointElements)
            {
                var endpointName = e.Attribute("name").Value;

                foreach (var i in e.Descendants("instance"))
                {
                    var discriminatorAttribute = i.Attribute("discriminator");
                    var discriminator = discriminatorAttribute?.Value;

                    var properties = i.Attributes().Where(a => a.Name != "discriminator");
                    var propertyDictionary = properties.ToDictionary(a => a.Name.LocalName, a => a.Value);

                    instances.Add(new EndpointInstance(endpointName, discriminator, propertyDictionary));
                }
            }

            return instances;
        }
    }
}