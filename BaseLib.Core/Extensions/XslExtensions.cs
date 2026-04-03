using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace BaseLib.Core.Extensions
{
    /// <summary>Extension methods for <see cref="XslCompiledTransform"/> and <see cref="XsltArgumentList"/>.</summary>
    public static class XslExtensions
    {
        /// <summary>Applies the XSLT transform to <paramref name="source"/> and returns the output as a rewound <see cref="Stream"/>.</summary>
        public static Stream Transform(this XslCompiledTransform xslt, Stream source, XsltArgumentList? arguments = null)
        {
            var output = new MemoryStream();
            using (var sourceReader = XmlReader.Create(source))
            using (var outputWriter = XmlWriter.Create(output))
            {
                xslt.Transform(sourceReader, arguments, outputWriter);
                outputWriter.Flush();
                output.Position = 0;
            }
            return output;
        }

        /// <summary>Loads a stream as an <see cref="System.Xml.XPath.XPathNavigator"/> and adds it as an XSLT parameter.</summary>
        public static void AddNavigatorParam(this XsltArgumentList arguments, string name, string namespaceUri, Stream stream)
        {
            var document = new XPathDocument(stream);
            var navigator = document.CreateNavigator();
            arguments.AddParam(name, namespaceUri, navigator);

        }
    }
}
