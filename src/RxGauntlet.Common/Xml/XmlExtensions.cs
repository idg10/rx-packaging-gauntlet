using System.Xml;

namespace RxGauntlet.Xml;

public static class XmlExtensions
{
    public static XmlNode GetRequiredNode(this XmlDocument document, string xpath)
    {
        return document.SelectSingleNode(xpath)
            ?? throw new InvalidOperationException($"Did not find '{xpath}'");
    }

    public static void SetAttribute(this XmlNode node, string attributeName, string attributeValue)
    {
        XmlAttributeCollection attributes = node.Attributes!;
        if (attributes[attributeName] is not XmlAttribute attribute)
        {
            attribute = node.OwnerDocument!.CreateAttribute(attributeName);
            attributes.Append(attribute);
        }

        attribute.Value = attributeValue;
    }
}
