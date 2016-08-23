using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Xml;
using System.Xml.Serialization;

using System.Reflection;

public class SerializableComponent : XMLSerializableObject<SerializableComponent>
{
    [XmlAttribute("ComponentName")]
    public string componentName;

    [XmlAttribute("ComponentId")]
    public int componentId;

    [XmlArray("ComponentProperties")]
    [XmlArrayItem("ComponentProp")]
    public List<SerializableComponentProperty> properties = new List<SerializableComponentProperty>();

    public void ApplySerializedDataTo(Component comp)
    {
        foreach (var prop in properties)
        {
            Type type = comp.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos) {
                 if (pinfo.CanWrite && pinfo.Name == prop.propertyName) {
                     try {
                        if (prop.propertyType == 0)
                        {
                            pinfo.SetValue(comp, prop.propertyValue, null);
                        }
                        else if (prop.propertyType == 1)
                        {
                            pinfo.SetValue(comp, int.Parse(prop.propertyValue), null);
                        }
                        else if (prop.propertyType == 2)
                        {
                            pinfo.SetValue(comp, float.Parse(prop.propertyValue), null);
                        }
                        else if (prop.propertyType == 3)
                        {
                            // Debug.Log("Trying to set " + copiedComponent.componentName + "." + prop.propertyName + " = " + prop.propertyValue +
                            // " [" + pinfo.PropertyType.ToString() + "] ");

                            pinfo.SetValue(comp, bool.Parse(prop.propertyValue), null);
                        }
                        else if (prop.propertyType == 4)
                        {
                            pinfo.SetValue(comp, SerializableVector3.LoadFromText(prop.propertyValue).GetVector3(), null);
                        }
                        else if (prop.propertyType == 5)
                        {
                            pinfo.SetValue(comp, SerializableVector2.LoadFromText(prop.propertyValue).GetVector2(), null);
                        }
                        else if (prop.propertyType == 6 && !string.IsNullOrEmpty(prop.propertyValue))
                        {
                            SerializableGameObjectReference sGObjectReference = SerializableGameObjectReference.LoadFromText(prop.propertyValue);

                            if (sGObjectReference != null)
                            {
                                ComponentAsText.gameObjectsDeserializableReferences.Add(sGObjectReference);
                                ComponentAsText.componentsToSetGameObject.Add(comp);
                                ComponentAsText.gameObjectPropertyNames.Add(prop.propertyName);
                            }
                        }
                        else if (prop.propertyType == 7 && !string.IsNullOrEmpty(prop.propertyValue))
                        {
                            SerializableComponentReference sComponentReference = SerializableComponentReference.LoadFromText(prop.propertyValue);

                            if (sComponentReference != null)
                            {
                                ComponentAsText.componentsDeserializableReferences.Add(sComponentReference);
                                ComponentAsText.componentsToSetComponent.Add(comp);
                                ComponentAsText.componentPropertyNames.Add(prop.propertyName);
                            }
                        }
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.

                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos) {
                if (finfo.Name == prop.propertyName) {
                     try {
                        if (prop.propertyType == 0)
                        {
                            finfo.SetValue(comp, prop.propertyValue);
                        }
                        else if (prop.propertyType == 1)
                        {
                            finfo.SetValue(comp, int.Parse(prop.propertyValue));
                        }
                        else if (prop.propertyType == 2)
                        {
                            finfo.SetValue(comp, float.Parse(prop.propertyValue));
                        }
                        else if (prop.propertyType == 3)
                        {
                            // Debug.Log("Trying to set " + copiedComponent.componentName + "." + prop.propertyName + " = " + prop.propertyValue +
                            // " [" + finfo.FieldType.ToString() + "] ");

                            finfo.SetValue(comp, bool.Parse(prop.propertyValue));
                        }
                        else if (prop.propertyType == 4)
                        {
                            finfo.SetValue(comp, SerializableVector3.LoadFromText(prop.propertyValue).GetVector3());
                        }
                        else if (prop.propertyType == 5)
                        {
                            finfo.SetValue(comp, SerializableVector2.LoadFromText(prop.propertyValue).GetVector2());
                        }
                        else if (prop.propertyType == 6)
                        {
                            SerializableGameObjectReference sGObjectReference = SerializableGameObjectReference.LoadFromText(prop.propertyValue);

                            if (sGObjectReference != null)
                            {
                                ComponentAsText.gameObjectsDeserializableReferences.Add(sGObjectReference);
                                ComponentAsText.componentsToSetGameObject.Add(comp);
                                ComponentAsText.gameObjectPropertyNames.Add(prop.propertyName);
                            }
                        }
                        else if (prop.propertyType == 7)
                        {
                            SerializableComponentReference sComponentReference = SerializableComponentReference.LoadFromText(prop.propertyValue);

                            if (sComponentReference != null)
                            {
                                ComponentAsText.componentsDeserializableReferences.Add(sComponentReference);
                                ComponentAsText.componentsToSetComponent.Add(comp);
                                ComponentAsText.componentPropertyNames.Add(prop.propertyName);
                            }
                        }
                     }
                     catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
        }
    }
}