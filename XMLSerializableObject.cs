using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

using System;

public class XMLSerializableObject<T>
{
    public void Save(string path)
    {
        var serializer = new XmlSerializer(typeof(T));
        using (var stream = new FileStream(path, FileMode.Create))
        {
            using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
            {
                serializer.Serialize(xmlWriter, this);
            }
        }
    }

    public string GetString()
    {
        string xmlText = "";
        var serializer = new XmlSerializer(typeof(T));
        using (StringWriter textWriter = new StringWriter())
        {
            serializer.Serialize(textWriter, this);
            xmlText = textWriter.ToString();
        }

        return xmlText;
    }

    public static T Load(string path)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return (T) (serializer.Deserialize(stream));
            }
        }
        catch (XmlException e) { }

        return default(T);
    }

    //Loads the xml directly from the given string. Useful in combination with www.text.
    public static T LoadFromText(string text)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)(serializer.Deserialize(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)), Encoding.UTF8)));
        }
        catch (XmlException e) { }

        return default(T);
    }
}