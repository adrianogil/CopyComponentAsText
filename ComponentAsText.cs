using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

using System;
using System.Reflection;

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

public class SerializableGameObjectReference : XMLSerializableObject<SerializableGameObjectReference>
{
    [XmlAttribute("GameObjectReferenceId")]
    public int gameObjectReferenceId;

    [XmlAttribute("GameObjectReferenceName")]
    public string gameObjectReferenceName;
}

public class SerializableComponentReference : XMLSerializableObject<SerializableComponentReference>
{
    [XmlAttribute("ComponentReferenceId")]
    public int componentReferenceId;

    [XmlAttribute("ComponentReferenceName")]
    public string componentReferenceName;
}

public class SerializableVector3 : XMLSerializableObject<SerializableVector3>
{
    [XmlAttribute("vecX")]
    public float x;

    [XmlAttribute("vecY")]
    public float y;

    [XmlAttribute("vecZ")]
    public float z;

    public SerializableVector3() {}
    public SerializableVector3(Vector3 vector) { x = vector.x; y = vector.y; z = vector.z;}
    public Vector3 GetVector3() { return new Vector3(x,y,z); }
}

public class SerializableVector2 : XMLSerializableObject<SerializableVector2>
{
    [XmlAttribute("vecX")]
    public float x;

    [XmlAttribute("vecY")]
    public float y;

    public SerializableVector2() {}
    public SerializableVector2(Vector2 vector) { x = vector.x; y = vector.y;}
    public Vector2 GetVector2() { return new Vector2(x,y); }
}

public class SerializableComponentProperty : XMLSerializableObject<SerializableComponentProperty>
{
    [XmlAttribute("PropertyName")]
    public string propertyName;

    [XmlAttribute("PropertyValue")]
    public string propertyValue;

    [XmlAttribute("PropertyType")]
    public int propertyType;
}

public class SerializableComponent : XMLSerializableObject<SerializableComponent>
{
    [XmlAttribute("ComponentName")]
    public string componentName;

    [XmlAttribute("ComponentId")]
    public int componentId;

    [XmlArray("ComponentProperties")]
    [XmlArrayItem("ComponentProp")]
    public List<SerializableComponentProperty> properties = new List<SerializableComponentProperty>();
}

public class SerializableGameObject : XMLSerializableObject<SerializableGameObject>
{
    [XmlAttribute("GameObjectName")]
    public string name;

    [XmlAttribute("GameObjectId")]
    public int gameObjectId;

    [XmlArray("GameObjectComponents")]
    [XmlArrayItem("Component")]
    public List<SerializableComponent> components = new List<SerializableComponent>();

    [XmlArray("SerializableChildren")]
    [XmlArrayItem("SerializableChild")]
    public List<SerializableGameObject> children = new List<SerializableGameObject>();
}

public class ComponentAsText : EditorWindow
{
    // In order to perform Serialization of GameObject references
    static List<SerializableGameObjectReference> gameObjectsSerializableReferences = new List<SerializableGameObjectReference>();
    static List<GameObject> referencedGameObjects = new List<GameObject>();
    static List<SerializableComponentProperty> objectReferenceProperties = new List<SerializableComponentProperty>();
    // In order to perform Serialization of Component references
    static List<SerializableComponentReference> componentsSerializableReferences = new List<SerializableComponentReference>();
    static List<Component> referencedComponents = new List<Component>();
    static List<SerializableComponentProperty> componentReferenceProperties = new List<SerializableComponentProperty>();

    // In order to perform Deserialization of GameObject references
    static List<SerializableGameObjectReference> gameObjectsDeserializableReferences = new List<SerializableGameObjectReference>();
    static List<Component> componentsToSetGameObject = new List<Component>();
    static List<string> gameObjectPropertyNames = new List<string>();

    // In order to perform Deserialization of Component references
    static List<SerializableComponentReference> componentsDeserializableReferences = new List<SerializableComponentReference>();
    static List<Component> componentsToSetComponent= new List<Component>();
    static List<string> componentPropertyNames = new List<string>();

    static int currentGameObjectId = -1;
    static int currentComponentId = -1;


    [MenuItem("GameObject/Tools/Copy as Text", false, 0)]
    static void CopyGameObject()
    {
        Copy();
    }

    [MenuItem("GameObject/Tools/Paste from Text", false, 0)]
    static void PasteGameObject()
    {
        Paste();
    }

    [MenuItem("GameObject/Copy all components as Text")]
    static void Copy()
    {
        EditorUtility.DisplayProgressBar("Copying GameObjects", "...", 0f);

        objectReferenceProperties.Clear();
        gameObjectsSerializableReferences.Clear();
        referencedGameObjects.Clear();
        componentsSerializableReferences.Clear();
        referencedComponents.Clear();

        currentGameObjectId = 0;
        currentComponentId = 0;

        GameObject target = Selection.activeGameObject;

        SerializableGameObject sGObject = GetSerializableGameObject(target);

        EditorUtility.DisplayProgressBar("Copying GameObjects", "...", 0.5f);

        GetReferencesForSerialization(target, sGObject);

        EditorUtility.DisplayProgressBar("Copying GameObjects", "...", 1f);

        if (sGObject != null)
        {
            EditorGUIUtility.systemCopyBuffer = sGObject.GetString();
        }

        EditorUtility.ClearProgressBar();
    }

    static void VerifyReferencedGameObject(GameObject gameObject, SerializableGameObject sGObject)
    {
        for (int i = 0; i < referencedGameObjects.Count; i++)
        {
            if (gameObject == referencedGameObjects[i])
            {
                gameObjectsSerializableReferences[i].gameObjectReferenceId = sGObject.gameObjectId;
                objectReferenceProperties[i].propertyValue = gameObjectsSerializableReferences[i].GetString();
            }
        }
    }

    static void VerifyReferencedComponents(Component component, SerializableComponent sComponent)
    {
        for (int i = 0; i < referencedComponents.Count; i++)
        {
            if (component == referencedComponents[i])
            {
                componentsSerializableReferences[i].componentReferenceId = sComponent.componentId;
                componentReferenceProperties[i].propertyValue = componentsSerializableReferences[i].GetString();
            }
        }
    }

    static void GetReferencesForSerialization(GameObject gameObject, SerializableGameObject sGObject)
    {
        if (gameObject == null)
        {
            return;
        }

        VerifyReferencedGameObject(gameObject, sGObject);

        Component[] copiedComponents = gameObject.GetComponents<Component>();
        if (copiedComponents != null)
        {
            for (int c = 0; c < copiedComponents.Length; c++)
            {
                VerifyReferencedComponents(copiedComponents[c], sGObject.components[c]);
            }
        }

        if (gameObject.transform.childCount > 0)
        {
            for (int s = 0; s < gameObject.transform.childCount; s++)
            {
                GetReferencesForSerialization(gameObject.transform.GetChild(s).gameObject, sGObject.children[s]);
            }
        }
    }

    static SerializableGameObject GetSerializableGameObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return null;
        }

        Component[] copiedComponents = gameObject.GetComponents<Component>();

        if (copiedComponents != null)
        {
            SerializableGameObject sGObject = new SerializableGameObject();
            sGObject.gameObjectId = currentGameObjectId;
            sGObject.name = gameObject.name;
            SerializableComponent sComponent;

            currentGameObjectId++;

            for (int c = 0; c < copiedComponents.Length; c++)
            {
                sComponent = new SerializableComponent();
                sComponent.componentId = currentComponentId;
                sComponent.componentName = copiedComponents[c].GetType ().Name;

                currentComponentId++;

                SerializableComponentProperty sProp;

                Type type = copiedComponents[c].GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
                PropertyInfo[] pinfos = type.GetProperties(flags);
                foreach (var pinfo in pinfos) {
                     if (pinfo.CanWrite) {

                        try {

                            if (pinfo.PropertyType == typeof(string) ||
                                 pinfo.PropertyType == typeof(int)    ||
                                 pinfo.PropertyType == typeof(float)  ||
                                 pinfo.PropertyType == typeof(bool)  )
                             {
                                sProp = new SerializableComponentProperty();
                                sProp.propertyName = pinfo.Name;
                                if (pinfo.PropertyType == typeof(string) && pinfo.GetValue(copiedComponents[c], null) == null)
                                {
                                    sProp.propertyValue = "";
                                }
                                else
                                {
                                    sProp.propertyValue = pinfo.GetValue(copiedComponents[c], null).ToString();
                                }

                                if (pinfo.PropertyType == typeof(string))
                                    sProp.propertyType = 0;
                                else if (pinfo.PropertyType == typeof(int))
                                    sProp.propertyType = 1;
                                else if (pinfo.PropertyType == typeof(float))
                                    sProp.propertyType = 2;
                                else if (pinfo.PropertyType == typeof(bool))
                                    sProp.propertyType = 3;
                                sComponent.properties.Add(sProp);
                             }
                             else if (pinfo.PropertyType == typeof(Vector3))
                             {
                                sProp = new SerializableComponentProperty();
                                sProp.propertyName = pinfo.Name;
                                sProp.propertyValue = new SerializableVector3((Vector3)pinfo.GetValue(copiedComponents[c], null)).GetString();
                                sProp.propertyType = 4;
                                sComponent.properties.Add(sProp);
                             }
                             else if (pinfo.PropertyType == typeof(Vector2))
                             {
                                sProp = new SerializableComponentProperty();
                                sProp.propertyName = pinfo.Name;
                                sProp.propertyValue = new SerializableVector2((Vector2)pinfo.GetValue(copiedComponents[c], null)).GetString();
                                sProp.propertyType = 5;
                                sComponent.properties.Add(sProp);
                             }
                             else if (pinfo.PropertyType == typeof(GameObject) && pinfo.GetValue(copiedComponents[c], null) != null)
                             {
                                SerializableGameObjectReference sGObjectReference = new SerializableGameObjectReference()
                                {
                                    gameObjectReferenceName = ((GameObject)pinfo.GetValue(copiedComponents[c], null)).name
                                };
                                gameObjectsSerializableReferences.Add(sGObjectReference);
                                referencedGameObjects.Add((GameObject)pinfo.GetValue(copiedComponents[c], null));

                                sProp = new SerializableComponentProperty();
                                sProp.propertyName = pinfo.Name;
                                //sProp.propertyValue
                                sProp.propertyType = 6;
                                sComponent.properties.Add(sProp);
                                objectReferenceProperties.Add(sProp);
                             }
                             else if ((pinfo.PropertyType == typeof(Component) || pinfo.PropertyType.IsSubclassOf(typeof(Component))) &&
                                pinfo.GetValue(copiedComponents[c], null) != null)
                             {
                                SerializableComponentReference sComponentReference = new SerializableComponentReference()
                                {
                                    componentReferenceName = ((Component)pinfo.GetValue(copiedComponents[c], null)).GetType ().Name
                                };
                                componentsSerializableReferences.Add(sComponentReference);
                                referencedComponents.Add((Component)pinfo.GetValue(copiedComponents[c], null));

                                sProp = new SerializableComponentProperty();
                                sProp.propertyName = pinfo.Name;
                                //sProp.propertyValue
                                sProp.propertyType = 7;
                                sComponent.properties.Add(sProp);
                                componentReferenceProperties.Add(sProp);
                             }
                         }
                         catch {}
                     }
                 }

                 FieldInfo[] finfos = type.GetFields(flags);
                 foreach (var finfo in finfos) {
                     if (finfo.FieldType == typeof(string) ||
                             finfo.FieldType == typeof(int)    ||
                             finfo.FieldType == typeof(float)  ||
                             finfo.FieldType == typeof(bool)  )
                         {
                            sProp = new SerializableComponentProperty();
                            sProp.propertyName = finfo.Name;
                            if (finfo.FieldType == typeof(string) && finfo.GetValue(copiedComponents[c]) == null)
                            {
                                sProp.propertyValue = "";
                            }
                            else
                            {
                                sProp.propertyValue = finfo.GetValue(copiedComponents[c]).ToString();
                            }

                            if (finfo.FieldType == typeof(string))
                                sProp.propertyType = 0;
                            else if (finfo.FieldType == typeof(int))
                                sProp.propertyType = 1;
                            else if (finfo.FieldType == typeof(float))
                                sProp.propertyType = 2;
                            else if (finfo.FieldType == typeof(bool))
                                sProp.propertyType = 3;
                            sComponent.properties.Add(sProp);
                         }
                         else if (finfo.FieldType == typeof(Vector3))
                         {
                            sProp = new SerializableComponentProperty();
                            sProp.propertyName = finfo.Name;
                            sProp.propertyValue = new SerializableVector3((Vector3)finfo.GetValue(copiedComponents[c])).GetString();
                            sProp.propertyType = 4;
                            sComponent.properties.Add(sProp);
                         }
                         else if (finfo.FieldType == typeof(Vector2))
                         {
                            sProp = new SerializableComponentProperty();
                            sProp.propertyName = finfo.Name;
                            sProp.propertyValue = new SerializableVector2((Vector2)finfo.GetValue(copiedComponents[c])).GetString();
                            sProp.propertyType = 5;
                            sComponent.properties.Add(sProp);
                         }
                         else if (finfo.FieldType == typeof(GameObject) && ((GameObject)finfo.GetValue(copiedComponents[c])) != null)
                         {
                            SerializableGameObjectReference sGObjectReference = new SerializableGameObjectReference()
                            {
                                gameObjectReferenceName = ((GameObject)finfo.GetValue(copiedComponents[c])).name
                            };
                            gameObjectsSerializableReferences.Add(sGObjectReference);
                            referencedGameObjects.Add((GameObject)finfo.GetValue(copiedComponents[c]));

                            sProp = new SerializableComponentProperty();
                            sProp.propertyName = finfo.Name;
                            //sProp.propertyValue
                            sProp.propertyType = 6;
                            sComponent.properties.Add(sProp);
                            objectReferenceProperties.Add(sProp);
                         }
                         else if ((finfo.FieldType == typeof(Component) || finfo.FieldType.IsSubclassOf(typeof(Component))) &&
                            finfo.GetValue(copiedComponents[c]) != null)
                         {
                            SerializableComponentReference sComponentReference = new SerializableComponentReference()
                            {
                                componentReferenceName = ((Component)finfo.GetValue(copiedComponents[c])).GetType ().Name
                            };
                            componentsSerializableReferences.Add(sComponentReference);
                            referencedComponents.Add((Component)finfo.GetValue(copiedComponents[c]));

                            sProp = new SerializableComponentProperty();
                            sProp.propertyName = finfo.Name;
                            //sProp.propertyValue
                            sProp.propertyType = 7;
                            sComponent.properties.Add(sProp);
                            componentReferenceProperties.Add(sProp);
                         }
                 }

                sGObject.components.Add(sComponent);
            }

            if (gameObject.transform.childCount > 0)
            {
                SerializableGameObject sChildrenGObject;

                for (int s = 0; s < gameObject.transform.childCount; s++)
                {
                    sChildrenGObject = GetSerializableGameObject(gameObject.transform.GetChild(s).gameObject);
                    sGObject.children.Add(sChildrenGObject);
                }
            }

            return sGObject;
        }

        return null;
    }

    [MenuItem("GameObject/Paste all components from Text")]
    static void Paste()
    {
        EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 0f);

        string componentsText = EditorGUIUtility.systemCopyBuffer;

        //Debug.Log("ComponentAsText::Paste - " + componentsText);

        gameObjectsDeserializableReferences.Clear();
        componentsToSetGameObject.Clear();
        gameObjectPropertyNames.Clear();

        componentsDeserializableReferences.Clear();
        componentsToSetComponent.Clear();
        componentPropertyNames.Clear();

        SerializableGameObject sGObject = SerializableGameObject.LoadFromText(componentsText);

        EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 0.3f);

        if (sGObject != null)
        {
            //Debug.Log("ComponentAsText::Paste - " + Selection.gameObjects.Length);

            foreach (var targetGameObject in Selection.gameObjects)
            {
                if (!targetGameObject) continue;

                Debug.Log("ComponentAsText::Paste - GameObject.name = " + targetGameObject.name);

                PasteSerializableGameObject(targetGameObject, sGObject);

                EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 0.6f);

                GetReferencesForDeserialization(targetGameObject, sGObject);

                EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 1f);
            }
        }

        EditorUtility.ClearProgressBar();
    }

    static void VerifyGameObject(GameObject gameObject, SerializableGameObject sGObject)
    {
        for (int i = 0; i < gameObjectsDeserializableReferences.Count; i++)
        {
            if (sGObject.gameObjectId == gameObjectsDeserializableReferences[i].gameObjectReferenceId)
            {
                Type type = componentsToSetGameObject[i].GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
                PropertyInfo[] pinfos = type.GetProperties(flags);
                foreach (var pinfo in pinfos) {
                    if (pinfo.CanWrite && pinfo.Name == gameObjectPropertyNames[i]) {
                        try {
                            pinfo.SetValue(componentsToSetGameObject[i], gameObject, null);
                        } catch {}
                    }
                }

                FieldInfo[] finfos = type.GetFields(flags);
                foreach (var finfo in finfos) {
                    if (finfo.Name == gameObjectPropertyNames[i]) {
                        try {
                            finfo.SetValue(componentsToSetGameObject[i], gameObject);
                        } catch {}
                    }
                }
            }
        }
    }

    /// <summary>
    /// Assumption: component represents the same sComponent from source object
    /// </summary>
    static void VerifyComponent(Component component, SerializableComponent sComponent)
    {
        //Debug.Log("ComponentAsText::VerifyComponent - testing " + component.GetType().Name + " with SerializableComponent " + sComponent.componentName);

        for (int i = 0; i < componentsDeserializableReferences.Count; i++)
        {
            if (sComponent.componentId == componentsDeserializableReferences[i].componentReferenceId)
            {
                Type type = componentsToSetComponent[i].GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
                PropertyInfo[] pinfos = type.GetProperties(flags);
                foreach (var pinfo in pinfos) {
                    if (pinfo.CanWrite && pinfo.Name == componentPropertyNames[i]) {
                        try {
                            pinfo.SetValue(componentsToSetComponent[i], component, null);
                        } catch {}
                    }
                }

                FieldInfo[] finfos = type.GetFields(flags);
                foreach (var finfo in finfos) {
                    if (finfo.Name == componentPropertyNames[i]) {
                        try {
                            finfo.SetValue(componentsToSetComponent[i], component);
                        } catch {}
                    }
                }
            }
        }
    }

    static void GetReferencesForDeserialization(GameObject gameObject, SerializableGameObject sGObject)
    {
        if (gameObject == null)
        {
            return;
        }

        //Debug.Log("gil - ComponentAsText::GetReferencesForDeserialization - Testing " + gameObject.name + " with sGObject " + sGObject.name);

        VerifyGameObject(gameObject, sGObject);

        Component[] copiedComponents = gameObject.GetComponents<Component>();
        if (copiedComponents != null)
        {
            List<SerializableComponent> sComponents = sGObject.components;

            for (int c = 0; c < sComponents.Count; c++)
            {
                for (int i = 0; i < copiedComponents.Length; i++)
                {
                    if (copiedComponents[i] != null && copiedComponents[i].GetType().Name == sComponents[c].componentName)
                    {
                        VerifyComponent(copiedComponents[i], sComponents[c]);
                        copiedComponents[i] = null;
                        break;
                    }
                }
            }
        }

        if (gameObject.transform.childCount > 0)
        {
            for (int s = 0; s < gameObject.transform.childCount; s++)
            {
                GetReferencesForDeserialization(gameObject.transform.GetChild(s).gameObject, sGObject.children[s]);
            }
        }
    }

    static bool isDuplicatedComponent(GameObject gameObject, SerializableGameObject sGObject, SerializableComponent component)
    {
        int numberOfComponents = 0;

        foreach (var copiedComponent in sGObject.components)
        {
            if (copiedComponent.componentName == component.componentName)
            {
                numberOfComponents++;
            }
        }

        return gameObject.GetComponent(component.componentName) != null && numberOfComponents == 1;

    }

    static void PasteSerializableGameObject(GameObject gameObject, SerializableGameObject sGObject)
    {
        gameObject.name = sGObject.name;

        foreach (var copiedComponent in sGObject.components)
        {
            if (copiedComponent == null) continue;

            Component comp;

            if (copiedComponent.componentName == "Transform")
            {
                comp = (Component) gameObject.transform;
            }
            else if (isDuplicatedComponent(gameObject, sGObject, copiedComponent))
            {
                comp = gameObject.GetComponent(copiedComponent.componentName);
            }
            else
            {
                comp = UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(gameObject, "Assets/Editor/Tools/CopyComponents/ComponentAsText.cs (134,21)", copiedComponent.componentName);
            }

            if (comp == null) continue;

            foreach (var prop in copiedComponent.properties)
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
                                Debug.Log("Trying to set " + copiedComponent.componentName + "." + prop.propertyName + " = " + prop.propertyValue +
                                " [" + pinfo.PropertyType.ToString() + "] ");

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
                                    gameObjectsDeserializableReferences.Add(sGObjectReference);
                                    componentsToSetGameObject.Add(comp);
                                    gameObjectPropertyNames.Add(prop.propertyName);
                                }
                            }
                            else if (prop.propertyType == 7 && !string.IsNullOrEmpty(prop.propertyValue))
                            {
                                SerializableComponentReference sComponentReference = SerializableComponentReference.LoadFromText(prop.propertyValue);

                                if (sComponentReference != null)
                                {
                                    componentsDeserializableReferences.Add(sComponentReference);
                                    componentsToSetComponent.Add(comp);
                                    componentPropertyNames.Add(prop.propertyName);
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
                                Debug.Log("Trying to set " + copiedComponent.componentName + "." + prop.propertyName + " = " + prop.propertyValue +
                                " [" + finfo.FieldType.ToString() + "] ");

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
                                    gameObjectsDeserializableReferences.Add(sGObjectReference);
                                    componentsToSetGameObject.Add(comp);
                                    gameObjectPropertyNames.Add(prop.propertyName);
                                }
                            }
                            else if (prop.propertyType == 7)
                            {
                                SerializableComponentReference sComponentReference = SerializableComponentReference.LoadFromText(prop.propertyValue);

                                if (sComponentReference != null)
                                {
                                    componentsDeserializableReferences.Add(sComponentReference);
                                    componentsToSetComponent.Add(comp);
                                    componentPropertyNames.Add(prop.propertyName);
                                }
                            }
                         }
                         catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                    }
                }
            }
        }

        GameObject childGObject;

        foreach (var sGChild in sGObject.children)
        {
            childGObject = new GameObject();
            childGObject.transform.parent = gameObject.transform;
            PasteSerializableGameObject(childGObject, sGChild);
        }
    }
}