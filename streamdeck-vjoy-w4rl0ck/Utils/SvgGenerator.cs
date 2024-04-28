using System.Drawing;
using System.Text;
using System.Xml;
using Svg;

namespace streamdeck_vjoy_w4rl0ck.Utils;

public class SvgGenerator
{
    public static SvgDocument CreateButtonSvg(uint number, bool active = false)
    {
        var black = new SvgColourServer(Color.Black);
        var white = new SvgColourServer(Color.White);
        var red = new SvgColourServer(Color.Red);

        var square = new SvgRectangle
        {
            Width = 200,
            Height = 200,
            Stroke = black
        };

        var circle = new SvgCircle
        {
            Fill = active ? red : black,
            Stroke = white,
            StrokeWidth = 8.0f,
            Radius = 70,
            CenterX = 100,
            CenterY = 100
        };

        var svgDoc = new SvgDocument
        {
            Width = 200,
            Height = 200
        };

        var text = new SvgText(number.ToString())
        {
            X = [100],
            Y = [100],
            FontSize = new SvgUnit(60),
            TextAnchor = SvgTextAnchor.Middle,

            Fill = white
        };

        text.Y = [svgDoc.Height / 2 + text.FontSize / 4 + 4];

        svgDoc.Children.Add(square);
        svgDoc.Children.Add(circle);
        svgDoc.Children.Add(text);

        return svgDoc;
    }

    public static string CreateButtonSvgBase64(uint number, bool active = false)
    {
        var svgDocument = CreateButtonSvg(number, active);

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = true
        };

        string svgString;

        using (var stringWriter = new StringWriter())
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            svgDocument.Write(xmlWriter);
            xmlWriter.Flush();
            svgString = stringWriter.ToString();
        }

        var bytes = Encoding.UTF8.GetBytes(svgString);
        var base64Svg = Convert.ToBase64String(bytes);

        return $"data:image/svg+xml;charset=utf8;base64,{base64Svg}";
    }
}